namespace FairyBread;

public class DefaultFairyBreadOptions : IFairyBreadOptions
{
    /// <inheritdoc/>
    public virtual bool ThrowIfNoValidatorsFound { get; set; }
        = true;

    /// <inheritdoc/>
    public virtual Func<ObjectTypeDefinition, ObjectFieldDefinition, ArgumentDefinition, bool> ShouldValidateArgument { get; set; }
        = (o, f, a) => true;

    /// <inheritdoc/>
    public virtual bool UseMutationConventions { get; set; }
        = true;

    /// <inheritdoc/>
    public virtual Type ValidationExceptionType { get; set; }
        = typeof(DefaultValidationException);
}
