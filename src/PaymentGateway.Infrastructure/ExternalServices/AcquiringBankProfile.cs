using AutoMapper;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Infrastructure.ExternalServices
{
    /// <summary>
    /// Represents the AutoMapper configuration profile for mapping between 
    /// domain models (BankRequest, BankResponse) and external service models 
    /// (AcquiringBankRequest, AcquiringBankResponse).
    /// </summary>
    public class AcquiringBankProfile : Profile
    {
        /// <summary>
        /// Constructor that defines the object mappings for AutoMapper.
        /// </summary>
        public AcquiringBankProfile()
        {
            // Map from BankRequest (domain model) to AcquiringBankRequest (external service model)
            CreateMap<BankRequest, AcquiringBankRequest>();

            // Map from AcquiringBankResponse (external service model) to BankResponse (domain model)
            CreateMap<AcquiringBankResponse, BankResponse>();
        }
    }
}