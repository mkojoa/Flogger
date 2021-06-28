using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Flogger.Core.Middleware
{
    public static class CustomExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(
            this IApplicationBuilder builder, string product, string layer,
            string errorHandlingPath)
        {
            return builder.UseMiddleware<CustomExceptionHandlerMiddleware>
            (product, layer, Options.Create(new ExceptionHandlerOptions
            {
                ExceptionHandlingPath = new PathString(errorHandlingPath)
            }));
        }
    }
}