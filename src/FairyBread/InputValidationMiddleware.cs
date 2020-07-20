using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace FairyBread
{
    public class InputValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationResultHandler _validationResultHandler;

        public InputValidationMiddleware(FieldDelegate next, 
            IValidatorProvider validatorProvider,
            IValidationResultHandler validationResultHandler)
        {
            _next = next;
            _validatorProvider = validatorProvider;
            _validationResultHandler = validationResultHandler;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;
            if (arguments.Count > 0)
            {
                foreach (var argument in arguments)
                {
                    var validators = _validatorProvider.GetValidators(argument.ClrType);
                    var value = context.Argument<object>(argument.Name);
                    foreach (var validator in validators)
                    {
                        var validationResult = await validator.ValidateAsync(value, context.RequestAborted);
                        if (validationResult != null)
                        {
                            _validationResultHandler.Handle(context, validationResult);
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
