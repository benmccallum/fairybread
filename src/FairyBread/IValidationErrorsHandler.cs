namespace FairyBread;

public interface IValidationErrorsHandler
{
    void Handle(
        IMiddlewareContext context,
        IEnumerable<ArgumentValidationResult> invalidResults);
}