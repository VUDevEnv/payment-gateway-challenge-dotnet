using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Infrastructure.Repositories;

namespace PaymentGateway.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for registering Infrastructure layer services in the dependency injection (DI) container.
    /// These services typically deal with data access, external integrations, and system-level concerns.
    /// </summary>
    public static class InfrastructureServicesRegistration
    {
        /// <summary>
        /// Registers all services and dependencies related to the Infrastructure layer in the DI container.
        /// This includes repositories that handle data access and other infrastructure-specific services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> to allow method chaining.</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Register repositories that handle data access and are part of the Infrastructure layer.
            // Repositories are added with a singleton lifetime to ensure a single instance throughout the application's lifespan.
            services.AddSingleton<IIdempotencyRepository, IdempotencyRepository>(); // Registers the IdempotencyRepository for managing idempotency records.
            services.AddSingleton<IPaymentRepository, PaymentRepository>(); // Registers the PaymentRepository for managing payment data.
            
            return services;
        }
    }
}