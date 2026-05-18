using Microsoft.AspNetCore.Mvc;

namespace AgrochemAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    // GET: api/test
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "API работает!", time = DateTime.Now });
    }
}