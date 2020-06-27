using FluentValidation;
using System;
using System.Collections.Generic;

namespace FairyBread
{
    public interface IValidatorBag
    {
        IEnumerable<IValidator> GetValidators(Type typeToValidate);
    }
}
