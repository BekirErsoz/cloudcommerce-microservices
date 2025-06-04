using CloudCommerce.BuildingBlocks.Common;
using Ordering.Domain.Events;

namespace Ordering.Domain.Aggregates
{
    public class Order : Entity, IAggregateRoot
    {
        private readonly List<OrderItem> _orderItems;

        protected Order()
        {
            _orderItems = new List<OrderItem>();
        }

        public Order(
            Guid buyerId,
            string buyerName,
            Address shippingAddress,
            Address billingAddress,
            PaymentMethod paymentMethod) : this()
        {
            BuyerId = buyerId;
            BuyerName = buyerName ?? throw new ArgumentNullException(nameof(buyerName));
            ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
            BillingAddress = billingAddress ?? throw new ArgumentNullException(nameof(billingAddress));
            PaymentMethodId = paymentMethod?.Id ?? throw new ArgumentNullException(nameof(paymentMethod));

            OrderStatus = OrderStatus.Pending;
            OrderDate = DateTime.UtcNow;

            OrderNumber = GenerateOrderNumber();

            AddDomainEvent(new OrderCreatedDomainEvent(this));
        }

        public string OrderNumber { get; private set; }
        public DateTime OrderDate { get; private set; }
        public Guid BuyerId { get; private set; }
        public string BuyerName { get; private set; }
        public OrderStatus OrderStatus { get; private set; }
        public Address ShippingAddress { get; private set; }
        public Address BillingAddress { get; private set; }
        public int? PaymentMethodId { get; private set; }
        public string Description { get; private set; }

        public decimal SubTotal { get; private set; }
        public decimal Tax { get; private set; }
        public decimal ShippingCost { get; private set; }
        public decimal Discount { get; private set; }
        public decimal Total => SubTotal + Tax + ShippingCost - Discount;

        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        public void AddOrderItem(Guid productId,string productName,decimal unitPrice,decimal discount,string pictureUrl,int units=1)
        {
            if (units <= 0)
                throw new OrderingDomainException("Invalid number of units");

            var existing = _orderItems.FirstOrDefault(o => o.ProductId == productId);

            if (existing != null)
                existing.AddUnits(units);
            else
                _orderItems.Add(new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units));

            CalculateTotals();
        }

        public void RemoveOrderItem(Guid productId)
        {
            var item = _orderItems.FirstOrDefault(o => o.ProductId == productId);
            if (item != null)
            {
                _orderItems.Remove(item);
                CalculateTotals();
            }
        }

        public void SetAwaitingValidationStatus()
        {
            if (OrderStatus != OrderStatus.Pending)
                StatusChangeException(OrderStatus.AwaitingValidation);

            OrderStatus = OrderStatus.AwaitingValidation;
            AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(Id, _orderItems));
        }

        public void SetStockConfirmedStatus()
        {
            if (OrderStatus != OrderStatus.AwaitingValidation)
                StatusChangeException(OrderStatus.StockConfirmed);

            OrderStatus = OrderStatus.StockConfirmed;
            Description = "All the items were confirmed with available stock.";
            AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));
        }

        public void SetPaidStatus()
        {
            if (OrderStatus != OrderStatus.StockConfirmed)
                StatusChangeException(OrderStatus.Paid);

            OrderStatus = OrderStatus.Paid;
            Description = "Payment completed.";
            AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, OrderItems));
        }

        public void SetShippedStatus()
        {
            if (OrderStatus != OrderStatus.Paid)
                StatusChangeException(OrderStatus.Shipped);

            OrderStatus = OrderStatus.Shipped;
            Description = "Order shipped.";
            AddDomainEvent(new OrderShippedDomainEvent(Id));
        }

        public void SetCancelledStatus()
        {
            if (OrderStatus == OrderStatus.Shipped)
                StatusChangeException(OrderStatus.Cancelled);

            OrderStatus = OrderStatus.Cancelled;
            Description = "Order cancelled.";
            AddDomainEvent(new OrderCancelledDomainEvent(Id));
        }

        private void CalculateTotals()
        {
            SubTotal = _orderItems.Sum(o => o.Units * o.UnitPrice);
            Tax = SubTotal * 0.18m;
            ShippingCost = SubTotal switch
            {
                < 100 => 15,
                < 500 => 10,
                _ => 0
            };
        }

        private void StatusChangeException(OrderStatus statusToChange) =>
            throw new OrderingDomainException($"Cannot change order status from {OrderStatus} to {statusToChange}.");

        private string GenerateOrderNumber() =>
            $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    public class Address : ValueObject
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string State { get; private set; }
        public string Country { get; private set; }
        public string ZipCode { get; private set; }

        public Address(string street,string city,string state,string country,string zipcode)
        {
            Street = street;
            City = city;
            State = state;
            Country = country;
            ZipCode = zipcode;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return Country;
            yield return ZipCode;
        }
    }

    public enum OrderStatus
    {
        Pending = 1,
        AwaitingValidation,
        StockConfirmed,
        Paid,
        Shipped,
        Cancelled
    }
}
