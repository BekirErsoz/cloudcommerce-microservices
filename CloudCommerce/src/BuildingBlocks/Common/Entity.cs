
using System;
using System.Collections.Generic;

namespace CloudCommerce.BuildingBlocks.Common
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        private readonly List<object> _domainEvents = new();
        public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
        protected void AddDomainEvent(object @event) => _domainEvents.Add(@event);
    }

    public interface IAggregateRoot { }

    public interface IUnitOfWork
    {
        System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default);
    }

    public class PaginatedResult<T>
    {
        public PaginatedResult(int pageIndex, int pageSize, int totalItems, IEnumerable<T> items)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalItems = totalItems;
            Items = new List<T>(items);
        }

        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalItems { get; }
        public IReadOnlyList<T> Items { get; }
    }
}
