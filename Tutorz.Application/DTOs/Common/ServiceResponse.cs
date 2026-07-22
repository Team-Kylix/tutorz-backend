using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tutorz.Application.DTOs.Common
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();

        public static ServiceResponse<T> SuccessResponse(T data, string message = "")
        {
            return new ServiceResponse<T> { Data = data, Success = true, Message = message };
        }

        public static ServiceResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ServiceResponse<T> { Success = false, Message = message, Errors = errors ?? new List<string>() };
        }
    }

    public class BatchOperationResponse
    {
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public int RemainingCount { get; set; }
        public bool IsComplete { get; set; }
    }
}
