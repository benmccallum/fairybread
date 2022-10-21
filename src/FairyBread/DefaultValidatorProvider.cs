namespace FairyBread;

public class DefaultValidatorProvider : IValidatorProvider
{
    protected readonly IValidatorRegistry ValidatorRegistry;

    public DefaultValidatorProvider(
        IValidatorRegistry validatorRegistry)
    {
        ValidatorRegistry = validatorRegistry;
    }

    public virtual IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
    {
        if (!argument.ContextData.TryGetValue(
                WellKnownContextData.ValidatorDescriptors,
                out var validatorDescriptorsRaw) ||
            validatorDescriptorsRaw is not IEnumerable<ValidatorDescriptor> validatorDescriptors)
        {
            yield break;
        }

        foreach (var validatorDescriptor in validatorDescriptors)
        {
            if (validatorDescriptor.RequiresOwnScope)
            {
                var scope = context.Services.CreateScope(); // disposed by middleware
                var validator = (IValidator)scope.ServiceProvider.GetRequiredService(validatorDescriptor.ValidatorType);
                yield return new ResolvedValidator(validator, scope);
            }
            else
            {
                var validator = (IValidator)context.Services.GetRequiredService(validatorDescriptor.ValidatorType);
                yield return new ResolvedValidator(validator);
            }
        }
    }
}
