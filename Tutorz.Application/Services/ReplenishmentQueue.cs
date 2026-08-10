using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Tutorz.Application.Services
{
    public class ReplenishRequest
    {
        public Guid CreatorId { get; set; }
        public string CreatorRole { get; set; }
    }

    public interface IReplenishmentQueue
    {
        ValueTask QueueReplenishmentAsync(ReplenishRequest request, CancellationToken cancellationToken = default);
        ValueTask<ReplenishRequest> DequeueAsync(CancellationToken cancellationToken = default);
    }

    public class ReplenishmentQueue : IReplenishmentQueue
    {
        private readonly Channel<ReplenishRequest> _queue;

        public ReplenishmentQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<ReplenishRequest>(options);
        }

        public async ValueTask QueueReplenishmentAsync(ReplenishRequest request, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(request, cancellationToken);
        }

        public async ValueTask<ReplenishRequest> DequeueAsync(CancellationToken cancellationToken = default)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
