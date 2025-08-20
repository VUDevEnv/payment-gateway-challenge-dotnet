using FluentValidation;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Interfaces
{
    public interface IProcessPaymentCommandHandler
    {
        /// <summary>
        /// Handles the processing of a payment request, including validation, authorization, and response generation.
        /// </summary>
        /// <param name="postPaymentRequest">The payment request containing details to process the payment.</param>
        /// <param name="idempotencyKey">An idempotency key to prevent duplicate transactions.</param>
        /// <returns>A <see cref="Task{PostPaymentResponse}"/> representing the asynchronous operation, with a <see cref="PostPaymentResponse"/> containing payment processing results.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="postPaymentRequest"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationException">Thrown if the <paramref name="postPaymentRequest"/> fails validation.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the mapping from <see cref="Payment"/> to <see cref="PostPaymentResponse"/> fails.</exception>
        Task<PostPaymentResponse> HandleAsync(
            PostPaymentRequest postPaymentRequest,
            string idempotencyKey);
    }
}