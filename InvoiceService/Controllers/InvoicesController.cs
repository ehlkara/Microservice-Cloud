using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using InvoiceService.Configuration;
using InvoiceService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Shared.Utils;

namespace InvoiceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IMongoCollection<Invoice> _invoices;
        private readonly RabbitMqSettings _rabbitMQSettings;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IOptions<MongoDbSettings> mongoDbSettings,
            IOptions<RabbitMqSettings> rabbitMQSettings,
            ILogger<InvoicesController> logger)
        {
            _logger = logger;
            _logger.LogInformation("InvoicesController başlatıldı");
            var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _invoices = database.GetCollection<Invoice>("Invoices");
            _rabbitMQSettings = rabbitMQSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoices.Find(_ => true).ToListAsync();
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var invoice = await _invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            invoice.CreatedAt = DateTime.UtcNow;
            await _invoices.InsertOneAsync(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Invoice updatedInvoice)
        {
            var existingInvoice = await _invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (existingInvoice == null) return NotFound();

            updatedInvoice.Id = id;
            updatedInvoice.CreatedAt = existingInvoice.CreatedAt;

            var result = await _invoices.ReplaceOneAsync(i => i.Id == id, updatedInvoice);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _invoices.DeleteOneAsync(i => i.Id == id);
            if (result.DeletedCount == 0) return NotFound();
            return NoContent();
        }
    }
} 