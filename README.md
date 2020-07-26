<div align="center">
  <img alt="fairybread" src="logo.svg" height="200px">
  <p>
    Input validation for HotChocolate.
  </p>
  <p>
	  <a href="https://github.com/benmccallum/fairybread/releases"><img alt="GitHub release" src="https://img.shields.io/github/release/benmccallum/fairybread.svg"></a>
	  <a href="https://www.nuget.org/packages/FairyBread"><img alt="Nuget version" src="https://img.shields.io/nuget/v/FairyBread"></a>
	  <a href="https://www.nuget.org/packages/FairyBread"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/FairyBread"></a>
  </p>
</div>

## Getting started

Install the package.

```bash
dotnet add package FairyBread
```

Hook up FairyBread in your `Startup.cs`.

```c#
// Add the FluentValidation validators
services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();

// Add FairyBread
services.AddFairyBread(options =>
{
	options.AssembliesToScanForValidators = new[] { typeof(FooInputDtoValidator).Assembly };
});

// Configure FairyBread middleware using HotChocolate's ISchemaBuilder
HotChocolate.SchemaBuilder.New()
	...
	.UseFairyBread()
	...;
```

Set up FluentValidation validators like you usually would on your input CLR types.

```c#

public class UserInput { ... }

public class UserInputValidator : AbstractValidator<UserInput> { ... }

// An example GraphQL field in HotChocolate
public Task CreateUser(UserInput userInput) { ... }
```

By default, FairyBread will validate any argument that:
* is an `InputObjectType` on a mutation operation,
* or (coming soon) is:
    * manually marked with `[Validate]` (i.e. opt-in only at the field-level)
	* it's CLR type or `InputObjectType` are marked with `[Validate]` (i.e. opt-in this type of argument globally).

> Note: by default, query operations are notably excluded. This is mostly for performance reasons.

If the default doesn't suit you, you can always change it by configuring `IFairyBreadOptions.ShouldValidate`. See [Customization](#Customization).

For more examples, please see the tests.

## Customization

FairyBread was built with customization in mind. At configuration time, you can tweak the default settings as needed:

```c#
services.AddFairyBread(options =>
{
	options.AssembliesToScanForValidators = new[] { typeof(MyValidator).Assembly };
	options.ShouldValidate = (ctx, arg) => ...;
	options.ThrowIfNoValidatorsFound = true/false;
});
```

But it goes further than that. You can completely swap in your own options, validator provider, 
validator result handler and so on to get the functionality you need by simply adding your own 
implementation of the relevant interface before adding FairyBread, for example:

```c#
services.Add<IValidationResultHandler, CustomValidationResultHandler>();
```

If you want to just change one element of a default implementation, they aren't `sealed` and 
their methods are `virtual` so have at it. For instance, the `CustomValidationResultHandler` above might want to
modify the way error codes are set on the `IError`, which can be done like so:

```c#
public class CustomValidationResultHandler : DefaultValidationResultHandler
{
    protected override IErrorBuilder ExtractError(IMiddlewareContext context, ValidationFailure failure)
	    => base.ExtractError(context, failure)
			.SetExtension(nameof(failure.ErrorCode), $"CustomPrefix:{failure.ErrorCode}");
	}
}
```

Check out <a href="src/FairyBread.Tests/CustomizationTests.cs">CustomizationTests.cs</a> for complete examples.

## Backlog

See issues.

