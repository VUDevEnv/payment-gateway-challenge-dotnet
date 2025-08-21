using Newtonsoft.Json.Linq;
using PaymentGateway.Application.Constants;

namespace PaymentGateway.Api.IntegrationTests.Exceptions
{
    public class ValidationExceptionHandlerTests(PaymentGatewayApiFactory factory) : IClassFixture<PaymentGatewayApiFactory>
    {
        private readonly HttpClient _client = factory.Client;

        [Fact(DisplayName = "ValidationExceptionHandler When Card Name Validation Fails Should Return Problem Details")]
        public async Task ValidationExceptionHandler_WhenCardNameValidationFails_ShouldReturnProblemDetails()
        {
            // Arrange
            var paymentRequest = new PostPaymentRequest()
            {
                Currency = "GBP",
                Cvv = "123",
                Amount = 100,
                CardNumber = "", // Invalid input to trigger FluentValidation
                ExpiryMonth = 09,
                ExpiryYear = 29
            };

            // Act
            var response = await _client.PostAsJsonAsync("Payments", paymentRequest);

            // Assert
            // Status code should be BadRequest
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            // The title should indicate a validation error
            json["title"]!.ToString().Should().Be("Validation Error");

            // The detail should provide the validation error message
            json["detail"]!.ToString().Should().Be(ValidationMessages.CardNumberNumeric);

            // Status code should be 400 (Bad Request)
            json["status"]!.ToString().Should().Be("400");
        }
    }
}
