using System;
using Flogger.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Flog.WebJsTest.Controllers
{
    [ApiController]
    [Route("api/v1/flogger-core")]
    public class FloggerCoreController : ControllerBase
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        
        [HttpPost("diagnostic")]
        public void LogDiagnostic([FromBody] FlogDetail flogDetail)
        {
            Flogger.Core.Helpers.Flogger.WriteDiagnostic(flogDetail);
        }


        [HttpPost("errors")]
        public void LogError([FromBody] FlogDetail flogDetail)
        {
            Flogger.Core.Helpers.Flogger.WriteError(flogDetail);
        }


        [HttpPost("usages")]
        public void LogUsages([FromBody] FlogDetail flogDetail)
        {
            Flogger.Core.Helpers.Flogger.WriteUsage(flogDetail);
        }


        [HttpPost("performance")]
        public void LogPerformance([FromBody] FlogDetail flogDetail)
        {
            Flogger.Core.Helpers.Flogger.WritePerf(flogDetail);
        }

        [HttpGet("get-api-messages")]
        public IActionResult GetApiMessages()
        {
            return Ok(Summaries);
        }
        
        [HttpPut("get-api-messages-update")]
        public IActionResult GetApiMessagesUpdate([FromBody] MessageDto data)
        {
            if (data == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity();
            }
            
            return Ok(data);
        }
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public int Age { get; set; }
        public string UserName { get; set; }
        public string Location { get; set; }
        public string Phone { get; set; }
    }
}