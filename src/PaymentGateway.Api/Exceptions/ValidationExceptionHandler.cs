using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Exceptions
{
    /// <summary>
    /// Handles <see cref="FluentValidation.ValidationException"/> and returns a ProblemDetails response for validation error scenarios.
    /// </summary>
    public sealed class ValidationExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
    {
        /// <summary>
        /// Attempts to handle a <see cref="ValidationException"/> by logging the error and returning a structured 400 Bad Request response.
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
            // Check if the exception is a FluentValidation.ValidationException  
            if (exception is not ValidationException validationException)
                return false;

            // Log the validation error with the exception details
            logger.LogError(exception, "Validation error occurred: {Message}", validationException.Message);

            // Group validation errors by property name and select distinct error messages
            var errors = validationException.Errors?
                .GroupBy(e => e.PropertyName.ToLowerInvariant()) // Group errors by the property name (case-insensitive)
                .ToDictionary(
                    g => g.Key, // Key: Property name
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray() // Value: Array of distinct error messages for that property
                ) ?? new Dictionary<string, string[]>
                {
                    { "validation", [validationException.Message] } // Default error message if no grouped errors exist
                };

            // Build the ProblemDetails object for the response
            var problemDetails = new ProblemDetails
            {
                Title = "Validation Error", // Title of the problem
                Detail = validationException.Errors?.FirstOrDefault()?.ErrorMessage // Use the first error message as detail
                         ?? validationException.Message, // Fallback to the exception message if no specific error is available
                Status = StatusCodes.Status400BadRequest, // HTTP Status: 400 Bad Request
                Instance = httpContext.Request.Path, // The path of the request that caused the error
                Extensions =
                {
                    // Add validation errors in the 'errors' property of the ProblemDetails response
                    ["errors"] = errors
                }
            };

            // Explicitly set the HTTP response status code to 400 Bad Request
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            // Create the ProblemDetailsContext for a 400 error to pass to the IProblemDetailsService
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
