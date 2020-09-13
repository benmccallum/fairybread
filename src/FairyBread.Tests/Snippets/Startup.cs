using FairyBread;
using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    void Usage()
    {
        #region Startup

        var services = new ServiceCollection();

        // Add the FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<CustomValidator>();

        // Add FairyBread
        services.AddFairyBread(options =>
        {
            options.AssembliesToScanForValidators = new[] {typeof(CustomValidator).Assembly};
        });

        // Configure FairyBread middleware using HotChocolate's ISchemaBuilder
        HotChocolate.SchemaBuilder.New()
            .UseFairyBread()
            .Create()
            .MakeExecutable(options =>
            {
                options
                    .UseDefaultPipeline()
                    // Note: If you've already got your own IErrorFilter
                    // in the pipeline, you should have it call this one
                    // as part of its error handling, to rewrite the
                    // validation error
                    .AddErrorFilter<DefaultValidationErrorFilter>();
            });

        #endregion
    }
    void Customization()
    {
        var services = new ServiceCollection();

        #region Customization

        services.AddFairyBread(options =>
        {
            options.AssembliesToScanForValidators = new[] { typeof(CustomValidator).Assembly };
            options.ShouldValidate = (ctx, arg) =>
            {
                //TODO: define under what conditions to perform validation
                // for example: only validate queries
                return ctx.Operation.Operation == OperationType.Query;
            };
            options.ThrowIfNoValidatorsFound = true;
        });
        #endregion
    }

    class CustomValidator
    {
    }
}
