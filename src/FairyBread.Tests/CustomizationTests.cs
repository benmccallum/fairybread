using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;
using static FairyBread.Tests.InputValidationMiddlewareTests;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class CustomizationTests
    {
        private const string query = @"query { read(foo: { someInteger: 1, someString: ""hello"" }, bar: { emailAddress: ""ben@lol.com"" }) }";

        private IQueryExecutor InitQueryExecutor(Action<IServiceCollection> preBuildProviderAction)
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();
            services.AddFairyBread(options =>
            {
                options.AssembliesToScanForValidators = new[] { typeof(FooInputDtoValidator).Assembly };
                options.ShouldValidate = (ctx, arg) => ctx.Operation.Operation == OperationType.Query;
            });

            preBuildProviderAction?.Invoke(services);

            var serviceProvider = services.BuildServiceProvider();

            var schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddServices(serviceProvider)
                .UseFairyBread()
                .Create();

            return schema.MakeExecutable();
        }

        [Fact]
        public async Task CustomValidationResultHandler_Works()
        {
            // Arrange
            var queryExecutor = InitQueryExecutor(services =>
            {
                services.AddSingleton<IValidationResultHandler, CustomValidationResultHandler>();
            });

            // Act            
            var result = await queryExecutor.ExecuteAsync(query);

            // Assert
            Assert.NotEmpty(result.Errors.AsEnumerable());
            Assert.All(result.Errors, e => Assert.Equal("lol", e.Message));
        }

        public class CustomValidationResultHandler : DefaultValidationResultHandler
        {
            public override void Handle(IMiddlewareContext context, ValidationResult result)
            {
                context.ReportError("lol");
            }
        }

        [Fact]
        public async Task CustomValidatorProvider_Works()
        {
            // Arrange
            var queryExecutor = InitQueryExecutor(services =>
            {
                services.AddSingleton<IValidatorProvider, CustomValidatorProvider>();
            });

            // Act
            var result = await queryExecutor.ExecuteAsync(query);

            // Assert
            await Verifier.Verify(result, new VerifySettings());
        }

        public class CustomValidatorProvider : DefaultValidatorProvider
        {
            public CustomValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options)
                : base(serviceProvider, options) { }

            public override IEnumerable<IValidator> GetValidators(IMiddlewareContext context, Type typeToValidate) 
                => typeToValidate == typeof(FooInputDto)
                    ? (new IValidator[] { new CustomValidator() })
                    : base.GetValidators(context, typeToValidate);
        }

        public class CustomValidator : AbstractValidator<FooInputDto>
        {
            public CustomValidator()
            {
                RuleFor(x => x.SomeInteger)
                    .GreaterThanOrEqualTo(999);
            }
        }
    }
}
