using System.Collections.Concurrent;
using System.Collections.Generic;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Services
{
    public class ApiUsageTracker : IApiUsageTracker
    {
        private readonly ConcurrentQueue<ApiUsageLog> _logs = new ConcurrentQueue<ApiUsageLog>();

        public void LogRequest(ApiUsageLog log)
        {
            _logs.Enqueue(log);
        }

        public IReadOnlyList<ApiUsageLog> DequeueAll()
        {
            var logsToFlush = new List<ApiUsageLog>();
            while (_logs.TryDequeue(out var log))
            {
                logsToFlush.Add(log);
            }
            return logsToFlush;
        }

        public int CurrentCount => _logs.Count;
    }
}
