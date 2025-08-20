using System.Text.Json.Serialization;
using PaymentGateway.Application.DTOs.Enums;

namespace PaymentGateway.Application.DTOs.Responses;

public class PostPaymentResponse
{
    public Guid Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatusDto Status { get; set; }

    public string CardNumberLastFour { get; set; } 
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }
}

