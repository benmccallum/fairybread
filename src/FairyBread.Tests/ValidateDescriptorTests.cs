using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class ValidateDescriptorTests
    {
        private static async Task<IRequestExecutor> GetRequestExecutorAsync()
        {
            var services = new ServiceCollection();
            services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();

            return await services
                .AddGraphQL()
                .AddQueryType<QueryType>()
                    .AddType<QueryTypeExtension>()
                .AddMutationType<MutationType>()
                    .AddType<MutationTypeExtension>()
                .AddType<FooInputDto>()
                .AddType<BarInputType>()
                .AddType<LolInputDto>()
                .AddFairyBread(options =>
                {
                    options.AssembliesToScanForValidators = new[] { typeof(FooInputDtoValidator).Assembly };
                    options.ShouldValidate = DefaultFairyBreadOptions.DefaultImplementations.ShouldValidateBasedOnValidateDescriptor;
                })
                .BuildRequestExecutorAsync();
        }

        private static readonly Dictionary<int, string> _queries = new Dictionary<int, string>
        {
            { 1, @"read(foo: { someInteger: -1, someString: ""hello"" })" },
            { 2, @"readWithValidateOnArgument(foo: { someInteger: -1, someString: ""hello"" })" },
            { 3, @"readWithUseValidateOnArgument(foo: { someInteger: -1, someString: ""hello"" })" },
            { 4, @"readWithUseValidationOnInputType(bar: { someInteger: -1, someString: ""hello"" })" },
            { 5, @"readWithValidateOnClrType(lol: { someInteger: -1, someString: ""hello"" })" }
        };

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task Query_Works(int id)
        {
            // Arrange
            var executor = await GetRequestExecutorAsync();

            // Act
            var result = await executor.ExecuteAsync("query { " + _queries[id] + " }");

            // Assert
            await Verifier.Verify(result, GetVerifySettings(id));
        }

        private static readonly Dictionary<int, string> _mutations = new Dictionary<int, string>
        {
            { 1, @"write(foo: { someInteger: -1, someString: ""hello"" })" },
            { 2, @"writeWithValidateOnArgument(foo: { someInteger: -1, someString: ""hello"" })" },
            { 3, @"writeWithUseValidateOnArgument(foo: { someInteger: -1, someString: ""hello"" })" },
            { 4, @"writeWithUseValidationOnInputType(bar: { someInteger: -1, someString: ""hello"" })" },
            { 5, @"writeWithValidateOnClrType(lol: { someInteger: -1, someString: ""hello"" })" }
        };

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task Mutation_Works(int id)
        {
            // Arrange
            var executor = await GetRequestExecutorAsync();

            // Act
            var result = await executor.ExecuteAsync("mutation { " + _mutations[id] + " }");

            // Assert
            await Verifier.Verify(result, GetVerifySettings(id));
        }

        private VerifySettings GetVerifySettings(params object?[] parameters)
        {
            var settings = new VerifySettings();
            settings.UseParameters(parameters);
            return settings;
        }

        public class Query { }

        public class QueryType : ObjectType<Query>
        {
            protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor
                    .Field("readWithUseValidateOnArgument")
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

            public string ReadWithValidateOnArgument([Validate] FooInputDto foo) => $"{foo};";

            public string ReadWithUseValidationOnInputType(BarInputDto bar) => $"{bar};";

            public string ReadWithValidateOnClrType(LolInputDto lol) => $"{lol};";
        }

        public class Mutation { }

        public class MutationType : ObjectType<Mutation>
        {
            protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
            {
                descriptor
                    .Field("writeWithUseValidateOnArgument")
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

            public string WriteWithValidateOnArgument([Validate] FooInputDto foo) => $"{foo};";

            public string WriteWithUseValidationOnInputType(BarInputDto bar) => $"{bar};";

            public string WriteWithValidateOnClrType(LolInputDto lol) => $"{lol};";
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
            public int SomeInteger { get; set; }

            public string SomeString { get; set; } = "";

            public override string ToString() =>
                $"SomeInteger: {SomeInteger}, " +
                $"SomeString: {SomeString}";
        }

        public class BarInputType : InputObjectType<BarInputDto>
        {
            protected override void Configure(IInputObjectTypeDescriptor<BarInputDto> descriptor)
            {
                descriptor.UseValidation();
            }
        }

        public class BarInputDtoValidator : AbstractValidator<BarInputDto>
        {
            public BarInputDtoValidator()
            {
                RuleFor(x => x.SomeInteger).Equal(1);
                RuleFor(x => x.SomeString).Equal("hello");
            }
        }

        [Validate]
        public class LolInputDto
        {
            public int SomeInteger { get; set; }

            public string SomeString { get; set; } = "";

            public override string ToString() =>
                $"SomeInteger: {SomeInteger}, " +
                $"SomeString: {SomeString}";
        }
        public class LolInputDtoValidator : AbstractValidator<LolInputDto>
        {
            public LolInputDtoValidator()
            {
                RuleFor(x => x.SomeInteger).Equal(1);
                RuleFor(x => x.SomeString).Equal("hello");
            }
        }
    }
}
