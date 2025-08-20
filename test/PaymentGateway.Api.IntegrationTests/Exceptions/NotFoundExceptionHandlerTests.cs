namespace PaymentGateway.Api.IntegrationTests.Exceptions
{
    public class NotFoundExceptionHandlerTests(PaymentGatewayApiFactory factory) : IClassFixture<PaymentGatewayApiFactory>
    {
        private readonly HttpClient _client = factory.Client;

        [Fact(DisplayName = "NotFoundExceptionHandler When Payment Not Found Should Return Problem Details")]
        public async Task NotFoundExceptionHandler_WhenPaymentNotFound_ShouldReturnProblemDetails()
        {
            // Arrange
            const string id = "00000000-0000-0000-0000-00000000beef"; // Trigger for NotFoundException

            // Act
            var response = await _client.GetAsync($"payments/{id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails.Title.Should().Be("Resource Not Found");
            problemDetails.Detail.Should().Be($"Payment with ID '{id}' not found.");
            problemDetails.Status.Should().Be((int)HttpStatusCode.NotFound);
            problemDetails.Type.Should().Be("NotFoundException");
        }
    }
}