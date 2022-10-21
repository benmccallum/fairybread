namespace FairyBread;

public struct ResolvedValidator
{
    public IValidator Validator { get; }

    public IServiceScope? Scope { get; }

    public ResolvedValidator(IValidator validator)
    {
        Validator = validator;
        Scope = null;
    }

    public ResolvedValidator(IValidator validator, IServiceScope scope)
    {
        Validator = validator;
        Scope = scope;
    }
}