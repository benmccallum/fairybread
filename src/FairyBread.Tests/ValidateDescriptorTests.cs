using System.Threading.Tasks;
using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class ValidateDescriptorTests
    {
        private const string QueryOperation = @"
            query {
                read(foo: { someInteger: -1, someString: ""hello"" }),
                readWithValidate(foo: { someInteger: -1, someString: ""hello"" }),
                readWithUseValidate(foo: { someInteger: -1, someString: ""hello"" })
            }";

        private const string MutationOperation = @"
            mutation {
                write(foo: { someInteger: -1, someString: ""hello"" }),
                writeWithValidate(foo: { someInteger: -1, someString: ""hello"" }),
                writeWithUseValidate(foo: { someInteger: -1, someString: ""hello"" })
            }";

        private IQueryExecutor InitQueryExecutor()
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();
            services.AddFairyBread(options =>
            {
                options.AssembliesToScanForValidators = new[] { typeof(FooInputDtoValidator).Assembly };

                // Disable validation for everything here
                // so we can test that it validates when descriptor is applied
                options.ShouldValidate = (ctx, arg) => false;
            });

            var serviceProvider = services.BuildServiceProvider();

            var schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                    .AddType<QueryTypeExtension>()
                .AddMutationType<MutationType>()
                    .AddType<MutationTypeExtension>()
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
        public async Task Query_Works()
        {
            // Arrange
            var queryExecutor = InitQueryExecutor();

            // Act
            var result = await queryExecutor.ExecuteAsync(QueryOperation);

            // Assert
            await Verifier.Verify(result);
        }

        [Fact]
        public async Task Mutation_Works()
        {
            // Arrange
            var queryExecutor = InitQueryExecutor();

            // Act
            var result = await queryExecutor.ExecuteAsync(MutationOperation);

            // Assert
            await Verifier.Verify(result);
        }

        public class Query { }

        public class QueryType : ObjectType<Query>
        {
            protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor
                    .Field("readWithUseValidate")
                    .Type<NonNullType<StringType>>()
                    .Argument("foo", argDesc => argDesc
                        .Type(typeof(FooInputDto))
                        .UseValidation())
                    .Resolver(x => "lol");
            }
        }

        [ExtendObjectType(Name = nameof(Query))]
        public class QueryTypeExtension
        {
            public string Read(FooInputDto foo) => $"{foo};";

            public string ReadWithValidate([Validate] FooInputDto foo) => $"{foo};";
        }

        public class Mutation { }

        public class MutationType : ObjectType<Mutation>
        {
            protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
            {
                descriptor
                    .Field("writeWithUseValidate")
                    .Type<NonNullType<StringType>>()
                    .Argument("foo", argDesc => argDesc
                        .Type(typeof(FooInputDto))
                        .UseValidation())
                    .Resolver(x => "lol");
            }
        }

        [ExtendObjectType(Name = nameof(Mutation))]
        public class MutationTypeExtension
        {
            public string Write(FooInputDto foo) => $"{foo};";

            public string WriteWithValidate([Validate] FooInputDto foo) => $"{foo};";
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
    }
}
