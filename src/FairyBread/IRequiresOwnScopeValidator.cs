namespace FairyBread;

/// <summary>
/// Marker interface for validators that should be resolved
/// from the service provider in it's own scope.
/// </summary>
public interface IRequiresOwnScopeValidator : IValidator
{

}