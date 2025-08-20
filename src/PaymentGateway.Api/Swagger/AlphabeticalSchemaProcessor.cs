using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace PaymentGateway.Api.Swagger
{
    /// <summary>
    /// This class implements the <see cref="IDocumentProcessor"/> interface to customize the OpenAPI document generation.
    /// It ensures that the schema definitions in the OpenAPI document are ordered alphabetically by schema name.
    /// </summary>
    public class AlphabeticalSchemaProcessor : IDocumentProcessor
    {
        /// <summary>
        /// This method is called during the OpenAPI document generation process to customize the schemas.
        /// It retrieves all schemas from the OpenAPI document components, sorts them alphabetically by their schema name,
        /// and then repopulates the schemas dictionary in the sorted order.
        /// </summary>
        /// <param name="context">The context containing the OpenAPI document being processed.</param>
        public void Process(DocumentProcessorContext context)
        {
            // Retrieve all schemas from the OpenAPI document components and order them alphabetically by schema name (key)
            var sortedSchemas = context.Document.Components.Schemas
                .OrderBy(kvp => kvp.Key)  // Order by dictionary key (schema name)
                .ToList();

            // Reference the schemas dictionary to clear and then repopulate it in sorted order
            var target = context.Document.Components.Schemas;
            target.Clear(); // Remove all existing schemas to prepare for reordering

            // Add schemas back in alphabetical order
            foreach (var kvp in sortedSchemas)
            {
                target.Add(kvp.Key, kvp.Value); // Add each schema with its key and value
            }
        }
    }
}