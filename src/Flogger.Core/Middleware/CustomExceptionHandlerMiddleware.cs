﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Flogger.Core.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Flogger.Core.Middleware
{
    public sealed class CustomExceptionHandlerMiddleware
    {
        private readonly Func<object, Task> _clearCacheHeadersDelegate;
        private readonly string _layer;
        private readonly RequestDelegate _next;
        private readonly ExceptionHandlerOptions _options;
        private readonly string _product;

        public CustomExceptionHandlerMiddleware(string product, string layer,
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<ExceptionHandlerOptions> options,
            DiagnosticSource diagSource)
        {
            _product = product;
            _layer = layer;

            _next = next;
            _options = options.Value;
            _clearCacheHeadersDelegate = ClearCacheHeaders;
            _options.ExceptionHandler ??= _next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                WebHelper.LogWebError(_product, _layer, ex, context);

                var originalPath = context.Request.Path;
                if (_options.ExceptionHandlingPath.HasValue) context.Request.Path = _options.ExceptionHandlingPath;

                context.Response.Clear();
                var exceptionHandlerFeature = new ExceptionHandlerFeature
                {
                    Error = ex,
                    Path = originalPath.Value
                };

                context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
                context.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
                context.Response.StatusCode = 500;
                context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

                await _options.ExceptionHandler(context);
            }
        }

        private static Task ClearCacheHeaders(object state)
        {
            var response = (HttpResponse) state;
            response.Headers[HeaderNames.CacheControl] = "no-cache";
            response.Headers[HeaderNames.Pragma] = "no-cache";
            response.Headers[HeaderNames.Expires] = "-1";
            response.Headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }
}