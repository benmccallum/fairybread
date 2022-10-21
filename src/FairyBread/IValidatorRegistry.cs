namespace FairyBread;

/// <summary>
/// Maintains a registry of implicit validators
/// keyed by the target runtime type for validation.
/// </summary>
public interface IValidatorRegistry
{
    Dictionary<Type, List<ValidatorDescriptor>> Cache { get; }
}