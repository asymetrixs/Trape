using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Trape.Api.ControlCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;

        public StatusController(ILogger<StatusController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public StatusCodeResult Get()
        {
            return Ok();
        }
    }
}
