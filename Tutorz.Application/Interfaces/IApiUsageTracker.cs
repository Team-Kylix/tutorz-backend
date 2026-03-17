using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IApiUsageTracker
    {
        void LogRequest(ApiUsageLog log);
    }
}
