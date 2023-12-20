
namespace FairyBread;

/// <summary>
/// <para>
/// Instructs FairyBread, when using HotChocolate's mutation conventions
/// and with FairyBread configured with UseMutationConventions as true,
/// to report validation errors in the global errors array of the GraphQL
/// response (rather than inline in this mutation fields' local errors array).
/// </para>
/// <para>
/// This attribute is intended to allow a progressive transition from FairyBread's
/// former behavior with mutation conventions (global errors) to the
/// new behavior (local errors). Fields that you aren't ready to modernize can 
/// be annotated in the meantime. 
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class GlobalValidationErrorsAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo memberInfo)
    {
        descriptor.GlobalValidationErrors();
    }
}

public static class GlobalValidationErrorsObjectFieldDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Instructs FairyBread, when using HotChocolate's mutation conventions
    /// and with FairyBread configured with UseMutationConventions as true,
    /// to report validation errors in the global errors array of the GraphQL
    /// response (rather than inline in this mutation fields' local errors array).
    /// </para>
    /// <para>
    /// This is intended to allow a progressive transition from FairyBread's
    /// former behavior with mutation conventions (global errors) to the
    /// new behavior (local errors). Fields that you aren't ready to modernize can 
    /// use this in the meantime. 
    /// </para>
    /// </summary>
    public static IObjectFieldDescriptor GlobalValidationErrors(
        this IObjectFieldDescriptor descriptor)
    {
        descriptor.Extend().OnBeforeNaming((completionContext, objFieldDef) =>
        {
            objFieldDef.ContextData[WellKnownContextData.UsesGlobalErrors] = true;
        });

        return descriptor;
    }
}
