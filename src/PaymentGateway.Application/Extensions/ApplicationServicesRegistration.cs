using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Application.Commands;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Services;
using PaymentGateway.Application.Validators;

namespace PaymentGateway.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering application services in the dependency injection (DI) container.
    /// </summary>
    public static class ApplicationServicesRegistration
    {
        /// <summary>
        /// Registers all application-related services, validators, and interfaces into the DI container.
        /// This method is used to configure and resolve application services that handle business logic, validation, etc.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> to allow for chaining.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services for business logic. These services will be instantiated and injected when required.
            services.AddScoped<IIdempotencyService, IdempotencyService>(); // Registers IdempotencyService for handling idempotency logic.
            services.AddScoped<IProcessPaymentCommandHandler, ProcessPaymentCommandHandler>(); // Registers ProcessPaymentCommandHandler for processing payment requests.
            services.AddScoped<IPaymentService, PaymentService>(); // Registers PaymentService to manage payment operations.

            // Register FluentValidation validators for validating incoming DTOs (Data Transfer Objects).
            services.AddScoped<IValidator<PostPaymentRequest>, PostPaymentRequestValidator>(); // Registers the validator for PostPaymentRequest to ensure valid data is passed before processing.
            
            return services;
        }
    }
}
