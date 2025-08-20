using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentGateway.Application.Utilities
{
    public static class RequestHasher
    {
        public static string ComputeHash<TRequest>(TRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
