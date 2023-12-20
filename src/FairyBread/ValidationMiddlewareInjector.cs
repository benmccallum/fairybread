namespace FairyBread;

internal class ValidationMiddlewareInjector : TypeInterceptor
{
    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition objTypeDef)
        {
            return;
        }

        var options = context.Services
            .GetRequiredService<IFairyBreadOptions>();
        var validatorRegistry = context.Services
            .GetRequiredService<IValidatorRegistry>();

        foreach (var fieldDef in objTypeDef.Fields)
        {
            if (ValidationMiddlewareHelpers.NeedsMiddleware(options, validatorRegistry, objTypeDef, fieldDef))
            {
                ValidationMiddlewareHelpers.AddMiddleware(fieldDef);
            }
        }
    }
}
