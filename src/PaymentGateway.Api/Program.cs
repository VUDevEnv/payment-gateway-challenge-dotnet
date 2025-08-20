using System.Text.Json.Serialization;
using NSwag;
using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Swagger;
using PaymentGateway.Application.Extensions;
using PaymentGateway.Domain.Interfaces.AcquiringBank;
using PaymentGateway.Infrastructure.Extensions;
using PaymentGateway.Infrastructure.ExternalServices;
using PaymentGateway.Infrastructure.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Reads logging configuration from the app's configuration file
    .CreateLogger();

builder.Host.UseSerilog(); // Uses Serilog as the logging provider

builder.Services.AddRateLimitingService(builder.Configuration); // Configures and adds rate limiting services
builder.Services.AddApiVersioningService(builder.Configuration); // Configures and adds API versioning services

// Register problem details for consistent error responses
builder.Services.AddProblemDetails(configure =>
{
    configure.CustomizeProblemDetails = context =>
    {
        // Add the request ID as an extension to the problem details for traceability
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});

builder.Services.AddExceptionHandler<ValidationExceptionHandler>(); // Registers handler for validation exceptions
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>(); // Registers handler for 'NotFound' exceptions
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Registers handler for general exceptions

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configures a JSON converter to convert enums to strings in JSON responses
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Register AutoMapper for object mapping
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); // Scans all assemblies in the current domain for AutoMapper profiles

// Register application and infrastructure services
builder.Services.AddApplicationServices(); // Adds application-specific services like business logic, validation, handler etc.
builder.Services.AddInfrastructureServices(); // Adds infrastructure-specific services like database, external APIs, etc.

builder.Services.Configure<AcquiringBankOptions>(
    builder.Configuration.GetSection("SimulatedAcquiringBank"));
builder.Services.Configure<RetryPolicyOptions>(
    builder.Configuration.GetSection("RetryPolicy"));

// Configure the HttpClient for the IAcquiringBank service with a custom base URL
builder.Services.AddHttpClient<IAcquiringBank, SimulatedAcquiringBank>(client =>
{
    var configSection = builder.Configuration.GetSection("SimulatedAcquiringBank");
    var options = configSection.Get<AcquiringBankOptions>(); 

    if (options is null || string.IsNullOrWhiteSpace(options.BaseUrl))
        throw new InvalidOperationException("Missing or invalid 'SimulatedAcquiringBank' configuration.");

    client.BaseAddress = new Uri(options.BaseUrl); 
})
    .SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Sets the handler lifetime to 5 minutes (for connection pooling)

builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentProcessors.Add(new AlphabeticalSchemaProcessor()); 

    config.PostProcess = document =>
    {
        document.Info = new OpenApiInfo
        {
            Version = "v1", 
            Title = "Payment Gateway API",
            Description = "An ASP.NET Core Web API for managing E-Commerce payments." 
        };
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(); 
    app.UseSwaggerUi(settings =>
    {
        settings.OperationsSorter = "alpha"; 
        settings.TagsSorter = "alpha"; 
    });
}

// Global exception handling middleware (e.g., returns a standardized error response)
app.UseExceptionHandler();

app.UseHttpsRedirection();

// Log incoming HTTP requests using Serilog with a custom message template
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode}"; // Customize log format
});

app.UseAuthorization();

// Enable rate limiting middleware (configures API rate limiting according to policy)
app.UseRateLimiter();

// Map controller routes and apply rate limiting policy
app.MapControllers()
    .RequireRateLimiting(RateLimitingPolicies.FixedWindowPolicy); // Apply the fixed window rate-limiting policy to controller routes

app.Run();

public partial class Program { }
