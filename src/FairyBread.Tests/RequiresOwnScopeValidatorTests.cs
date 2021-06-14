﻿using System;
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
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class RequiresOwnScopeValidatorTests
    {
        private const string Query = @"query { read(foo: { someInteger: 1, someString: ""hello"" }) }";

        private static async Task<IRequestExecutor> GetRequestExecutorAsync(Action<IServiceCollection> preBuildProviderAction)
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<RequiresOwnScopeValidator>();
            preBuildProviderAction?.Invoke(services);

            return await services
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddFairyBread(options =>
                {
                    options.ShouldValidate = (ctx, arg) => ctx.Operation.Operation == OperationType.Query;
                })
                .BuildRequestExecutorAsync();
        }

        [Fact]
        public async Task OwnScopes_Work()
        {
            // Arrange
            var executor = await GetRequestExecutorAsync(services =>
            {
                services.AddScoped<IValidatorProvider, AssertingScopageValidatorProvider>();
            });

            // Act
            var result = await executor.ExecuteAsync(Query);

            // Assert
            await Verifier.Verify(result);
        }

        [Fact]
        public async Task OwnScopes_Are_Disposed()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.Dispose());

            var executor = await GetRequestExecutorAsync(services =>
            {
                services.AddScoped<IValidatorProvider>(sp =>
                    new ScopeMockingValidatorProvider(sp.GetRequiredService<IValidatorRegistry>(), scopeMock.Object));
            });

            // Act
            var result = await executor.ExecuteAsync(Query);

            // Assert
            scopeMock.Verify(x => x.Dispose(), Times.Once);
            await Verifier.Verify(result);
        }

        public class AssertingScopageValidatorProvider : DefaultValidatorProvider
        {
            public AssertingScopageValidatorProvider(IValidatorRegistry validatorRegistry)
                : base(validatorRegistry) { }

            public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
            {
                var validators = base.GetValidators(context, argument);

                var standardValidator = validators.Single(v => v.Validator is StandardValidator);
                var anotherStandardValidator = validators.Single(v => v.Validator is AnotherStandardValidator);
                Assert.Null(standardValidator.Scope);
                Assert.Null(anotherStandardValidator.Scope);
                Assert.Equal(standardValidator.Scope, anotherStandardValidator.Scope);

                var ownScopeValidator = validators.Single(v => v.Validator is RequiresOwnScopeValidator);
                Assert.NotNull(ownScopeValidator.Scope);
                Assert.NotEqual(standardValidator.Scope, ownScopeValidator.Scope);

                var anotherOwnScopeValidator = validators.Single(v => v.Validator is AnotherRequiresOwnScopeValidator);
                Assert.NotNull(anotherOwnScopeValidator.Scope);
                Assert.NotEqual(standardValidator.Scope, anotherOwnScopeValidator.Scope);

                Assert.NotEqual(ownScopeValidator.Scope, anotherOwnScopeValidator.Scope);

                return validators;
            }
        }

        public class ScopeMockingValidatorProvider : DefaultValidatorProvider
        {
            private readonly IServiceScope _mockScope;

            public ScopeMockingValidatorProvider(IValidatorRegistry validatorRegistry, IServiceScope mockScope)
                : base(validatorRegistry)
            {
                _mockScope = mockScope;
            }

            public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
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
