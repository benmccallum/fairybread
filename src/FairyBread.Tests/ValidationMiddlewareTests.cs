using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class ValidationMiddlewareTests
    {
        static ValidationMiddlewareTests()
        {
            VerifierSettings.NameForParameter<CaseData>(_ => _.CaseId);
        }

        private static async Task<IRequestExecutor> GetRequestExecutorAsync(
            Action<IFairyBreadOptions>? configureOptions = null,
            Action<IServiceCollection>? configureServices = null,
            bool registerValidators = true)
        {
            var services = new ServiceCollection();
            configureServices?.Invoke(services);

            if (registerValidators)
            {
                services.AddValidator<FooInputDtoValidator, FooInputDto>();
                services.AddValidator<BarInputDtoValidator, BarInputDto>();
                services.AddValidator<BarInputDtoAsyncValidator, BarInputDto>();
                services.AddValidator<NullableIntValidator, int?>();
            }

            var builder = services
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddFairyBread(options =>
                {
                    configureOptions?.Invoke(options);
                });

            return await builder
                .BuildRequestExecutorAsync();
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Query_Works(CaseData caseData)
        {
            // Arrange
            var executor = await GetRequestExecutorAsync();

            var query = "query { read(foo: " + caseData.FooInput + ", bar: " + caseData.BarInput + ") }";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(caseData);
            await Verifier.Verify(result, verifySettings);
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Mutation_Works(CaseData caseData)
        {
            // Arrange
            var executor = await GetRequestExecutorAsync();

            var query = "mutation { write(foo: " + caseData.FooInput + ", bar: " + caseData.BarInput + ") }";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(caseData);
            await Verifier.Verify(result, verifySettings);
        }

        [Fact]
        public async Task Multi_TopLevelFields_And_MultiRuns_Works()
        {
            // Arrange
            var executor = await GetRequestExecutorAsync();

            var query = @"
                query {
                    read(foo: { someInteger: -1, someString: ""hello"" })
                    read(foo: { someInteger: -1, someString: ""hello"" })
                }";

            // Act
            var result1 = await executor.ExecuteAsync(query);
            var result2 = await executor.ExecuteAsync(query);
            var result3 = await executor.ExecuteAsync(query);


            // Assert
            await Verifier.Verify(new { result1, result2, result3 });
        }

        [Fact]
        public async Task Ignores_Null_Argument_Value()
        {
            // Arrange
            var caseData = (CaseData)Cases().First()[0];
            var executor = await GetRequestExecutorAsync();

            var query = "query { read(foo: " + caseData.FooInput + ") }";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            var verifySettings = new VerifySettings();
            await Verifier.Verify(result, verifySettings);
        }

        [Fact]
        public async Task Doesnt_Call_Field_Resolver_If_Invalid()
        {
            // Arrange
            var executor = await GetRequestExecutorAsync();

            var query = @"query { someResolver(foo: { someInteger: -1, someString: ""hello"" }) }";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            Assert.False(Query.WasFieldResolverCalled);
            await Verifier.Verify(result);
        }

        [Fact]
        public async Task Should_Respect_ShouldValidateArgument_Option()
        {
            // Arrange
            var executor = await GetRequestExecutorAsync(options =>
            {
                options.ShouldValidateArgument = (o, t, a)
                    => a.Parameter is { } p && p.ParameterType != typeof(FooInputDto);
            });

            var query = @"mutation {
                write(
                    foo: { someInteger: -1, someString: ""hello"" },
                    bar: { emailAddress: ""ben@lol.com"" }) }";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            await Verifier.Verify(result);
        }

        // TODO: Unit tests for:
        // - cancellation

        public static IEnumerable<object[]> Cases()
        {
            var caseId = 1;
            yield return new object[]
            {
                // Happy days
                new CaseData(caseId++, @"{ someInteger: 1, someString: ""hello"" }", @"{ emailAddress: ""ben@lol.com"" }")
            };
            yield return new object[]
            {
                // Sync error
                new CaseData(caseId++, @"{ someInteger: -1, someString: ""hello"" }", @"{ emailAddress: ""ben@lol.com"" }")
            };
            yield return new object[]
            {
                // Async error
                new CaseData(caseId++, @"{ someInteger: 1, someString: ""hello"" }", @"{ emailAddress: ""-1"" }")
            };
            yield return new object[]
            {
                // Multiple sync errors and async error
                new CaseData(caseId++, @"{ someInteger: -1, someString: ""-1"" }", @"{ emailAddress: ""-1"" }")
            };
        }

        public class CaseData
        {
            public string CaseId { get; set; }
            public string FooInput { get; set; }
            public string BarInput { get; set; }

            public CaseData(int caseId, string fooInput, string barInput)
            {
                CaseId = caseId.ToString();
                FooInput = fooInput;
                BarInput = barInput;
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public class Query
        {
            public static bool WasFieldResolverCalled { get; private set; }

            public string Read(FooInputDto foo, BarInputDto? bar)
            {
                return $"{foo}; {bar}";
            }

            public string SomeResolver(FooInputDto foo, BarInputDto? bar)
            {
                WasFieldResolverCalled = true;
                return $"{foo}; {bar}";
            }

            public string IntResolver(int count)
            {
                return count.ToString();
            }

            public string NullableIntResolver(int? count)
            {
                return count.ToString();
            }
        }

        public class Mutation
        {
            public string Write(FooInputDto foo, BarInputDto bar) => $"{foo}; {bar}";
        }

        public class MyInput : InputObjectType<FooInputDto>
        {
            protected override void Configure(IInputObjectTypeDescriptor<FooInputDto> descriptor)
            {
                descriptor.BindFieldsImplicitly();
            }
        }

        public class FooInputDto
        {
            public int SomeInteger { get; set; }

            public string SomeString { get; set; } = "";

            public override string ToString() => 
                $"SomeInteger: {SomeInteger}, " +
                $"SomeString: {SomeString}";
        }

        public class FooInputDtoValidator : AbstractValidator<FooInputDto>
        {
            public FooInputDtoValidator()
            {
                RuleFor(x => x.SomeInteger).Equal(1);
                RuleFor(x => x.SomeString).Equal("hello");
            }
        }

        public class BarInputDto
        {
            public string EmailAddress { get; set; } = "";

            public override string ToString()
                => $"EmailAddress: {EmailAddress}";
        }

        public abstract class BarInputDtoValidatorBase : AbstractValidator<BarInputDto>
        {
            public BarInputDtoValidatorBase()
            {
                RuleFor(x => x.EmailAddress).NotNull();
            }
        }

        public class BarInputDtoValidator : BarInputDtoValidatorBase
        {

        }

        public class BarInputDtoAsyncValidator : AbstractValidator<BarInputDto>
        {
            public BarInputDtoAsyncValidator()
            {
                RuleFor(x => x.EmailAddress)
                    // TODO: Cancellation unit test
                    .MustAsync((val, _) => Task.FromResult(val == "ben@lol.com"));
            }
        }

        public class CustomValidationErrorsHandler : IValidationErrorsHandler
        {
            public void Handle(IMiddlewareContext context, IEnumerable<ValidationResult> invalidResults)
            {
                context.Result = "Custom set result on error";
            }
        }

        public class NullableIntValidator : AbstractValidator<int?>
        {
            public NullableIntValidator()
            {
                RuleFor(x => x)
                    //.Null()
                    .GreaterThan(0).When(x => x is not null);
            }
        }
    }
}
