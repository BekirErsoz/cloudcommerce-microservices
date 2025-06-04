
using System.Threading;
using System.Threading.Tasks;

namespace CloudCommerce.BuildingBlocks.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
        void Subscribe<TEvent, THandler>() where THandler : class;
    }
}
