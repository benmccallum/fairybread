using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace FairyBread
{
    public class InputValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IValidatorBag _validatorBag;

        public InputValidationMiddleware(FieldDelegate next, IValidatorBag validatorBag)
        {
            _next = next;
            _validatorBag = validatorBag;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;
            if (arguments.Count > 0)
            {
                foreach (var argument in arguments)
                {
                    var validators = _validatorBag.GetValidators(argument.ClrType);
                    foreach (var validator in validators)
                    {
                        await validator.ValidateAsync("", context.RequestAborted);
                    }
                }
            }


            await _next(context);
        }
    }
}
