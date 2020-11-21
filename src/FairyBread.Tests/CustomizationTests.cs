using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class CustomizationTests
    {
        private const string Query = @"query { read(foo: { someInteger: 1, someString: ""hello"" }) }";

        private IQueryExecutor InitQueryExecutor(Action<IServiceCollection> preBuildProviderAction)
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<CustomValidator>();
            services.AddFairyBread(options =>
            {
                options.AssembliesToScanForValidators = new[] { typeof(CustomValidator).Assembly };
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

            return schema.MakeExecutable(builder =>
            {
                builder
                    .UseDefaultPipeline()
                    .AddErrorFilter<ValidationErrorFilter>();
            });
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
            var result = await queryExecutor.ExecuteAsync(Query);

            // Assert
            Assert.NotEmpty(result.Errors.AsEnumerable());
            Assert.True(result.Errors.Count(e => e.Message == "lol") == 1);
        }

        public class CustomValidationResultHandler : DefaultValidationResultHandler
        {
            public override bool Handle(IMiddlewareContext context, ValidationResult result)
            {
                context.ReportError("lol");
                return false;
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
            var result = await queryExecutor.ExecuteAsync(Query);

            // Assert
            await Verifier.Verify(result);
        }

        public class CustomValidatorProvider : DefaultValidatorProvider
        {
            public CustomValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options)
                : base(serviceProvider, options) { }

            public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, Argument argument)
                => argument.ClrType == typeof(FooInputDto)
                    ? (new ResolvedValidator[] { new ResolvedValidator(new CustomValidator()) })
                    : base.GetValidators(context, argument);
        }

        public class QueryType
        {
            public string Read(FooInputDto foo) => $"{foo};";
        }

        public class MutationType
        {
            public string Write(FooInputDto foo) => $"{foo};";
        }

        public class FooInputDto
        {
            public int SomeInteger { get; set; }

            public string SomeString { get; set; } = "";

            public override string ToString() =>
                $"SomeInteger: {SomeInteger}, " +
                $"SomeString: {SomeString}";
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
