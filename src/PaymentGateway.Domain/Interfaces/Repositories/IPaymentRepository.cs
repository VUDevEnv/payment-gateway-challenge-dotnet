using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository responsible for managing payment records.
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// Saves a payment record in the repository.
        /// </summary>
        /// <param name="payment">The payment record to save.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SavePaymentAsync(Payment payment);

        /// <summary>
        /// Retrieves a payment record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the payment to retrieve.</param>
        /// <returns>A <see cref="Task{Payment}"/> representing the asynchronous operation, with the payment if found, otherwise <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="id"/> is <see cref="Guid.Empty"/>(an invalid GUID).</exception>
        Task<Payment?> GetPaymentByIdAsync(Guid id);
    }
}