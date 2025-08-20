using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Api.Exceptions
{
    /// <summary>
    /// Handles <see cref="NotFoundException"/> and returns a ProblemDetails response for the resource not found scenario.
    /// </summary>
    public sealed class NotFoundExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
    {
        /// <summary>
        /// Attempts to handle a <see cref="NotFoundException"/> by logging the error and returning a structured 404 Not Found response.
        /// </summary>
        /// <param name="httpContext">The HTTP context for the current request.</param>
        /// <param name="exception">The exception that occurred during request processing.</param>
        /// <param name="cancellationToken">A cancellation token to observe for cancellation requests.</param>
        /// <returns>A <see cref="ValueTask{Boolean}"/> representing the asynchronous operation, with a <c>true</c> value if the exception was handled successfully; otherwise, <c>false</c>.</returns>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Check if the exception is a NotFoundException
            if (exception is not NotFoundException notFoundException)
                return false;

            // Log the error with the exception details
            logger.LogError(exception, "Resource not found: {Message}", notFoundException.Message);

            // Create the ProblemDetailsContext for a 404 error to pass to the IProblemDetailsService
            var context = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Type = exception.GetType().Name,
                    Title = "Resource Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = notFoundException.Message
                }
            };

            // Explicitly set the HTTP response status code to 404 Not Found
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

            // Attempt to write the problem details to the response
            return await problemDetailsService.TryWriteAsync(context);
        }
    }
}