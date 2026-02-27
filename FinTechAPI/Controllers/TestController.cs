using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [Route("hello")]
        public IActionResult GetHelloMessage()
        {
            return Ok(new { message = "Hello from FinTech API!" });
        }

        [HttpGet]
        [Route("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { 
                status = "API is running successfully!", 
                version = "1.0.0",
                server = Environment.MachineName,
                uptime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        [HttpPost]
        [Route("echo")]
        public IActionResult EchoMessage([FromBody] TestMessageDto message)
        {
            return Ok(new { 
                originalMessage = message.Text,
                echoMessage = $"Echo: {message.Text}",
                receivedAt = DateTime.Now
            });
        }
    }

    public class TestMessageDto
    {
        public string Text { get; set; } = string.Empty;
    }
}