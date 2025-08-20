using PaymentGateway.Application.Utilities;

namespace PaymentGateway.Application.UnitTests.Services
{
    public class IdempotencyServiceTests
    {
        private readonly Mock<IIdempotencyRepository> _mockRepository = new();
        private readonly Mock<ILogger<IdempotencyService>> _mockLogger = new();

        private readonly IdempotencyService _service;

        public IdempotencyServiceTests()
        {
            _service = new IdempotencyService(_mockRepository.Object, _mockLogger.Object);
        }

        #region TryGetCachedResponseAsync Input Validation

        [Theory(DisplayName = "TryGetCachedResponseAsync throws if key is null or whitespace")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task TryGetCachedResponseAsync_Throws_OnInvalidKey(string? key)
        {
            var request = CreatePostPaymentRequest();

            Func<Task> act = async () =>
                await _service.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, key!);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("idempotencyKey");
        }

        [Fact(DisplayName = "TryGetCachedResponseAsync throws ArgumentNullException when request is null")]
        public async Task TryGetCachedResponseAsync_Throws_WhenRequestIsNull()
        {
            const string key = "39f7a4cd-9ed4-409d-846f-7b6765f8429f";

            Func<Task> act = async () =>
                await _service.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(null!, key);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }

        #endregion

        #region TryGetCachedResponseAsync Behavior

        [Fact(DisplayName = "TryGetCachedResponseAsync returns cached response if hash matches")]
        public async Task TryGetCachedResponseAsync_ReturnsCachedResponse_WhenHashMatches()
        {
            var request = CreatePostPaymentRequest();
            const string idempotencyKey = "21d82e1c-1509-4a18-9ce6-93c79b378825";
            string hash = RequestHasher.ComputeHash(request);

            var expectedResponse = new PostPaymentResponse { Id = Guid.NewGuid(), Status = PaymentStatusDto.Authorized };

            var record = new IdempotencyRecord<PostPaymentResponse>
            {
                RequestHash = hash,
                Response = expectedResponse
            };

            _mockRepository.Setup(r => r.GetRecordAsync<PostPaymentResponse>(idempotencyKey))
                           .ReturnsAsync(record);

            (PostPaymentResponse? response, string? returnedHash) =
                await _service.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey);

            response.Should().BeEquivalentTo(expectedResponse);
            returnedHash.Should().Be(hash);
        }

        [Fact(DisplayName = "TryGetCachedResponseAsync throws when hash does not match")]
        public async Task TryGetCachedResponseAsync_Throws_WhenHashMismatch()
        {
            var request = CreatePostPaymentRequest();
            const string idempotencyKey = "07674671-bd4e-41f6-9db1-3033c16dd17f";

            var record = new IdempotencyRecord<PostPaymentResponse>
            {
                RequestHash = "different-hash",
                Response = new PostPaymentResponse()
            };

            _mockRepository.Setup(r => r.GetRecordAsync<PostPaymentResponse>(idempotencyKey))
                           .ReturnsAsync(record);

            Func<Task> act = async () =>
                await _service.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Idempotency-Key has already been used with a different request payload.");
        }

        [Fact(DisplayName = "TryGetCachedResponseAsync returns null if no record found")]
        public async Task TryGetCachedResponseAsync_ReturnsNull_WhenNoRecordExists()
        {
            var request = CreatePostPaymentRequest();
            const string idempotencyKey = "a3f24b27-0af3-4f2d-bf83-2cca5300462e";

            _mockRepository.Setup(r => r.GetRecordAsync<PostPaymentResponse>(idempotencyKey))
                           .ReturnsAsync((IdempotencyRecord<PostPaymentResponse>?)null);

            (PostPaymentResponse? response, string? hash) =
                await _service.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey);

            response.Should().BeNull();
            hash.Should().NotBeNullOrWhiteSpace(); // hash is still computed
        }

        #endregion

        #region SaveResponseAsync Input Validation

        [Theory(DisplayName = "SaveResponseAsync throws on if key is null or whitespace")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task SaveResponseAsync_Throws_OnInvalidKey(string? key)
        {
            var response = new PostPaymentResponse();
            var hash = "hash";

            Func<Task> act = async () => await _service.SaveResponseAsync(key!, response, hash);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("idempotencyKey");
        }

        [Theory(DisplayName = "SaveResponseAsync throws on null or whitespace hash")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task SaveResponseAsync_Throws_OnInvalidHash(string? hash)
        {
            var response = new PostPaymentResponse();
            const string key = "2e2d9526-0657-4036-8c83-5794f48fdfd2";

            Func<Task> act = async () => await _service.SaveResponseAsync(key, response, hash!);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("requestHash");
        }

        [Fact(DisplayName = "SaveResponseAsync throws on null response")]
        public async Task SaveResponseAsync_Throws_OnNullResponse()
        {
            const string key = "15aceaeb-4fb7-4f80-a14c-b009c3d7aa2a";
            const string hash = "hash";

            Func<Task> act = async () => await _service.SaveResponseAsync<PostPaymentResponse>(key, null!, hash);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("response");
        }

        #endregion

        #region SaveResponseAsync Behavior

        [Fact(DisplayName = "SaveResponseAsync stores the response with request hash")]
        public async Task SaveResponseAsync_SavesCorrectly()
        {
            const string key = "15aceaeb-4fb7-4f80-a14c-b009c3d7aa2a";
            var response = new PostPaymentResponse { Id = Guid.NewGuid(), Status = PaymentStatusDto.Authorized };
            const string requestHash = "somehash";

            await _service.SaveResponseAsync(key, response, requestHash);

            _mockRepository.Verify(r => r.SaveRecordAsync(key, response, requestHash, null), Times.Once);
        }

        [Fact(DisplayName = "SaveResponseAsync forwards TTL To repository")]
        public async Task SaveResponseAsync_ForwardsTtlToRepository()
        {
            const string key = "d7967eff-d733-4ad4-a34d-4a308775c97c";
            var response = new PostPaymentResponse { Id = Guid.NewGuid(), Status = PaymentStatusDto.Authorized };
            const string hash = "req-hash";
            var ttl = TimeSpan.FromMinutes(5);

            await _service.SaveResponseAsync(key, response, hash, ttl);

            _mockRepository.Verify(r => r.SaveRecordAsync(key, response, hash, ttl), Times.Once);
        }

        [Fact(DisplayName = "SaveResponseAsync logs saving operation")]
        public async Task SaveResponseAsync_Logs_Information()
        {
            const string key = "d7967eff-d733-4ad4-a34d-4a308775c97c";
            const string hash = "hash";
            var response = new PostPaymentResponse();

            await _service.SaveResponseAsync(key, response, hash);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saving idempotency response")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Helpers

        private static PostPaymentRequest CreatePostPaymentRequest() => new()
        {
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Cvv = "123",
            Currency = "GBP",
            Amount = 100
        };

        #endregion
    }
}
