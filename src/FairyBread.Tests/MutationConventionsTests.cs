namespace FairyBread.Tests;

[UsesVerify]
public class MutationsConventionTests
{
    static MutationsConventionTests()
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
            services.AddValidator<ArrayOfFooInputDtoValidator, FooInputDto[]>();
            services.AddValidator<ListOfFooInputDtoValidator, List<FooInputDto>>();
            services.AddValidator<BarInputDtoValidator, BarInputDto>();
            services.AddValidator<BarInputDtoAsyncValidator, BarInputDto>();
            services.AddValidator<NullableIntValidator, int?>();
        }

        var builder = services
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddMutationConventions()
            .AddFairyBread(options =>
            {
                configureOptions?.Invoke(options);
            });

        return await builder
            .BuildRequestExecutorAsync();
    }

    [Fact]
    public async Task SchemaWorks()
    {
        // Arrange
        var executor = await GetRequestExecutorAsync();

        // Act
        var result = executor.Schema.ToString();

        // Assert
        await Verifier.Verify(result);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Mutation_Works(CaseData caseData)
    {
        // Arrange
        var executor = await GetRequestExecutorAsync();

        var query = $$"""
            mutation {
              write(foo: {{caseData.FooInput}}, bar: {{caseData.BarInput}}) {
                string
                errors {
                  __typename
                }
              }
            }
            """;

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

#pragma warning disable CA1822 // Mark members as static
    public class Query
    {
        public string Meh => "meh";
    }

    public class Mutation
    {
        public string Write(FooInputDto foo, BarInputDto bar) => $"{foo}; {bar}";
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

    public class ArrayOfFooInputDtoValidator : AbstractValidator<FooInputDto[]>
    {
        public ArrayOfFooInputDtoValidator()
        {
            RuleForEach(x => x).SetValidator(new FooInputDtoValidator());
        }
    }

    public class ListOfFooInputDtoValidator : AbstractValidator<List<FooInputDto>>
    {
        public ListOfFooInputDtoValidator()
        {
            RuleForEach(x => x).SetValidator(new FooInputDtoValidator());
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
