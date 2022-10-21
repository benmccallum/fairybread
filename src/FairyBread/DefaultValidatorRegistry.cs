namespace FairyBread;

public class DefaultValidatorRegistry : IValidatorRegistry
{
    public DefaultValidatorRegistry(IServiceCollection services, IFairyBreadOptions options)
    {
        var validatorResults = new List<AssemblyScanner.AssemblyScanResult>();
        var objectValidatorInterface = typeof(IValidator<object>);
        var underlyingValidatorType = objectValidatorInterface.GetGenericTypeDefinition().UnderlyingSystemType;

        foreach (var service in services)
        {
            if (!service.ServiceType.IsGenericType ||
                service.ServiceType.Name != objectValidatorInterface.Name ||
                service.ServiceType.GetGenericTypeDefinition() != underlyingValidatorType)
            {
                continue;
            }

            validatorResults.Add(
                new AssemblyScanner.AssemblyScanResult(
                    service.ServiceType,
                    service.ImplementationType));
        }

        if (!validatorResults.Any() && options.ThrowIfNoValidatorsFound)
        {
            throw new Exception($"No validators were found by FairyBread. " +
                                $"Ensure you're registering your FluentValidation validators for DI.");
        }

        foreach (var validatorResult in validatorResults)
        {
            var validatorType = validatorResult.ValidatorType;

            var validatedType = validatorResult.InterfaceType.GenericTypeArguments.Single();
            if (!Cache.TryGetValue(validatedType, out var validatorsForType))
            {
                Cache[validatedType] = validatorsForType = new List<ValidatorDescriptor>();
            }

            var validatorDescriptor = new ValidatorDescriptor(validatorType);

            if (!validatorDescriptor.ExplicitUsageOnly)
            {
                validatorsForType.Add(validatorDescriptor);
            }
        }
    }

    public Dictionary<Type, List<ValidatorDescriptor>> Cache { get; } = new();

}
