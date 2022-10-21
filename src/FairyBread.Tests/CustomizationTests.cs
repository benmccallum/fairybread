namespace FairyBread.Tests;

[UsesVerify]
public class CustomizationTests
{
    private const string Query = @"query { read(foo: { someInteger: 1, someString: ""hello"" }) }";

    private static async Task<IRequestExecutor> GetRequestExecutorAsync(
        Action<IServiceCollection> preBuildProviderAction)
    {
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining<CustomValidator>();
        preBuildProviderAction?.Invoke(services);

        return await services
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            .AddFairyBread()
            .BuildRequestExecutorAsync();
    }

    [Fact]
    public async Task CustomValidationResultHandler_Works()
    {
        // Arrange
        var executor = await GetRequestExecutorAsync(services =>
        {
            services.AddSingleton<IValidationErrorsHandler, CustomValidationErrorsHandler>();
        });

        // Act
        var result = await executor.ExecuteAsync(Query) as IQueryResult;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.Errors);
        Assert.NotEmpty(result.Errors);
        Assert.True(result.Errors!.All(e => e.Message == "lol"));
    }

    public class CustomValidationErrorsHandler : DefaultValidationErrorsHandler
    {
        protected override IErrorBuilder CreateErrorBuilder(IMiddlewareContext context, string argName, IValidator val, ValidationFailure failure) =>
            base.CreateErrorBuilder(context, argName, val, failure)
                .SetMessage("lol");
    }

    [Fact]
    public async Task CustomValidatorProvider_Works()
    {
        // Arrange
        var executor = await GetRequestExecutorAsync(services =>
        {
            services.AddSingleton<IValidatorProvider, CustomValidatorProvider>();
        });

        // Act
        var result = await executor.ExecuteAsync(Query);

        // Assert
        await Verifier.Verify(result);
    }

    public class CustomValidatorProvider : DefaultValidatorProvider
    {
        public CustomValidatorProvider(IServiceProvider serviceProvider, IFairyBreadOptions options)
            : base(null!) { }

        public override IEnumerable<ResolvedValidator> GetValidators(IMiddlewareContext context, IInputField argument)
            => argument.RuntimeType == typeof(FooInputDto)
                ? (new ResolvedValidator[] { new ResolvedValidator(new CustomValidator()) })
                : base.GetValidators(context, argument);
    }

#pragma warning disable CA1822 // Mark members as static
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

    public class CustomValidator : AbstractValidator<FooInputDto>
    {
        public CustomValidator()
        {
            RuleFor(x => x.SomeInteger)
                .GreaterThanOrEqualTo(999);
        }
    }
}
