using Newtonsoft.Json.Linq;

namespace PaymentGateway.Api.IntegrationTests.Controllers
{
    public class PaymentsControllerTests(PaymentGatewayApiFactory factory) : IClassFixture<PaymentGatewayApiFactory>
    {
        private readonly HttpClient _client = factory.Client;

        #region Tests - GET /Payments/{id}

        [Theory(DisplayName = "GetPaymentById returns expected result based on input")]
        [MemberData(nameof(GetPaymentByIdTestData))]
        public async Task GetPaymentById_ReturnsExpectedResult(
            string requestUrl,
            HttpStatusCode expectedStatus,
            Guid? expectedPaymentId,
            string expectedErrorMessage,
            PaymentStatusDto? expectedPaymentStatus)
        {
            // Arrange
            // (Handled via test parameters)

            // Act
            var response = await _client.GetAsync(requestUrl);

            // Assert
            response.StatusCode.Should().Be(expectedStatus);

            if (expectedStatus == HttpStatusCode.OK)
            {
                var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();
                paymentResponse.Should().NotBeNull();

                paymentResponse.Id.Should().Be((Guid)expectedPaymentId!);
                paymentResponse.Status.Should().Be(expectedPaymentStatus);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    var json = JObject.Parse(content);
                    var actualDetail = json["detail"]?.ToString();
                    actualDetail.Should().Be(expectedErrorMessage);
                }
                else
                {
                    expectedErrorMessage.Should().BeNullOrEmpty();
                }
            }
        }

        #endregion

        #region TestData - GetPaymentById

        public static IEnumerable<object[]> GetPaymentByIdTestData =>
            new List<object[]>
            {
                // Valid payment ID (Authorized)
                new object[]
                {
                    "Payments/11111111-1111-1111-1111-111111111111",
                    HttpStatusCode.OK,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    null!,
                    PaymentStatusDto.Authorized
                },

                // Payment not found
                new object[]
                {
                    "Payments/00000000-0000-0000-0000-00000000beef", // Trigger for NotFoundException
                    HttpStatusCode.NotFound,
                    null!,
                    "Payment with ID '00000000-0000-0000-0000-00000000beef' not found.",
                    null!
                },

                // Invalid (empty Guid)
                new object[]
                {
                    "Payments/00000000-0000-0000-0000-000000000000", // Trigger for ArgumentException
                    HttpStatusCode.BadRequest,
                    null!,
                    "Payment ID cannot empty. (Parameter 'id')",
                    null!
                },

                // Simulated server error
                new object[]
                {
                    "Payments/00000000-0000-0000-0000-00000000dead", // Trigger for server error 
                    HttpStatusCode.InternalServerError,
                    null!,
                    "Unhandled exception occurred.",
                    null!
                },

                // Malformed GUID
                new object[]
                {
                    "Payments/this-is-not-a-guid",
                    HttpStatusCode.NotFound,
                    null!,
                    null!,
                    null!
                }
            };

        #endregion

        #region Tests - POST /Payments
        [Theory(DisplayName = "ProcessPayment returns expected result based on input")]
        [MemberData(nameof(ProcessPaymentTestData))]
        public async Task ProcessPayment_ReturnsExpectedResult(
            PostPaymentRequest paymentRequest,
            HttpStatusCode expectedStatus,
            PaymentStatusDto? expectedStatusDto,
            string? expectedErrorMessage)
        {
            // Arrange
            // (Handled via test parameters)

            // Act
            var response = await _client.PostAsJsonAsync("Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(expectedStatus);

            if (expectedStatus == HttpStatusCode.Created)
            {
                var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
                paymentResponse.Should().NotBeNull();
                paymentResponse.Status.Should().Be(expectedStatusDto);
                paymentResponse.CardNumberLastFour.Should().Be(paymentRequest.CardNumber[^4..]);
                paymentResponse.Amount.Should().Be(paymentRequest.Amount);
                paymentResponse.Currency.Should().Be(paymentRequest.Currency);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var actualDetail = json["detail"]?.ToString() ?? json["message"]?.ToString();
                actualDetail.Should().Be(expectedErrorMessage);
            }
        }
        #endregion

        #region TestData - ProcessPayment

        public static IEnumerable<object[]> ProcessPaymentTestData => new List<object[]>
        {
            // Successful Payment - Authorized
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "4111111111111234",
                    ExpiryMonth = 12,
                    ExpiryYear = 2030,
                    Cvv = "123",
                    Currency = "GBP",
                    Amount = 1000
                },
                HttpStatusCode.Created,
                PaymentStatusDto.Authorized,
                null!
            },

            // Invalid Card Number (non-numeric)
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "abcd1234efgh5678",
                    ExpiryMonth = 12,
                    ExpiryYear = 2030,
                    Cvv = "123",
                    Currency = "GBP",
                    Amount = 1000
                },
                HttpStatusCode.BadRequest,
                null!,
                "Card number must contain only numeric digits."
            },

            // Expired Card (year in past)
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "4111111111111234",
                    ExpiryMonth = 12,
                    ExpiryYear = 2020,
                    Cvv = "123",
                    Currency = "GBP",
                    Amount = 1000
                },
                HttpStatusCode.BadRequest,
                null!,
                "Expiry year must be the current year or later."
            },

            // Invalid CVV (not 3 digits)
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "4111111111111234",
                    ExpiryMonth = 12,
                    ExpiryYear = 2030,
                    Cvv = "12",
                    Currency = "GBP",
                    Amount = 1000
                },
                HttpStatusCode.BadRequest,
                null!,
                "CVV must be 3 or 4 digits long."
            },

            // Invalid Amount (zero)
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "4111111111111234",
                    ExpiryMonth = 12,
                    ExpiryYear = 2030,
                    Cvv = "123",
                    Currency = "GBP",
                    Amount = 0
                },
                HttpStatusCode.BadRequest,
                null!,
                "Amount must be greater than zero."
            },

            // Simulate server error
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "9999999999999999", // Trigger value
                    ExpiryMonth = 12,
                    ExpiryYear = 2030,
                    Cvv = "123",
                    Currency = "GBP",
                    Amount = 1000
                },
                HttpStatusCode.InternalServerError,
                null!,
                "Unhandled exception occurred."
            },

            // Declined payment
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "4000000000000002", // Declined scenario
                    ExpiryMonth = 12,
                    ExpiryYear = 2030,
                    Cvv = "123",
                    Currency = "GBP",
                    Amount = 1000
                },
                HttpStatusCode.Created,
                PaymentStatusDto.Declined,
                null!
            },
        };

        #endregion

    }
}
