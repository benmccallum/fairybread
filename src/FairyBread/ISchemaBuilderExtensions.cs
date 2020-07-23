using HotChocolate;

namespace FairyBread
{
    public static class ISchemaBuilderExtensions
    {
        public static ISchemaBuilder UseFairyBread(this ISchemaBuilder schemaBuilder)
            => schemaBuilder.Use<InputValidationMiddleware>();
    }
}
