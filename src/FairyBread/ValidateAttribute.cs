namespace FairyBread;

/// <summary>
/// Instructs FairyBread to add the given validator/s for the annotated argument.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ValidateAttribute : ArgumentDescriptorAttribute
{
    public Type[] ValidatorTypes;

    public ValidateAttribute(params Type[] validatorTypes)
    {
        ValidatorTypes = validatorTypes;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<ValidateAttribute>() is not ValidateAttribute attr)
        {
            return;
        }

        descriptor.ValidateWith(attr.ValidatorTypes);
    }
}

public static class ValidateArgumentDescriptorExtensions
{
    /// <summary>
    /// Instructs FairyBread to add the given validator to the argument.
    /// </summary>
    public static IArgumentDescriptor ValidateWith<TValidator>(
        this IArgumentDescriptor descriptor)
        where TValidator : IValidator
    {
        return descriptor.ValidateWith(typeof(TValidator));
    }

    /// <summary>
    /// Instructs FairyBread to add the given validator/s to the argument.
    /// </summary>
    public static IArgumentDescriptor ValidateWith(
        this IArgumentDescriptor descriptor,
        params Type[] validatorTypes)
    {
        descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
        {
            if (!argDef.ContextData.TryGetValue(WellKnownContextData.ExplicitValidatorTypes, out var explicitValidatorTypesRaw) ||
                explicitValidatorTypesRaw is not List<Type> explicitValidatorTypes)
            {
                argDef.ContextData[WellKnownContextData.ExplicitValidatorTypes]
                    = new List<Type>(validatorTypes.Distinct());
                return;
            }

            foreach (var validatorType in validatorTypes)
            {
                if (!explicitValidatorTypes.Contains(validatorType))
                {
                    explicitValidatorTypes.Add(validatorType);
                }
            }
        });

        return descriptor;
    }
}
