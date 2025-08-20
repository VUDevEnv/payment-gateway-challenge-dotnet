using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Application.UnitTests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _mockPaymentRepository = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<PaymentService>> _mockLogger = new();

        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentService = new PaymentService(
                _mockPaymentRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        #region GetPaymentByIdAsync

        [Fact(DisplayName = "GetPaymentByIdAsync should throw ArgumentException when ID is empty")]
        public async Task GetPaymentByIdAsync_ShouldThrow_ArgumentException_WhenIdIsEmpty()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            Func<Task> act = async () => await _paymentService.GetPaymentByIdAsync(emptyId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("id")
                .WithMessage("Payment ID cannot be empty.*");
        }

        [Fact(DisplayName = "GetPaymentByIdAsync should throw NotFoundException when payment not found")]
        public async Task GetPaymentByIdAsync_ShouldThrow_NotFoundException_WhenPaymentNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockPaymentRepository.Setup(repo => repo.GetPaymentByIdAsync(id))
                .ReturnsAsync((Payment)null!);

            // Act
            Func<Task> act = async () => await _paymentService.GetPaymentByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Payment with ID '{id}' was not found.");
        }

        [Fact(DisplayName = "GetPaymentByIdAsync should return GetPaymentResponse when payment is found")]
        public async Task GetPaymentByIdAsync_ShouldReturn_Response_WhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var payment = CreateSamplePayment();

            var expectedResponse = new GetPaymentResponse
            {
                Id = id,
                CardNumberLastFour = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Currency = "GBP",
                Amount = 1000,
                Status = PaymentStatusDto.Authorized
            };

            _mockPaymentRepository.Setup(r => r.GetPaymentByIdAsync(id))
                .ReturnsAsync(payment);

            _mockMapper.Setup(m => m.Map<GetPaymentResponse>(payment))
                .Returns(expectedResponse);

            // Act
            var result = await _paymentService.GetPaymentByIdAsync(id);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact(DisplayName = "GetPaymentByIdAsync should throw if mapper returns null")]
        public async Task GetPaymentByIdAsync_ShouldThrow_IfMapperReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var payment = CreateSamplePayment();

            _mockPaymentRepository.Setup(r => r.GetPaymentByIdAsync(id))
                .ReturnsAsync(payment);

            _mockMapper.Setup(m => m.Map<GetPaymentResponse>(payment))
                .Returns((GetPaymentResponse)null!); // Simulate mapping failure

            // Act
            Func<Task> act = async () => await _paymentService.GetPaymentByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Could not map payment with ID '{id}' to GetPaymentResponse.");
        }

        #endregion

        #region Helpers

        private static Payment CreateSamplePayment() =>
            new(
                cardNumberLastFour: "1234",
                expiryMonth: 12,
                expiryYear: 2030,
                currency: Currency.GBP,
                amount: 1000,
                cvv: "123",
                authorizationCode: "16c1b0f-2fcd-443e-8475-830459e49be5",
                status: PaymentStatus.Authorized);

        #endregion
    }
}
