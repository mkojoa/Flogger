using System.Collections.Generic;
using Flogger.Core.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Flogger.Core.Filters
{
    public class TrackUsageAttribute : ActionFilterAttribute
    {
        private readonly string _activityName;
        private readonly string _layer;
        private readonly string _product;

        public TrackUsageAttribute(string product, string layer, string activityName)
        {
            _product = product;
            _layer = layer;
            _activityName = activityName;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var dict = new Dictionary<string, object>();
            if (context.RouteData.Values?.Keys != null)
                foreach (var key in context.RouteData.Values?.Keys)
                    dict.Add($"RouteData-{key}", (string) context.RouteData.Values[key]);

            WebHelper.LogWebUsage(_product, _layer, _activityName, context.HttpContext, dict);
        }
    }
}