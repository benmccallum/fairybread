using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FairyBread.Tests
{
    public class RequiresOwnScopeValidatorTests
    {
        private const string Query = @"query { read(foo: { someInteger: 1, someString: ""hello"" }) }";

        private IQueryExecutor InitQueryExecutor(Action<IServiceCollection> preBuildProviderAction)
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<RequiresOwnScopeValidator>();
            services.AddFairyBread(options =>
            {
                options.AssembliesToScanForValidators = new[] { typeof(RequiresOwnScopeValidator).Assembly };
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
                    .AddErrorFilter<DefaultValidationErrorFilter>();
            });
        }

        [Fact]
        public async Task OwnScopes_Work()
        {
            // Arrange
            var queryExecutor = InitQueryExecutor(services =>
            {
                services.AddSingleton<IValidatorProvider, AssertingScopageValidatorProvider>();
            });

            // Act            
            var result = await queryExecutor.ExecuteAsync(Query);

            // Assert
            // Done in CustomValidatorProvider
        }

        [Fact]
        public async Task OwnScopes_Are_Disposed()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.Dispose());

            var queryExecutor = InitQueryExecutor(services =>
            {
                services.AddSingleton<IValidatorProvider>(sp =>
                    new ScopeMockingValidatorProvider(sp, sp.GetRequiredService<IFairyBreadOptions>(), scopeMock.Object));
            });

            // Act            
            var result = await queryExecutor.ExecuteAsync(Query);

            // Assert
            scopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        public class AssertingScopageValidatorProvider : DefaultValidatorProvider
        {
            public AssertingScopageValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options)
                : base(serviceProvider, options) { }

            public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, Argument argument)
            {
                var validators = base.GetValidators(context, argument);

                var standardValidator = validators.Where(v => v.Validator is StandardValidator).Single();
                var anotherStandardValidator = validators.Where(v => v.Validator is AnotherStandardValidator).Single();
                Assert.Null(standardValidator.Scope);
                Assert.Null(anotherStandardValidator.Scope);
                Assert.Equal(standardValidator.Scope, anotherStandardValidator.Scope);

                var ownScopeValidator = validators.Where(v => v.Validator is RequiresOwnScopeValidator).Single();
                Assert.NotNull(ownScopeValidator.Scope);
                Assert.NotEqual(standardValidator.Scope, ownScopeValidator.Scope);

                var anotherOwnScopeValidator = validators.Where(v => v.Validator is AnotherRequiresOwnScopeValidator).Single();
                Assert.NotNull(anotherOwnScopeValidator.Scope);
                Assert.NotEqual(standardValidator.Scope, anotherOwnScopeValidator.Scope);

                Assert.NotEqual(ownScopeValidator.Scope, anotherOwnScopeValidator.Scope);

                return validators;
            }
        }

        public class ScopeMockingValidatorProvider : DefaultValidatorProvider
        {
            private readonly IServiceScope _mockScope;

            public ScopeMockingValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options, IServiceScope mockScope)
                : base(serviceProvider, options)
            {
                _mockScope = mockScope;
            }

            public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, Argument argument)
            {
                yield return new ResolvedValidator(new RequiresOwnScopeValidator(), _mockScope);
            }
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

        public class StandardValidator : AbstractValidator<FooInputDto>
        {
            public StandardValidator()
            {
                RuleFor(x => x.SomeInteger)
                    .GreaterThanOrEqualTo(50);
            }
        }

        public class AnotherStandardValidator : AbstractValidator<FooInputDto>
        {
            public AnotherStandardValidator()
            {
                RuleFor(x => x.SomeInteger)
                    .GreaterThanOrEqualTo(100);
            }
        }

        public class RequiresOwnScopeValidator : AbstractValidator<FooInputDto>, IRequiresOwnScopeValidator
        {
            public RequiresOwnScopeValidator()
            {
                RuleFor(x => x.SomeInteger)
                    .GreaterThanOrEqualTo(999);
            }
        }

        public class AnotherRequiresOwnScopeValidator : AbstractValidator<FooInputDto>, IRequiresOwnScopeValidator
        {
            public AnotherRequiresOwnScopeValidator()
            {
                RuleFor(x => x.SomeInteger)
                    .GreaterThanOrEqualTo(9999);
            }
        }
    }
}
