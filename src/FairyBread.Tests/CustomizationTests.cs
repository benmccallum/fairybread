using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class CustomizationTests
    {
        private const string Query = @"query { read(foo: { someInteger: 1, someString: ""hello"" }) }";

        private static async Task<IRequestExecutor> GetRequestExecutorAsync(
            Action<IServiceCollection> preBuildProviderAction)
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<CustomValidator>();
            preBuildProviderAction?.Invoke(services);

            return await services
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddFairyBread(options =>
                {
                    options.AssembliesToScanForValidators = new[] { typeof(CustomValidator).Assembly };
                    options.ShouldValidate = (ctx, arg) => ctx.Operation.Operation == OperationType.Query;
                })
                .BuildRequestExecutorAsync();
        }

        [Fact]
        public async Task CustomValidationResultHandler_Works()
        {
            // Arrange
            var executor = await GetRequestExecutorAsync(services =>
            {
                services.AddSingleton<IValidationErrorsHandler, CustomValidationErrorsHandler>();
            });

            // Act
            var result = await executor.ExecuteAsync(Query);

            // Assert
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);
            Assert.True(result.Errors!.All(e => e.Message == "lol"));
        }

        public class CustomValidationErrorsHandler : DefaultValidationErrorsHandler
        {
            protected override IError BuildError(IMiddlewareContext context, ValidationFailure failure) =>
                base.BuildError(context, failure)
                .WithMessage("lol");
        }

        [Fact]
        public async Task CustomValidatorProvider_Works()
        {
            // Arrange
            var executor = await GetRequestExecutorAsync(services =>
            {
                services.AddSingleton<IValidatorProvider, CustomValidatorProvider>();
            });

            // Act
            var result = await executor.ExecuteAsync(Query);

            // Assert
            await Verifier.Verify(result);
        }

        public class CustomValidatorProvider : DefaultValidatorProvider
        {
            public CustomValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options)
                : base(serviceProvider, options) { }

            public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
                => argument.RuntimeType == typeof(FooInputDto)
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
