using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Exceptions;
using System.Text.RegularExpressions;
using FluentValidation;

namespace PaymentGateway.Api.IntegrationTests
{
    public class PaymentGatewayApiFactory : WebApplicationFactory<Program>
    {
        private readonly Mock<IPaymentService> _mockPaymentService = new();
        private readonly Mock<IProcessPaymentCommandHandler> _mockProcessHandler = new();

        private static readonly Guid NotFoundId = Guid.Parse("00000000-0000-0000-0000-00000000beef");
        private static readonly Guid ServerErrorId = Guid.Parse("00000000-0000-0000-0000-00000000dead");
        private static readonly Guid KnownGoodId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private HttpClient? _client;

        public HttpClient Client => _client ??= CreateClientWithIdempotencyKey();

        private HttpClient CreateClientWithIdempotencyKey()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost/api/v1/")
            });

            // Add the Idempotency-Key header
            client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            builder.ConfigureTestServices(services =>
            {
                #region PaymentService - GetPaymentByIdAsync

                _mockPaymentService
                    .Setup(service => service.GetPaymentByIdAsync(It.IsAny<Guid>()))
                    .Returns<Guid>(id =>
                    {
                        if (id == Guid.Empty)
                        {
                            return Task.FromException<GetPaymentResponse>(
                                new ArgumentException("Payment ID cannot empty.", nameof(id)));
                        }

                        if (id == NotFoundId)
                        {
                            return Task.FromException<GetPaymentResponse>(
                                new NotFoundException($"Payment with ID '{id}' not found."));
                        }

                        if (id == ServerErrorId)
                        {
                            return Task.FromException<GetPaymentResponse>(
                                new InvalidOperationException("Unhandled exception occurred."));
                        }

                        if (id == KnownGoodId)
                        {
                            return Task.FromResult(new GetPaymentResponse
                            {
                                Id = id,
                                Status = PaymentStatusDto.Authorized,
                                Amount = 1000,
                                Currency = "GBP",
                                CardNumberLastFour = "1234",
                                ExpiryMonth = 12,
                                ExpiryYear = 2030
                            });
                        }

                        return Task.FromException<GetPaymentResponse>(
                            new NotFoundException($"Payment with ID '{id}' not found."));
                    });

                #endregion

                #region ProcessPaymentCommandHandler - HandleAsync

                _mockProcessHandler
                    .Setup(handler =>
                        handler.HandleAsync(It.IsAny<PostPaymentRequest>(), It.IsAny<string>()))
                    .Returns<PostPaymentRequest, string>((request, _) =>
                    {
                        // Validation: Card number must be numeric and 14–19 digits
                        if (string.IsNullOrWhiteSpace(request.CardNumber) ||
                            !Regex.IsMatch(request.CardNumber, @"^\d{14,19}$"))
                        {
                            throw new ValidationException("Card number must contain only numeric digits.");
                        }

                        var now = DateTime.UtcNow;
                        if (request.ExpiryYear < now.Year ||
                            (request.ExpiryYear == now.Year && request.ExpiryMonth < now.Month))
                        {
                            throw new ValidationException("Expiry year must be the current year or later.");
                        }

                        if (string.IsNullOrWhiteSpace(request.Cvv) || !Regex.IsMatch(request.Cvv, @"^\d{3}$"))
                        {
                            throw new ValidationException("CVV must be 3 or 4 digits long.");
                        }

                        if (request.Amount <= 0)
                        {
                            throw new ValidationException("Amount must be greater than zero.");
                        }

                        if (request.CardNumber == "9999999999999999")
                        {
                            throw new InvalidOperationException("Unhandled exception occurred.");
                        }

                        var isDeclined = request.CardNumber == "4000000000000002";

                        var response = new PostPaymentResponse
                        {
                            Id = Guid.NewGuid(),
                            CardNumberLastFour = request.CardNumber[^4..],
                            ExpiryMonth = request.ExpiryMonth,
                            ExpiryYear = request.ExpiryYear,
                            Currency = request.Currency,
                            Amount = request.Amount,
                            Status = isDeclined ? PaymentStatusDto.Declined : PaymentStatusDto.Authorized
                        };

                        return Task.FromResult(response);
                    });

                #endregion

                #region Register Mocks in DI

                services.AddScoped(_ => _mockPaymentService.Object);
                services.AddScoped(_ => _mockProcessHandler.Object);

                #endregion
            });
        }
    }
}
