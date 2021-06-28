using System;
using System.Collections.Generic;
using Flogger.Core.Filters;
using Flogger.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Flog.WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [TrackUsage("HCM Persol Payroll", "Payroll API", "Salary Grade Fetched")]
        public IEnumerable<WeatherForecast> Get()
        {
            WebHelper.LogWebDiagnostic("HCM Persol Payroll", "Payroll API", "Just checking in here ...", HttpContext,
                new Dictionary<string, object>
                {
                    {"very", "Important Here"}
                });

            throw new Exception("Hi Ex");

            // var rng = new Random();
            // return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            //     {
            //         Date = DateTime.Now.AddDays(index),
            //         TemperatureC = rng.Next(-20, 55),
            //         Summary = Summaries[rng.Next(Summaries.Length)]
            //     })
            //     .ToArray();
        }
    }
}