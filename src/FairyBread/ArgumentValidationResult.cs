using System;
using FluentValidation.Results;
using HotChocolate;

namespace FairyBread
{
    public record ArgumentValidationResult
    {
        public NameString ArgumentName { get; }
        public ValidationResult Result { get; }

        public ArgumentValidationResult(NameString argumentName, ValidationResult result)
        {
            ArgumentName = argumentName;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }
}
