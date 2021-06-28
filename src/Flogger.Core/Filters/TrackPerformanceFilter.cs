using System.Collections.Generic;
using Flogger.Core.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Flogger.Core.Filters
{
    public class TrackPerformanceFilter : IActionFilter
    {
        private readonly string _layer;
        private readonly string _product;
        private PerfTracker _tracker;

        public TrackPerformanceFilter(string product, string layer)
        {
            _product = product;
            _layer = layer;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;
            var activity = $"{request.Path}-{request.Method}";

            var dict = new Dictionary<string, object>();
            if (context.RouteData.Values?.Keys != null)
                foreach (var key in context.RouteData.Values?.Keys)
                    dict.Add($"RouteData-{key}", (string) context.RouteData.Values[key]);

            var details = WebHelper.GetWebFlogDetail(_product, _layer, activity,
                context.HttpContext, dict);

            _tracker = new PerfTracker(details);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _tracker?.Stop();
        }
    }
}