using AutoMapper;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.Interfaces.Repositories;

namespace PaymentGateway.Application.Services
{
    public class PaymentService(
        IPaymentRepository paymentRepository,
        IMapper mapper,
        ILogger<PaymentService> logger) : IPaymentService
    {
        /// <summary>
        /// Retrieves payment details by ID, handling any necessary validation and mapping.
        /// </summary>
        /// <param name="id">The unique identifier for the payment.</param>
        /// <returns>A <see cref="Task{GetPaymentResponse}"/> representing the asynchronous operation, with a <see cref="GetPaymentResponse"/> containing payment details.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="id"/> is <see cref="Guid.Empty"/>(an invalid GUID).</exception>
        /// <exception cref="NotFoundException">Thrown if no payment is found with the provided <paramref name="id"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the mapping from <see cref="Payment"/> to <see cref="GetPaymentResponse"/> fails.</exception>
        public async Task<GetPaymentResponse> GetPaymentByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                logger.LogWarning("Invalid payment identifier provided: {PaymentId}. The ID cannot be empty", id);
                throw new ArgumentException("Payment ID cannot be empty.", nameof(id));
            }
            
            logger.LogInformation("Fetching payment with ID: {PaymentId}", id);

            var payment = await paymentRepository.GetPaymentByIdAsync(id);

            if (payment == null)
            {
                logger.LogWarning("No payment found with ID: {PaymentId}", id);
                throw new NotFoundException($"Payment with ID '{id}' was not found.");
            }
            
            var getPaymentResponse = mapper.Map<GetPaymentResponse>(payment);

            if (getPaymentResponse == null)
            {
                logger.LogError("Mapping failed: Mapper returned null for payment ID {PaymentId}.", id);
                throw new InvalidOperationException($"Could not map payment with ID '{id}' to GetPaymentResponse.");
            }
            
            return getPaymentResponse;
        }
    }
}
