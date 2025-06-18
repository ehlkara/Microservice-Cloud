using OrderService.Configuration;
using OrderService.Services;
using Prometheus;
using Shared.Utils.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Configure RabbitMQ
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<RabbitMqClient>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure port
// builder.WebHost.UseUrls("http://*:5000");

// Add Prometheus metrics
builder.Services.AddMetricServer(options =>
{
    options.Port = 9090;
});

// Background RabbitMQ consumer
builder.Services.AddHostedService<OrderService.Services.MemberRegisteredConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start consuming messages
var rabbitMqClient = app.Services.GetRequiredService<RabbitMqClient>();
rabbitMqClient.ConsumeInvoiceRequests();

// Enable Prometheus metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();

// Initialize metrics
MetricsRegistry.QueueDepth.WithLabels("order_queue").Set(0);
MetricsRegistry.ActiveNodes.WithLabels("order_service").Set(1);

await app.RunAsync();