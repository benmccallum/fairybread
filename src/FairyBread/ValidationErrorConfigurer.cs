using HotChocolate.Types;

namespace FairyBread;

internal class ValidationErrorConfigurer : MutationErrorConfiguration
{
    public override void OnConfigure(
        IDescriptorContext context,
        ObjectFieldDefinition fieldDef)
    {
        var options = context.Services
            .GetRequiredService<IFairyBreadOptions>();
        if (!options.UseMutationConventions)
        {
            return;
        }

        var validatorRegistry = context.Services
            .GetRequiredService<IValidatorRegistry>();

        // TODO: Can I get this? I think I need to PR this as Michael said he'd pass this through but hasn't.
        var objTypeDef = new ObjectTypeDefinition(); 

        if (ValidationMiddlewareHelpers.NeedsMiddleware(options, validatorRegistry, objTypeDef, fieldDef))
        {
            ValidationMiddlewareHelpers.AddMiddleware(fieldDef);

            if (!fieldDef.ContextData.TryGetValue(WellKnownContextData.UsesGlobalErrors, out var usesGlobalErrorsObj) ||
                usesGlobalErrorsObj is not bool usesGlobalErrors ||
                usesGlobalErrors == false)
            {
                fieldDef.AddErrorType(context, options.ValidationErrorType);

                // Set a flag for the errors handler
                fieldDef.ContextData[WellKnownContextData.UsesInlineErrors] = true;

                // Cleanup
                //fieldDef.ContextData.Remove(WellKnownContextData.UsesGlobalErrors);
            }
        }     
    }
}
