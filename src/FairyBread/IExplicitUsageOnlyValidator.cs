namespace FairyBread;

/// <summary>
/// Marker interface for indicating that a validator
/// should only be run by FairyBread on a field resolver argument
/// if it is explicitly assigned to that argument.
/// </summary>
public interface IExplicitUsageOnlyValidator
{
}