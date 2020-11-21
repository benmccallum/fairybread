using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class InputValidationMiddlewareTests
    {
        static InputValidationMiddlewareTests()
        {
            VerifierSettings.NameForParameter<CaseData>(_ => _.CaseId);
        }

        private IQueryExecutor GetQueryExecutor(Action<IFairyBreadOptions>? configureOptions = null)
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();
            services.AddFairyBread(options =>
            {
                options.AssembliesToScanForValidators = new[] { typeof(FooInputDtoValidator).Assembly };
                configureOptions?.Invoke(options);
            });
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

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Query_Works(CaseData caseData)
        {
            // Arrange
            var executor = GetQueryExecutor(options =>
            {
                options.ShouldValidate = (ctx, arg) => ctx.Operation.Operation == OperationType.Query;
            });

            var query = "query { read(foo: " + caseData.FooInput + ", bar: " + caseData.BarInput + ") }";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(caseData);
            await Verifier.Verify(result, verifySettings);
        }

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

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Mutation_Works(CaseData caseData)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();
            services.AddFairyBread(options =>
            {
                options.AssembliesToScanForValidators = new[] { typeof(FooInputDtoValidator).Assembly };
            });
            var serviceProvider = services.BuildServiceProvider();

            var schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddServices(serviceProvider)
                .UseFairyBread()
                .Create();

            var query = "mutation { write(foo: " + caseData.FooInput + ", bar: " + caseData.BarInput + ") }";

            var executor = schema.MakeExecutable(builder =>
            {
                builder
                    .UseDefaultPipeline()
                    .AddErrorFilter<ValidationErrorFilter>();
            });

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(caseData);
            await Verifier.Verify(result, verifySettings);
        }

        // TODO: Unit tests for:
        // - cancellation

        public class QueryType
        {
            public string Read(FooInputDto foo, BarInputDto bar) => $"{foo}; {bar}";
        }

        public class MutationType
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

        public class BarInputDtoValidator : AbstractValidator<BarInputDto>
        {
            public BarInputDtoValidator()
            {
                RuleFor(x => x.EmailAddress).NotNull();
            }
        }

        public class BarInputDtoAsyncValidator : AbstractValidator<BarInputDto>
        {
            public BarInputDtoAsyncValidator()
            {
                RuleFor(x => x.EmailAddress)
                    // TODO: Cancellation unit test
                    .MustAsync((val, cancellationToken) => Task.FromResult(val == "ben@lol.com"));
            }
        }
    }
}
