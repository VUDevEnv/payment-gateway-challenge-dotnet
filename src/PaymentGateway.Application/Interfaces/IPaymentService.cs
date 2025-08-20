using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Application.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Retrieves payment details by ID, handling any necessary validation and mapping.
        /// </summary>
        /// <param name="id">The unique identifier for the payment.</param>
        /// <returns>A <see cref="Task{GetPaymentResponse}"/> representing the asynchronous operation, with a <see cref="GetPaymentResponse"/> containing payment details.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="id"/> is <see cref="Guid.Empty"/>(an invalid GUID).</exception>
        /// <exception cref="NotFoundException">Thrown if no payment is found with the provided <paramref name="id"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the mapping from <see cref="Payment"/> to <see cref="GetPaymentResponse"/> fails.</exception>
        Task<GetPaymentResponse> GetPaymentByIdAsync(Guid id);
    }
}