using Microsoft.AspNetCore.Mvc;

namespace Intellinspect.Backend.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { 
            message = "Intellinspect Backend API is running", 
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            endpoints = new {
                swagger = "/swagger",
                api = "/api"
            }
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow 
        });
    }

    [HttpGet("api")]
    public IActionResult ApiInfo()
    {
        return Ok(new { 
            message = "Intellinspect API", 
            version = "1.0.0",
            endpoints = new string[] {
                "/api/upload",
                "/api/dataset", 
                "/api/training",
                "/api/simulation"
            }
        });
    }
}
