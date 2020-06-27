using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace FairyBread
{
    public class InputValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IValidatorBag _validatorBag;
        private readonly IValidationResultHandler validationResultHandler;

        public InputValidationMiddleware(FieldDelegate next, 
            IValidatorBag validatorBag,
            IValidationResultHandler validationResultHandler)
        {
            _next = next;
            _validatorBag = validatorBag;
            this.validationResultHandler = validationResultHandler;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;
            if (arguments.Count > 0)
            {
                foreach (var argument in arguments)
                {
                    var validators = _validatorBag.GetValidators(argument.ClrType);
                    var value = context.Argument<object>(argument.Name);
                    foreach (var validator in validators)
                    {
                        var validationResult = await validator.ValidateAsync(value, context.RequestAborted);
                        if (validationResult != null)
                        {
                            validationResultHandler.Handle(context, validationResult);
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
