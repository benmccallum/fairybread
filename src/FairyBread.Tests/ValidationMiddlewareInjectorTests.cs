using System;
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
    public class ValidationMiddlewareInjectorTests
    {
        private static async Task<IRequestExecutor> GetRequestExecutorAsync(
            Action<IFairyBreadOptions>? configureOptions = null,
            Action<IServiceCollection>? configureServices = null,
            bool registerValidators = true)
        {
            var services = new ServiceCollection();
            configureServices?.Invoke(services);

            if (registerValidators)
            {
                services.AddValidator<IntValidator, int>();
                services.AddValidator<NullableIntValidator, int?>();
                services.AddValidator<BoolValidator, bool>();
                services.AddValidator<NullableBoolValidator, bool?>();
                services.AddValidator<TestInputValidator, TestInput>();
                services.AddValidator<ArrayOfNullableIntValidator, int?[]>();
                services.AddValidator<ListOfNullableIntValidator, List<int?>>();
                services.AddValidator<ListOfListOfNullableIntValidator, List<List<int?>>>();
            }

            var builder = services
                .AddGraphQL()
                .AddQueryType<QueryIType>()
                .AddType<TestInputType>()
                .AddFairyBread(options =>
                {
                    configureOptions?.Invoke(options);
                });

            return await builder
                .BuildRequestExecutorAsync();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Works(bool registerValidators)
        {
            // Arrange
            var executor = await GetRequestExecutorAsync(
                registerValidators: registerValidators,
                configureOptions: options =>
                {
                    if (!registerValidators)
                    {
                        options.ThrowIfNoValidatorsFound = false;
                    }
                });
            
            var query = "query { " +
                "noArgs " +
                "scalarArgsA(a: 0, b: false) " +
                "scalarArgsB(a: 0, b: false) " +
                "scalarArgsC(a: 0, b: false) " +
                "nullableScalarArgsA(a: 0, b: false) " +
                "nullableScalarArgsB(a: 0, b: false) " +
                "nullableScalarArgsC(a: 0, b: false) " +
                "objectArgA(input: { a: 0, b: false }) " +
                "objectArgB(input: { a: 0, b: false }) " +
                "objectArgC(input: { a: 0, b: false }) " +
                "arrayArgA(items: [0, 0]) " +
                "listArgA(items: [0, 0]) " +
                "listArgB(items: [0, 0]) " +
                "listArgC(items: [0, 0]) " +
                "listOfListArgC(items: [[0, 0], [0, 0]]) " +
                "}";

            // Act
            var result = await executor.ExecuteAsync(query);

            // Assert
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(registerValidators);
            await Verifier.Verify(result, verifySettings);
        }

#pragma warning disable CA1822 // Mark members as static
        public class QueryI
        {
            public string NoArgs => "foo";


            public string ScalarArgsA(int a, bool b) => $"{a} | {b}";

            public string NullableScalarArgsA(int? a, bool? b) => $"{a} | {b}";

            public string ObjectArgA(TestInput input) => input.ToString();

            public string ArrayArgA(int?[] items) => string.Join(", ", items);

            public string ListArgA(List<int?> items) => string.Join(", ", items);
        }

        public class QueryIType
            : ObjectType<QueryI>
        {
            protected override void Configure(IObjectTypeDescriptor<QueryI> descriptor)
            {
                descriptor
                    .Field("scalarArgsB")
                    .Argument("a", arg => arg.Type<NonNullType<IntType>>())
                    .Argument("b", arg => arg.Type<NonNullType<BooleanType>>())
                    .Type<StringType>()
                    .ResolveWith<QueryIType>(x => x.ScalarArgsBResolver(default, default));

                descriptor
                    .Field("scalarArgsC")
                    .Argument("a", arg => arg.Type<NonNullType<IntType>>())
                    .Argument("b", arg => arg.Type<NonNullType<BooleanType>>())
                    .Type<StringType>()
                    .Resolver(ctx => "hello");

                descriptor
                    .Field("nullableScalarArgsB")
                    .Argument("a", arg => arg.Type<IntType>())
                    .Argument("b", arg => arg.Type<BooleanType>())
                    .Type<StringType>()
                    .ResolveWith<QueryIType>(x => x.NullableScalarArgsBResolver(default, default));;

                descriptor
                    .Field("nullableScalarArgsC")
                    .Argument("a", arg => arg.Type<IntType>())
                    .Argument("b", arg => arg.Type<BooleanType>())
                    .Type<StringType>()
                    .Resolver(ctx => "hello");

                descriptor
                    .Field("objectArgB")
                    .Argument("input", arg => arg.Type<TestInputType>())
                    .Type<StringType>()
                    .ResolveWith<QueryIType>(x => x.ObjArgResolver(default!));

                descriptor
                    .Field("objectArgC")
                    .Argument("input", arg => arg.Type<TestInputType>())
                    .Type<StringType>()
                    .Resolver(ctx => "hello");

                descriptor
                    .Field("listArgB")
                    .Argument("items", arg => arg.Type<NonNullType<ListType<IntType>>>())
                    .Type<StringType>()
                    .ResolveWith<QueryIType>(x => x.ListArgResolver(default!));

                descriptor
                    .Field("listArgC")
                    .Argument("items", arg => arg.Type<NonNullType<ListType<IntType>>>())
                    .Type<StringType>()
                    .Resolver(ctx => "hello");

                descriptor
                    .Field("listOfListArgC")
                    .Argument("items", arg => arg.Type<NonNullType<ListType<NonNullType<ListType<IntType>>>>>())
                    .Type<StringType>()
                    .Resolver(ctx => "hello");
            }

            public string ScalarArgsBResolver(int a, bool b) => $"{a} | {b}";
            public string NullableScalarArgsBResolver(int? a, bool? b) => $"{a?.ToString() ?? "null"} | {b?.ToString() ?? "null"}";
            public string ObjArgResolver(TestInput input) => input.ToString();
            public string ListArgResolver(List<int> items) => string.Join(",", items);
        }


        public class TestInput
        {
            public int A { get; set; }

            public bool B { get; set; }

            public override string ToString() => $"{A} | {B}";
        }

        public class TestInputType : InputObjectType<TestInput>
        {

        }

        public class IntValidator : AbstractValidator<int>
        {
            public IntValidator()
            {
                RuleFor(x => x).NotEmpty();
            }
        }

        public class NullableIntValidator : AbstractValidator<int?>
        {
            public NullableIntValidator()
            {
                RuleFor(x => x).NotEmpty();
            }
        }

        public class BoolValidator : AbstractValidator<bool>
        {
            public BoolValidator()
            {
                RuleFor(x => x).NotEmpty();
            }
        }

        public class NullableBoolValidator : AbstractValidator<bool?>
        {
            public NullableBoolValidator()
            {
                RuleFor(x => x).NotEmpty();
            }
        }

        public class TestInputValidator : AbstractValidator<TestInput>
        {
            public TestInputValidator()
            {
                RuleFor(x => x.A).NotEmpty();
                RuleFor(x => x.B).NotEmpty();
            }
        }

        public class ArrayOfNullableIntValidator : AbstractValidator<int?[]>
        {
            public ArrayOfNullableIntValidator()
            {
                RuleForEach(x => x).NotEmpty().GreaterThan(0); // TODO: Log FluentValidation issue as GreaterThan(0) shouldn't be required...
            }
        }

        public class ListOfNullableIntValidator : AbstractValidator<List<int?>>
        {
            public ListOfNullableIntValidator()
            {
                RuleForEach(x => x).NotEmpty().GreaterThan(0); // TODO: Log FluentValidation issue as GreaterThan(0) shouldn't be required...
            }
        }

        public class ListOfListOfNullableIntValidator : AbstractValidator<List<List<int?>>>
        {
            public ListOfListOfNullableIntValidator(
                ListOfNullableIntValidator innerValidator)
            {
                RuleForEach(x => x)
                    .SetValidator(innerValidator);
            }
        }
    }
}
