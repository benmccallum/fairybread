using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class InputValidationMiddlewareTests
    {
        private readonly VerifySettings _verifySettings;

        public InputValidationMiddlewareTests()
        {
            _verifySettings = new VerifySettings();
            //_verifySettings.ScrubLinesContaining(@"""Id"":");
        }

        [Fact]
        public Task Works()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            
            // TODO: Use FluentValidation.DependencyInjection extension
            serviceCollection.AddTransient<MyInputDtoValidator>();

            // TODO: Move into helper setup extension
            serviceCollection.AddSingleton<IFairyBreadOptions>(sp => new FairyBreadOptions
            {
                AssembliesToScanForValidators = new Assembly[] { typeof(MyInputDtoValidator).Assembly }
            });
            serviceCollection.AddSingleton<IValidatorBag, ValidatorBag>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .Use<InputValidationMiddleware>()
                .AddServices(serviceProvider)
                .Create();

            var input = @"{ someInteger: 1, someString: ""hello"" }";
            var query = "query { foo(input: " + input + ") }";

            // Act
            var executor = schema.MakeExecutable();
            var result = executor.ExecuteAsync(query);

            // Assert
            return Verifier.Verify(result, _verifySettings);
        }



        public class QueryType
        {
            public string Foo(MyInputDto input) => input.ToString();
        }

        public class MutationType
        {
            public string Bar(MyInputDto input) => input.ToString();
        }

        public class MyInput : InputObjectType<MyInputDto>
        {
            protected override void Configure(IInputObjectTypeDescriptor<MyInputDto> descriptor)
            {
                descriptor.BindFieldsImplicitly();
            }
        }

#nullable disable
        public class MyInputDto
        {
            public int SomeInteger { get; set; }
            
            [GraphQLType(typeof(NonNullType<StringType>))]
            public string SomeString { get; set; }

            public override string ToString()
            {
                return $"SomeInteger: {SomeInteger}, " +
                    $"SomeString: {SomeString}";
            }
        }
#nullable enable

        public class MyInputDtoValidator : AbstractValidator<MyInputDto>
        {
            public MyInputDtoValidator()
            {
                RuleFor(x => x.SomeInteger).Equals(1);
                RuleFor(x => x.SomeString).Equals("hello");
            }
        }
    }
}
