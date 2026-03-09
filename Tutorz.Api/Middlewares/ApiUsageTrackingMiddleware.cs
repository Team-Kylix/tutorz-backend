using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Api.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Tutorz.Api.Middlewares
{
    public class ApiUsageTrackingMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiUsageTrackingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Execute the rest of the pipeline
            await _next(context);

            // After execution we know the endpoint and the user claims 
            var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
            
            // We only care about checking API usage realistically. We can omit static files and swagger etc.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // We resolve the usage tracker via RequestServices because the tracker is a singleton
                // and we're inside the middleware where services are available
                var usageTracker = context.RequestServices.GetService<IApiUsageTracker>();
                
                if (usageTracker != null)
                {
                    Guid? userId = null;
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(userIdClaim, out var parsedId))
                    {
                        userId = parsedId;
                    }

                    string purpose = "API Request";

                    // Use Reflection to grab the custom ApiPurposeAttribute if applied
                    if (endpoint != null)
                    {
                        var purposeAttribute = endpoint.Metadata.GetMetadata<ApiPurposeAttribute>();
                        if (purposeAttribute != null)
                        {
                            purpose = purposeAttribute.Purpose;
                        }
                    }

                    var log = new ApiUsageLog
                    {
                        UserId = userId, // Could be null if unauthenticated, still logged
                        Endpoint = context.Request.Path,
                        Method = context.Request.Method,
                        Purpose = purpose,
                        Timestamp = DateTime.UtcNow
                    };

                    usageTracker.LogRequest(log);
                }
            }
        }
    }

    public static class ApiUsageTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiUsageTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiUsageTrackingMiddleware>();
        }
    }
}
