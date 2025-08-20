using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Repositories;

namespace PaymentGateway.Infrastructure.Repositories
{
    /// <summary>
    /// A repository for managing payment records.
    /// Provides functionality to save payments and retrieve payments by ID.
    /// </summary>
    public class PaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _payments = new();

        /// <summary>
        /// Saves a payment record in the repository.
        /// </summary>
        /// <param name="payment">The payment record to save.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="payment"/> is <c>null</c>.</exception>
        public Task SavePaymentAsync(Payment payment)
        {
            if (payment == null)
            {
                // Log or throw an exception for invalid input (null payment)
                throw new ArgumentNullException(nameof(payment), "New payment cannot be null");
            }

            _payments.Add(payment);
            return Task.CompletedTask;  // Return completed task, as we're simulating in-memory storage
        }

        /// <summary>
        /// Retrieves a payment record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the payment to retrieve.</param>
        /// <returns>A <see cref="Task{Payment}"/> representing the asynchronous operation, with the payment if found, otherwise <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="id"/> is <see cref="Guid.Empty"/>(an invalid GUID).</exception>
        public Task<Payment?> GetPaymentByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                // Log or throw an exception for invalid input (empty ID)
                throw new ArgumentException("Payment ID cannot be empty.", nameof(id));
            }

            // Simulate fetching from an in-memory list (replace with actual DB logic)
            return Task.FromResult(_payments.FirstOrDefault(p => p.Id == id));
        }
    }
}
