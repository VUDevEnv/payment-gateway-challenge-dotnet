namespace PaymentGateway.Api.IntegrationTests.Exceptions
{
    public class GlobalExceptionHandlerTests(PaymentGatewayApiFactory factory) : IClassFixture<PaymentGatewayApiFactory>
    {
        private readonly HttpClient _client = factory.Client;

        [Fact(DisplayName = "GlobalExceptionHandler When Unhandled Exception Occurs Should Return Problem Details")]
        public async Task GlobalExceptionHandler_WhenUnhandledExceptionOccurs_ShouldReturnProblemDetails()
        {
            // Arrange
            const string triggerGuid = "00000000-0000-0000-0000-00000000dead"; // Trigger for server error
            
            // Act
            var response = await _client.GetAsync($"payments/{triggerGuid}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("An error occurred");
            problemDetails.Detail.Should().Be("Unhandled exception occurred.");
            problemDetails.Type.Should().Be("InvalidOperationException");
            problemDetails.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact(DisplayName = "ArgumentExceptionHandler When Payment ID Is Empty Should Return Problem Details")]
        public async Task ArgumentExceptionHandler_WhenPaymentIdIsEmpty_ShouldReturnProblemDetails()
        {
            // Arrange
            var id = Guid.Empty; // Trigger for ArgumentException

            // Act
            var response = await _client.GetAsync($"payments/{id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("An error occurred");
            problemDetails.Detail.Should().Be("Payment ID cannot empty. (Parameter 'id')");
            problemDetails.Type.Should().Be("ArgumentException");
            problemDetails.Status.Should().Be((int)HttpStatusCode.BadRequest);
        }
    }
}