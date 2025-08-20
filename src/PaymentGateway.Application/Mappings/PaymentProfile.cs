using AutoMapper;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Mappings
{
    /// <summary>
    /// AutoMapper profile configuration for mapping between domain entity (Payment)
    /// and DTOs (PostPaymentResponse, GetPaymentResponse).
    /// </summary>
    public class PaymentProfile : Profile
    {
        /// <summary>
        /// Constructor that defines the object mappings for AutoMapper.
        /// </summary>
        public PaymentProfile()
        {
            // Map from Payment (domain entity) to PostPaymentResponse (DTO for post-payment details)
            CreateMap<Payment, PostPaymentResponse>();

            // Map from Payment (domain entity) to GetPaymentResponse (DTO for payment retrieval)
            CreateMap<Payment, GetPaymentResponse>();
        }
    }
}