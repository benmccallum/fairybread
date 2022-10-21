namespace FairyBread;

public record ArgumentValidationResult
{
    /// <summary>
    /// Name of the argument this result is for.
    /// </summary>
    public string ArgumentName { get; }

    /// <summary>
    /// The validator that caused this result.
    /// </summary>
    public IValidator Validator { get; }

    /// <summary>
    /// The validation result.
    /// </summary>
    public ValidationResult Result { get; }

    public ArgumentValidationResult(
        string argumentName,
        IValidator validator,
        ValidationResult result)
    {
        ArgumentName = argumentName;
        Validator = validator;
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }
}
