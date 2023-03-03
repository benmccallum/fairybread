namespace FairyBread;

/// <summary>
/// Instructs FairyBread to not run any validation on the annotated argument.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class DontValidateAttribute : ArgumentDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter)
    {
        descriptor.DontValidate();
    }
}

public static class DontValidateArgumentDescriptorExtensions
{
    /// <summary>
    /// Instructs FairyBread to not run any validation for this argument.
    /// </summary>
    public static IArgumentDescriptor DontValidate(
        this IArgumentDescriptor descriptor)
    {
        descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
        {
            argDef.ContextData[WellKnownContextData.DontValidate] = true;
        });

        return descriptor;
    }
}
