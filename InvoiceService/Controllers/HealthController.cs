using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using InvoiceService.Configuration;

namespace InvoiceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly MongoDbSettings _mongoDbSettings;

        public HealthController(IOptions<MongoDbSettings> mongoDbSettings)
        {
            _mongoDbSettings = mongoDbSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            try
            {
                var client = new MongoClient(_mongoDbSettings.ConnectionString);
                var database = client.GetDatabase(_mongoDbSettings.DatabaseName);
                
                // Test connection
                await database.RunCommandAsync((Command<dynamic>)"{ping:1}");
                
                return Ok(new { Status = "Healthy", Message = "MongoDB connection is successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Unhealthy", Message = $"MongoDB connection failed: {ex.Message}" });
            }
        }
    }
} 