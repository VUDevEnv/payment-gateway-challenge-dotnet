using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Interfaces.AcquiringBank;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Application.Commands
{
    public class ProcessPaymentCommandHandler(
        IValidator<PostPaymentRequest> postPaymentRequestValidator,
        IIdempotencyService idempotencyService,
        IAcquiringBank acquiringBank,
        IMapper mapper,
        IPaymentRepository paymentRepository,
        ILogger<ProcessPaymentCommandHandler> logger) : IProcessPaymentCommandHandler
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
        public async Task<PostPaymentResponse> HandleAsync(PostPaymentRequest postPaymentRequest, string idempotencyKey)
        {
            if (postPaymentRequest is null)
                throw new ArgumentNullException(nameof(postPaymentRequest));

            // Check if a cached response exists for the given idempotency key.
            (PostPaymentResponse? cachedResponse, string? requestHash) = await TryGetCachedResponseAsync(postPaymentRequest, idempotencyKey);
            if (cachedResponse != null)
            {
                logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}.", idempotencyKey);
                return cachedResponse;
            }
            
            await ValidateRequestAsync(postPaymentRequest);
           
            var cardLastFour = postPaymentRequest.CardNumber[^4..];
            LogPaymentInitiation(postPaymentRequest, cardLastFour);

            // Create a payment entity from the acquiring bank’s response.
            var payment = await CreatePaymentFromBankResponseAsync(postPaymentRequest, cardLastFour);
          
            await paymentRepository.SavePaymentAsync(payment);
            logger.LogInformation("Payment saved with ID: {PaymentId}, Status: {Status}.", payment.Id, payment.Status);
            
            var response = mapper.Map<PostPaymentResponse>(payment);
            if (response == null)
            {
                logger.LogError("Mapping to PostPaymentResponse failed for payment ID {PaymentId}.", payment.Id);
                throw new InvalidOperationException($"Mapping to PostPaymentResponse failed for payment ID '{payment.Id}'.");
            }

            // Cache the response if an idempotency key is provided.
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && !string.IsNullOrWhiteSpace(requestHash))
            {
                await idempotencyService.SaveResponseAsync(idempotencyKey, response, requestHash);
            }
            
            logger.LogInformation("Returning PostPaymentResponse for PaymentId: {PaymentId}.", response.Id);
            return response;
        }

        #region Helper Methods

        /// <summary>
        /// Attempts to retrieve a cached response for the payment request if an idempotency key is provided.
        /// </summary>
        /// <param name="request">The payment request to look up.</param>
        /// <param name="idempotencyKey">The idempotency key to identify previously processed requests.</param>
        /// <returns>A tuple containing the cached response and request hash if found, otherwise <c>null</c>.</returns>
        private async Task<(PostPaymentResponse? Response, string? RequestHash)> TryGetCachedResponseAsync(PostPaymentRequest request, string idempotencyKey)
        {
            (PostPaymentResponse? cachedResponse, string? requestHash) = await idempotencyService
                .TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey);

            return (cachedResponse, requestHash);
        }

        /// <summary>
        /// Validates the payment request using the provided validator.
        /// </summary>
        /// <param name="request">The payment request to validate.</param>
        /// <exception cref="ValidationException">Thrown if the request is invalid.</exception>
        private async Task ValidateRequestAsync(PostPaymentRequest request)
        {
            var validationResult = await postPaymentRequestValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        /// <summary>
        /// Logs the initiation of payment processing, including the card's last four digits.
        /// </summary>
        /// <param name="request">The payment request being processed.</param>
        /// <param name="cardLastFour">The last four digits of the card number for logging.</param>
        private void LogPaymentInitiation(PostPaymentRequest request, string cardLastFour)
        {
            logger.LogInformation("PostPaymentRequest validation succeeded for card ending in {CardLastFour}.", cardLastFour);
            logger.LogInformation("Starting payment processing for card ending in {CardLastFour}, amount: {Amount} {Currency}.",
                cardLastFour, request.Amount, request.Currency);
        }

        /// <summary>
        /// Communicates with the acquiring bank to authorize the payment and create the corresponding payment record.
        /// </summary>
        /// <param name="request">The payment request containing transaction details.</param>
        /// <param name="cardLastFour">The last four digits of the card for logging purposes.</param>
        /// <returns>A <see cref="Task{Payment}"/> representing the asynchronous operation, with the <see cref="Payment"/> entity created from the bank response.</returns>
        private async Task<Payment> CreatePaymentFromBankResponseAsync(PostPaymentRequest request, string cardLastFour)
        {
            var bankRequest = mapper.Map<BankRequest>(request);
            logger.LogInformation("Sending payment authorization request to acquiring bank for card ending in {CardLastFour}.", cardLastFour);
            
            var bankResponse = await acquiringBank.AuthorizePaymentAsync(bankRequest);

            if (bankResponse is null)
            {
                logger.LogError("Acquiring bank returned a null response.");
                throw new InvalidOperationException("Acquiring bank returned an unexpected null response.");
            }

            logger.LogInformation("Received response from acquiring bank: Authorized = {Authorized}, AuthCode = {AuthCode}.",
                bankResponse.Authorized, bankResponse.AuthorizationCode);

            // Create the payment record based on the bank's response.
            var payment = CreatePayment(request, cardLastFour, bankResponse);

            if (payment.Status == PaymentStatus.Declined)
            {
                logger.LogWarning("Payment was declined by acquiring bank for card ending in {CardLastFour}.", cardLastFour);
            }

            return payment;
        }

        /// <summary>
        /// Creates a payment record from the bank response and payment request.
        /// </summary>
        /// <param name="request">The payment request containing transaction details.</param>
        /// <param name="cardLastFour">The last four digits of the card number.</param>
        /// <param name="bankResponse">The response from the acquiring bank.</param>
        /// <returns>A <see cref="Payment"/> representing the created payment entity.</returns>
        private static Payment CreatePayment(PostPaymentRequest request, string cardLastFour, BankResponse bankResponse)
        {
            var currencyEnum = Enum.Parse<Currency>(request.Currency.Trim(), ignoreCase: true);
            var status = bankResponse.Authorized
                ? PaymentStatus.Authorized
                : PaymentStatus.Declined;

            return new Payment(
                cardNumberLastFour: cardLastFour,
                expiryMonth: request.ExpiryMonth,
                expiryYear: request.ExpiryYear,
                currency: currencyEnum,
                amount: request.Amount,
                cvv: request.Cvv,
                authorizationCode: bankResponse.AuthorizationCode,
                status: status
            );
        }

        #endregion
    }
}
