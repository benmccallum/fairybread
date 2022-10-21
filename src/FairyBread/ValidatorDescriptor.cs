namespace FairyBread;

/// <summary>
/// Description of a validator configuration to be stored in the validator descriptor cache.
/// </summary>
public class ValidatorDescriptor
{
    /// <summary>
    /// Type of the validator.
    /// </summary>
    public Type ValidatorType { get; }

    /// <summary>
    /// Does the validator inherit <see cref="IRequiresOwnScopeValidator"/>?
    /// If so, this means it should be resolved from the service provider in it's own scope.
    /// </summary>
    public bool RequiresOwnScope { get; }

    /// <summary>
    /// Does the validator inherit <see cref="IExplicitUsageOnlyValidator"/>?
    /// If so, this means it should be only executed when explicitly set on an argument
    /// (rather than implicitly given the type it can validate).
    /// </summary>
    public bool ExplicitUsageOnly { get; }

    /// <summary>
    /// Instantiates a new <see cref="ValidatorDescriptor"/>.
    /// </summary>
    public ValidatorDescriptor(Type validatorType)
    {
        ValidatorType = validatorType;
        RequiresOwnScope = WellKnownTypes.IRequiresOwnScopeValidator.IsAssignableFrom(validatorType);
        ExplicitUsageOnly = WellKnownTypes.IExplicitUsageOnlyValidator.IsAssignableFrom(validatorType);
    }
}
