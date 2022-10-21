namespace FairyBread.Tests;

[UsesVerify]
public class InjectorTests
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
            services.AddValidator<PositiveIntValidator, int>();
            services.AddValidator<TestInputExplicitValidator, TestInput>();
        }

        var builder = services
            .AddGraphQL()
            .AddQueryType<QueryIType>()
            .AddType<TestInputType>()
            .AddSorting()
            .AddFiltering()
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
                    "scalarArgsD(a: 0, b: false) " +
                    "nullableScalarArgsA(a: 0, b: false) " +
                    "nullableScalarArgsB(a: 0, b: false) " +
                    "nullableScalarArgsC(a: 0, b: false) " +
                    "nullableScalarArgsD(a: 0, b: false) " +
                    "objectArgA(input: { a: 0, b: false }) " +
                    "objectArgB(input: { a: 0, b: false }) " +
                    "objectArgC(input: { a: 0, b: false }) " +
                    "objectArgD(input: { a: 0, b: false }) " +
                    "arrayArgA(items: [0, 0]) " +
                    "listArgA(items: [0, 0]) " +
                    "listArgB(items: [0, 0]) " +
                    "listArgC(items: [0, 0]) " +
                    "listArgD(items: [0, 0]) " +
                    "listOfListArgC(items: [[0, 0], [0, 0]]) " +
                    "filterSortAndPagingArgs(first: 10) { nodes { a } }" +
                    "}";

        // Act
        var result = await executor.ExecuteAsync(query);

        // Assert
        var verifySettings = new VerifySettings();
        verifySettings.UseParameters(registerValidators);
        await Verifier.Verify(result, verifySettings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Respect_ExplicitValidationAttributes(bool valid)
    {
        // Arrange
        var executor = await GetRequestExecutorAsync();

        var args = valid
            ? @"fooInt: 1,
                    barInt: 1,
                    lolInt: 1,
                    fooInput: { a: 1, b: true },
                    barInput: { a: 1, b: true },
                    lolInput: { a: 1, b: true },
                    dblInput: { a: 1, b: true }"
            : @"fooInt: -1,
                    barInt: -1,
                    lolInt: -1,
                    fooInput: { a: 0, b: false },
                    barInput: { a: 0, b: false },
                    lolInput: { a: 0, b: false },
                    dblInput: { a: 0, b: false }";

        var query = @"
                query {
                    readWithExplicitValidation(" + args + @")
                }";

        // Act
        var result = await executor.ExecuteAsync(query);

        // Assert
        await Verifier.Verify(result).UseParameters(valid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Respect_ExplicitValidationFluent(bool valid)
    {
        // Arrange
        var executor = await GetRequestExecutorAsync();

        var args = valid
            ? @"fooInt: 1,
                    barInt: 1,
                    lolInt: 1,
                    fooInput: { a: 1, b: true },
                    barInput: { a: 1, b: true },
                    lolInput: { a: 1, b: true },
                    dblInput: { a: 1, b: true }"
            : @"fooInt: -1,
                    barInt: -1,
                    lolInt: -1,
                    fooInput: { a: 0, b: false },
                    barInput: { a: 0, b: false },
                    lolInput: { a: 0, b: false },
                    dblInput: { a: 0, b: false }";

        var query = @"
                query {
                    readWithExplicitValidationFluent(" + args + @")
                }";

        // Act
        var result = await executor.ExecuteAsync(query);

        // Assert
        await Verifier.Verify(result).UseParameters(valid);
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

        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IEnumerable<FooI> GetFilterSortAndPagingArgs() => new FooI[] { new FooI() };

        public string ReadWithExplicitValidation(
            // Should validate explicitly
            [Validate(typeof(PositiveIntValidator))]
            int fooInt,
            // Shouldn't validate implicitly
            [Validate(typeof(PositiveIntValidator))]
            [DontValidateImplicitly]
            int barInt,
            // Shouldn't validate
            [Validate(typeof(PositiveIntValidator))]
            [DontValidate]
            int lolInt,
            // Should validate explicitly
            [Validate(typeof(TestInputExplicitValidator))]
            TestInput fooInput,
            // Shouldn't validate implicitly
            [Validate(typeof(TestInputExplicitValidator))]
            [DontValidateImplicitly]
            TestInput barInput,
            // Shouldn't validate
            [Validate(typeof(TestInputExplicitValidator))]
            [DontValidate]
            TestInput lolInput,
            // Shouldn't add an implicitly added validator again
            [Validate(typeof(TestInputValidator))]
            TestInput dblInput)
        {
            return $"{fooInt} {barInt} {lolInt} {fooInput} {barInput} {lolInput}";
        }
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
                .Resolve(ctx => "hello");

            descriptor
                .Field("scalarArgsD")
                .Argument("a", arg => arg.Type(typeof(int)))
                .Argument("b", arg => arg.Type(typeof(bool)))
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("nullableScalarArgsB")
                .Argument("a", arg => arg.Type<IntType>())
                .Argument("b", arg => arg.Type<BooleanType>())
                .Type<StringType>()
                .ResolveWith<QueryIType>(x => x.NullableScalarArgsBResolver(default, default));

            descriptor
                .Field("nullableScalarArgsC")
                .Argument("a", arg => arg.Type<IntType>())
                .Argument("b", arg => arg.Type<BooleanType>())
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("nullableScalarArgsD")
                .Argument("a", arg => arg.Type(typeof(int?)))
                .Argument("b", arg => arg.Type(typeof(bool?)))
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("objectArgB")
                .Argument("input", arg => arg.Type<TestInputType>())
                .Type<StringType>()
                .ResolveWith<QueryIType>(x => x.ObjArgResolver(default!));

            descriptor
                .Field("objectArgC")
                .Argument("input", arg => arg.Type<TestInputType>())
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("objectArgD")
                .Argument("input", arg => arg.Type(typeof(TestInput)))
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("listArgB")
                .Argument("items", arg => arg.Type<NonNullType<ListType<IntType>>>())
                .Type<StringType>()
                .ResolveWith<QueryIType>(x => x.ListArgResolver(default!));

            descriptor
                .Field("listArgC")
                .Argument("items", arg => arg.Type<NonNullType<ListType<IntType>>>())
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("listArgD")
                .Argument("items", arg => arg.Type(typeof(List<int?>)))
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("listOfListArgC")
                .Argument("items", arg => arg.Type<NonNullType<ListType<NonNullType<ListType<IntType>>>>>())
                .Type<StringType>()
                .Resolve(ctx => "hello");

            descriptor
                .Field("readWithExplicitValidationFluent")
                // Should validate explicitly
                .Argument("fooInt", arg => arg.Type<IntType>().ValidateWith<PositiveIntValidator>())
                // Shouldn't validate implicitly
                .Argument("barInt", arg => arg.Type<IntType>().ValidateWith<PositiveIntValidator>().DontValidateImplicitly())
                // Shouldn't validate
                .Argument("lolInt", arg => arg.Type<IntType>().ValidateWith<PositiveIntValidator>().DontValidate())
                // Should validate explicitly
                .Argument("fooInput", arg => arg.Type<TestInputType>().ValidateWith<TestInputExplicitValidator>())
                // Shouldn't validate implicitly
                .Argument("barInput", arg => arg.Type<TestInputType>().ValidateWith<TestInputExplicitValidator>().DontValidateImplicitly())
                // Shouldn't validate
                .Argument("lolInput", arg => arg.Type<TestInputType>().ValidateWith<TestInputExplicitValidator>().DontValidate())
                // Shouldn't add an implicitly added validator again
                .Argument("dblInput", arg => arg.Type<TestInputType>().ValidateWith<TestInputValidator>())
                .ResolveWith<QueryI>(q => q.ReadWithExplicitValidation(default, default, default, default!, default!, default!, default!));
        }

        public string ScalarArgsBResolver(int a, bool b) => $"{a} | {b}";
        public string NullableScalarArgsBResolver(int? a, bool? b) => $"{a?.ToString() ?? "null"} | {b?.ToString() ?? "null"}";
        public string ObjArgResolver(TestInput input) => input.ToString();
        public string ListArgResolver(List<int> items) => string.Join(",", items);
    }

    public class FooI
    {
        public string A { get; set; } = "A";
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
            RuleFor(x => x).NotEmpty().GreaterThan(0);
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
            RuleFor(x => x).NotEmpty().Equal(true);
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

    public class TestInputExplicitValidator : AbstractValidator<TestInput>, IExplicitUsageOnlyValidator
    {
        public TestInputExplicitValidator()
        {
            RuleFor(x => x.A).NotNull().GreaterThan(0).WithMessage("Explicit validator error msg.");
        }
    }

    public class ArrayOfNullableIntValidator : AbstractValidator<int?[]>
    {
        public ArrayOfNullableIntValidator()
        {
            RuleForEach(x => x).NotEmpty().GreaterThan(0);
        }
    }

    public class ListOfNullableIntValidator : AbstractValidator<List<int?>>
    {
        public ListOfNullableIntValidator()
        {
            RuleForEach(x => x).NotEmpty().GreaterThan(0);
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

    public class PositiveIntValidator : AbstractValidator<int>, IExplicitUsageOnlyValidator
    {
        public PositiveIntValidator()
        {
            RuleFor(x => x).NotNull().GreaterThan(0);
        }
    }
}
