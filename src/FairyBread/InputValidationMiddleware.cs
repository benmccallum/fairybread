using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace FairyBread
{
    public class InputValidationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IFairyBreadOptions _options;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IValidationResultHandler _validationResultHandler;

        public InputValidationMiddleware(FieldDelegate next,
            IFairyBreadOptions options,
            IValidatorProvider validatorProvider,
            IValidationResultHandler validationResultHandler)
        {
            _next = next;
            _options = options;
            _validatorProvider = validatorProvider;
            _validationResultHandler = validationResultHandler;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;
            foreach (var argument in arguments)
            {
                if (argument == null || !_options.ShouldValidate(context, argument))
                {
                    continue;
                }

                var resolvedValidators = _validatorProvider.GetValidators(context, argument);
                try
                {
                    var value = context.Argument<object>(argument.Name);
                    foreach (var resolvedValidator in resolvedValidators)
                    {
                        var validationResult = await resolvedValidator.Validator.ValidateAsync(value, context.RequestAborted);
                        if (validationResult != null)
                        {
                            _validationResultHandler.Handle(context, validationResult);
                        }
                    }
                }
                finally
                {
                    foreach (var resolvedValidator in resolvedValidators)
                    {
                        resolvedValidator.Scope?.Dispose();
                    }
                }
            }

            await _next(context);
        }
    }
}
