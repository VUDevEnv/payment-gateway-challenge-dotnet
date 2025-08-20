using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Api.Controllers.V1
{
    /// <summary>
    /// API controller responsible for processing payment transaction and payment retrieval by unique identifier.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/payments")]
    public class PaymentController(IProcessPaymentCommandHandler handler, 
        IPaymentService paymentService) : ControllerBase
    {
        /// <summary>
        /// Processes a payment transaction request by initiating authorization with the acquiring bank.
        /// </summary>
        /// <remarks>
        /// This method accepts a payment request, validates the input, and processes the payment.
        /// It then returns an appropriate response based on the outcome of the payment authorization.
        /// 
        /// <para><strong>Behavior:</strong></para>
        /// - Returns a <c>400 Bad Request</c> if the input validation fails or if there are missing/incorrect parameters.
        /// - On successful payment processing, returns a <c>201 Created</c> with the payment details and the location header pointing to the created payment resource.
        /// - In case of an unhandled exception, returns a <c>500 Internal Server Error</c> with a generic error message.
        /// </remarks>
        /// <param name="postPaymentRequest">The payment details, including card information, amount, and currency to be processed.</param>
        /// <param name="idempotencyKey">A unique key to ensure the request is processed only once, preventing duplicate payments.</param>
        /// <returns>
        /// A <see cref="PostPaymentResponse"/> object containing the payment authorization result and the newly created payment ID, or an error response in case of failure.
        /// </returns>
        /// <response code="201">The payment was successfully processed, and a new payment resource was created.</response>
        /// <response code="400">The request was invalid due to failed validation or incorrect input.</response>
        /// <response code="500">An unexpected error occurred during payment processing.</response>
        [HttpPost]
        [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessPayment(
            [FromBody] PostPaymentRequest postPaymentRequest,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {
            // Process payment asynchronously with the provided request details and idempotency key.
            var response = await handler.HandleAsync(postPaymentRequest, idempotencyKey);

            // Return a 201 Created response with the location of the newly created payment resource.
            return CreatedAtAction(nameof(GetPaymentById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Retrieves the details of a payment based on its unique identifier (GUID).
        /// </summary>
        /// <remarks>
        /// This method allows retrieval of previously processed payment data using a unique GUID identifier.
        /// The payment details are fetched from the service layer, and an appropriate response is returned depending on the outcome.
        /// 
        /// <para><strong>Behavior:</strong></para>
        /// - If the <paramref name="id"/> is an empty GUID, a <c>400 Bad Request</c> is returned due to invalid input.
        /// - If no payment is found for the provided <paramref name="id"/>, a <c>404 Not Found</c> is returned.
        /// - If the payment is successfully found, a <c>200 OK</c> is returned with the payment details.
        /// - In the case of an unhandled error, a <c>500 Internal Server Error</c> is returned with a generic error message.
        /// </remarks>
        /// <param name="id">The unique GUID identifier of the payment to retrieve.</param>
        /// <returns>
        /// A <see cref="GetPaymentResponse"/> object containing the payment details, or an appropriate error response if not found.
        /// </returns>
        /// <response code="200">The payment was successfully retrieved and is included in the response body.</response>
        /// <response code="400">The provided payment ID is invalid (e.g., an empty GUID).</response>
        /// <response code="404">No payment was found for the provided ID.</response>
        /// <response code="500">An unexpected error occurred during the retrieval of the payment.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            return Ok(await paymentService.GetPaymentByIdAsync(id));
        }
    }
}
