using AutoMapper;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Application.Mappings
{
    /// <summary>
    /// AutoMapper profile configuration for mapping between DTOs (PostPaymentRequest)
    /// and domain models (BankRequest).
    /// </summary>
    public class BankProfile : Profile
    {
        /// <summary>
        /// Constructor that defines the object mappings for AutoMapper.
        /// </summary>
        public BankProfile()
        {
            // Map from PostPaymentRequest (DTO) to BankRequest (domain model)
            CreateMap<PostPaymentRequest, BankRequest>()
                // Explicitly map the ExpiryDate field by formatting the ExpiryMonth and ExpiryYear as MM/YY
                .ForMember(dest => dest.ExpiryDate,
                    opt => opt.MapFrom(src =>
                        $"{src.ExpiryMonth:D2}/{src.ExpiryYear}")); // Ensure two-digit month and year format
        }
    }
}