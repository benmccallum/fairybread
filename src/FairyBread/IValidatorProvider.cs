using FluentValidation;
using HotChocolate.Resolvers;
using System;
using System.Collections.Generic;

namespace FairyBread
{
    public interface IValidatorProvider
    {
        IEnumerable<IValidator> GetValidators(IMiddlewareContext context, Type typeToValidate);
    }
}
