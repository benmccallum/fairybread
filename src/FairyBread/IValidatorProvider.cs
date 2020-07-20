using FluentValidation;
using System;
using System.Collections.Generic;

namespace FairyBread
{
    public interface IValidatorProvider
    {
        IEnumerable<IValidator> GetValidators(Type typeToValidate);
    }
}
