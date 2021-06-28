using System.Collections.Generic;
using Flogger.Core.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Flogger.Core.Filters
{
    public class TrackUsageFilter : IActionFilter
    {
        private readonly string _layer;
        private readonly string _product;

        public TrackUsageFilter(string product, string layer)
        {
            _product = product;
            _layer = layer;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var request = context.HttpContext.Request;
            var activity = $"{request.Path}-{request.Method}";

            var dict = new Dictionary<string, object>();
            if (context.RouteData.Values?.Keys != null)
            {
                foreach (var key in context.RouteData.Values?.Keys)
                    dict.Add($"RouteData-{key}", (string) context.RouteData.Values[key]);

                WebHelper.LogWebUsage(_product, _layer, activity, context.HttpContext, dict);
            }
        }
    }
}