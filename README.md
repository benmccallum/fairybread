<div align="center">
  <img alt="fairybread" src="logo.svg" height="200px">
  <p>
    Input validation for HotChocolate.
  </p>
  <p>
	  <a href="https://github.com/benmccallum/fairybread/releases"><img alt="GitHub release" src="https://img.shields.io/github/release/benmccallum/fairybread.svg"></a>
	  <a href="https://www.nuget.org/packages/FairyBread"><img alt="Nuget version" src="https://img.shields.io/nuget/v/FairyBread"></a>
	  <a href="https://www.nuget.org/packages/FairyBread"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/FairyBread"></a>	  
      <a href="https://codecov.io/gh/benmccallum/FairyBread">
        <img src="https://codecov.io/gh/benmccallum/FairyBread/branch/main/graph/badge.svg?token=HB3O7GR51M"/>
      </a>    
  </p>
</div>

## Getting started

Install a [compatible version](#Compatibility).

```bash
dotnet add package FairyBread
```

Configure services.

```csharp
// Add FluentValidation validators
services.AddValidatorsFromAssemblyContaining<FooInputDtoValidator>();

// Add FairyBread
services
    .AddGraphQL()
    .AddFairyBread();
```

Configure [FluentValidation](https://github.com/FluentValidation/FluentValidation) validators on your input types.

```csharp
public class UserInput { ... }

public class UserInputValidator : AbstractValidator<UserInput> { ... }

// An example GraphQL field in HotChocolate
public Task CreateUser(UserInput userInput) { ... }
```

### When validation will fire

By default, FairyBread will validate any argument that:
* is an object type in a mutation operation,
* is manually opted-in at the field level with:
    * `[Validate]` on the resolver method argument in pure code first
    * `.UseValidation()` on the argument definition in code first
* is manually opted-in at the input type level with:
    * `[Validate]` on the CLR backing type in pure code first
    * `.UseValidation()` on the InputObjectType descriptor in code first

> Note: by default, query operations are notably excluded. This is mostly for performance reasons.

If the default doesn't suit you, you can always change it by configuring `IFairyBreadOptions.ShouldValidate`. See [Customization](#Customization).
Some of the default implementations are defined publicly on the default options class so they can be re-used and composed to form your own implementation.

### How validation errors will be handled

Errors will be written out into the GraphQL execution result in the `Errors` property with one error being reported per failure on a field.

### Dealing with multi-threaded execution issues

GraphQL resolvers are inherently multi-threaded; as such, you can run into issues injecting things like an EntityFramework `DbContext` into a field resolver (or something it uses) which doesn't allow muli-thread usage. One solution for this is to resolve `DbContext` into its own "scope", rather than the default scope (for an ASP.NET Core application is tied to the HTTP Request).

With FairyBread, you might need to do this if one of your validators uses a `DbContext` (say to check if a username already exists on a create user mutation). Good news is, it's as easy as marking your validator with `IRequiresOwnScopeValidator` and we'll take care of the rest.

```csharp
public class UserInputValidator : AbstractValidator<UserInput>, IRequiresOwnScopeValidator
{
    public UserInputValidator(SomeDbContext db) { ... } // db will be a unique instance for this validation operation
}
```

### Using MediatR?

If you want to let MediatR fire validation, you can set up:
* FairyBread to skip validating `MediatR.IRequest` arguments, 
* your MediatR pipeline to validate them and throw a `ValidationException`, and
* an `IErrorFilter`(in HotChocolate) to handle it handles using `FairyBread.DefaultValidationErrorsHandler` to report the errors.

### Where to next?

For more examples, please see the tests.

## Customization

FairyBread was built with customization in mind. At configuration time, you can tweak the default settings as needed:

```csharp
services.AddFairyBread(options =>
{
    options.ShouldValidate = (ctx, arg) => ...;
    options.ThrowIfNoValidatorsFound = true/false;
});
```

Or, you can completely swap in your own options, validator provider, validation errors result handler and 
so on to get the functionality you need by simply adding your own implementation of the relevant interface 
before adding FairyBread. You can even subclass a default implementation, register it and override singular 
methods if that makes life easier.

Check out <a href="src/FairyBread.Tests/CustomizationTests.cs">CustomizationTests.cs</a>
for complete examples.

## Backlog

See issues.

## Compatibility

FairyBread depends on [HotChocolate.Execution](https://www.nuget.org/packages/HotChocolate.Execution)
which can bring breaking changes from time to time and require a major bump our end. This is also the case
for FluentValidation.
Compatibility is listed below.

We strive to match HotChocolate's supported target frameworks, though this might not always be possible.

| HotChocolate | FluentValidation | FairyBread | FairyBread docs |
| ------------ | ---------------- | ---------- | --------------- |
|          v10 |               v8 |         v1 | [/v1/main](https://github.com/benmccallum/fairybread/tree/v1/main) branch |
|          v11 |               v8 |         v2 | [/v2/main](https://github.com/benmccallum/fairybread/tree/v2/main) branch |
|          v11 |               v9 |         v3 | [/v3/main](https://github.com/benmccallum/fairybread/tree/v3/main) branch |
|          v11 |               v9 |         v4 |      right here |
|     v11.0.9* |               v9 |     v4.1.1 |      right here |

* Unexpected binary incompatibility / breaking change in HotChocolate

## What the heck is a fairy bread?

A (bizarre) Australian food served at children's parties. Since I'm Australian and HotChocolate has a lot of 
project names with sweet tendencies, this seemed like a fun name for the project.

According to [wikipedia](https://en.wikipedia.org/wiki/Fairy_bread):
> "Fairy bread is sliced white bread spread with butter or margarine and covered with sprinkles or "hundreds and thousands", served at children's parties in Australia and New Zealand. It is typically cut into triangles."
