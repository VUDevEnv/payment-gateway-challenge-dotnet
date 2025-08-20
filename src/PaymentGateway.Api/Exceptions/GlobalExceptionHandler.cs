using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Exceptions
{
    /// <summary>
    /// Handles unhandled exceptions globally, and returns a ProblemDetails response.
    /// </summary>
    public sealed class GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        /// <summary>
        /// Attempts to handle an unhandled exception asynchronously by logging the error and returning a structured response.
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
            // Log the error with the exception details
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            // Determine the appropriate HTTP status code based on the exception type
            var statusCode = exception switch
            {
                ArgumentNullException or ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            // Explicitly set the HTTP response status code
            httpContext.Response.StatusCode = statusCode;

            // Create a ProblemDetails object to represent the error
            var problemDetails = new ProblemDetails
            {
                Type = exception.GetType().Name,
                Title = "An error occurred",
                Detail = exception.Message,
                Status = statusCode
            };

            // Create the ProblemDetailsContext to pass to the IProblemDetailsService
            var context = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            };

            // Attempt to write the problem details to the response
            return await problemDetailsService.TryWriteAsync(context);
        }
    }
}
