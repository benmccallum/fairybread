using FairyBread;

#region CustomValidationResultHandler

public class CustomValidationResultHandler : DefaultValidationResultHandler
{
    //TODO: what to do here since ExtractError doesnt exist
    //protected override IErrorBuilder ExtractError(IMiddlewareContext context, ValidationFailure failure)
    //    => base.ExtractError(context, failure)
    //        .SetExtension(nameof(failure.ErrorCode), $"CustomPrefix:{failure.ErrorCode}");
}

#endregion
