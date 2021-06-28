using System.Diagnostics;
using System.Text;
using Flogger.Core.Helpers;
using Flogger.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Flogger.Core
{
    public static class Extension
    {
        public static IConfiguration StaticConfig { get; private set; }

        public static IServiceCollection AddFloggerCore(this IServiceCollection services,
            IConfiguration configuration)
        {
            StaticConfig = configuration;
            return services;
        }

        public static IApplicationBuilder UseFloggerCore(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseExceptionHandler(eApp =>
            {
                eApp.Run(async context =>
                {
                    context.Response.StatusCode = configuration.GetValue("FloggerCore:ErrorCode", 500);
                    context.Response.ContentType =
                        configuration.GetValue("FloggerCore:ContentType", "application/json");

                    var errorCtx = context.Features.Get<IExceptionHandlerFeature>();
                    if (errorCtx != null)
                    {
                        var ex = errorCtx.Error;
                        WebHelper.LogWebError(configuration.GetValue("FloggerCore:Product", "Default API Services"),
                            configuration.GetValue("FloggerCore:Layer", "Default API"), ex, context);

                        var errorId = Activity.Current?.Id ?? context.TraceIdentifier;
                        var jsonResponse = JsonConvert.SerializeObject(new CustomErrorResponse
                        {
                            ErrorId = errorId,
                            Message = configuration.GetValue("FloggerCore:Display",
                                "Some kind of error happened in the API.")
                        });
                        await context.Response.WriteAsync(jsonResponse, Encoding.UTF8);
                    }
                });
            });

            return app;
        }
    }
}