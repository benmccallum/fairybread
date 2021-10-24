using System;
using FluentValidation.Results;
using HotChocolate;

namespace FairyBread
{
    public record ArgumentValidationResult
    {
        public NameString ArgumentName { get; }
        public string ValidatorName { get; }
        public ValidationResult Result { get; }

        public ArgumentValidationResult(
            NameString argumentName,
            string validatorName,
            ValidationResult result)
        {
            ArgumentName = argumentName;
            ValidatorName = validatorName;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }
}
