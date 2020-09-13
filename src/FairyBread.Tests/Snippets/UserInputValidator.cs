using FairyBread;
using FluentValidation;

#region UserInputValidator

public class UserInputValidator : AbstractValidator<UserInput>, IRequiresOwnScopeValidator
{
    // db will be a unique instance for this validation operation
    public UserInputValidator(SomeDbContext db)
    {
        // ...
    }
}

#endregion

public class SomeDbContext
{
}

public class UserInput
{
}
