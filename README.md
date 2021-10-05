<div align="center">
  <img alt="fairybread" src="logo.svg" height="200px">
  <p>
    Input validation for Hot Chocolate.
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
// Add your FluentValidation validators
services.AddValidatorsFromAssemblyContaining<CreateUserInputValidator>();

// Add FairyBread
services
    .AddGraphQL()
    .AddFairyBread();
```

Configure [FluentValidation](https://github.com/FluentValidation/FluentValidation) validators on your input types.


```csharp
public class UserInput { ... }

public class UserInputValidator : AbstractValidator<CreateUserInput> { ... }

// An example GraphQL field in Hot Chocolate
public Task CreateUser(CreateUserInput userInput) { ... }
```

### How validation errors will be handled

Errors will be written out into the GraphQL execution result in the `Errors` property with one error being reported per failure on a field.

### Dealing with multi-threaded execution issues

GraphQL resolvers are inherently multi-threaded; as such, you can run into issues injecting things like an EntityFramework `DbContext` into a field resolver (or something it uses) which doesn't allow muli-thread usage. One solution for this is to resolve `DbContext` into its own "scope", rather than the default scope (for an ASP.NET Core application is tied to the HTTP Request).

With FairyBread, you might need to do this if one of your validators uses a `DbContext` (say to check if a username already exists on a create user mutation). Good news is, it's as easy as marking your validator with `IRequiresOwnScopeValidator` and we'll take care of the rest.

```csharp
public class UserInputValidator : AbstractValidator<UserInput>, IRequiresOwnScopeValidator
{
    // db will be a unique instance for this validation operation
    public UserInputValidator(SomeDbContext db) { ... } 
}
```

### Using MediatR?

If you want to let MediatR fire validation, you can set up:
* FairyBread to skip validating `MediatR.IRequest` arguments, 
* your MediatR pipeline to validate them and throw a `ValidationException`, and
* an `IErrorFilter`(in Hot Chocolate) to handle it handles using `FairyBread.DefaultValidationErrorsHandler` to report the errors.

### Where to next?

For more examples, please see the tests.

## Customization

FairyBread was built with customization in mind.

You can tweak the default settings as needed:

```csharp
services.AddFairyBread(options =>
{
    options.ShouldValidateArgument = (objTypeDef, fieldTypeDef, argTypeDef) => ...;
    options.ThrowIfNoValidatorsFound = true/false;
});
```

You can completely swap in your own concrete implementations of bits with DI.
These could be based on the default implementations whose methods can be overridden.

See <a href="src/FairyBread.Tests/CustomizationTests.cs">CustomizationTests.cs</a>.

## Backlog

See issues.

## Compatibility

FairyBread depends on [HotChocolate.Execution](https://www.nuget.org/packages/HotChocolate.Execution)
which can bring breaking changes from time to time and require a major bump our end. This is also the case
for FluentValidation.
Compatibility is listed below.

We strive to match Hot Chocolate's supported .NET target frameworks, though this might not always be possible.

| HotChocolate | FluentValidation | FairyBread | FairyBread docs |
| ------------ | ---------------- | ---------- | --------------- |
|          v10 |               v8 |         v1 | [/v1/main](https://github.com/benmccallum/fairybread/tree/v1/main) branch |
|          v11 |               v8 |         v2 | [/v2/main](https://github.com/benmccallum/fairybread/tree/v2/main) branch |
|          v11 |               v9 |         v3 | [/v3/main](https://github.com/benmccallum/fairybread/tree/v3/main) branch |
|          v11 |               v9 |         v4 | [/v4/main](https://github.com/benmccallum/fairybread/tree/v4/main) branch |
|     v11.0.9* |               v9 |     v4.1.1 | [/v4/main](https://github.com/benmccallum/fairybread/tree/v4/main) branch |
|      v11.0.9 |               v9 |         v5 | [/v5/main](https://github.com/benmccallum/fairybread/tree/v5/main) branch |
|      v11.0.9 |              v10 |         v6 | [/v6/main](https://github.com/benmccallum/fairybread/tree/v6/main) branch |
|      v11.0.9 |              v10 |         v7 | [/v7/main](https://github.com/benmccallum/fairybread/tree/v7/main) branch |
|      v12.0.1 |              v10 |         v8 | right here |


\* Denotes unexpected binary incompatibility / breaking change in Hot Chocolate

## What the heck is a fairy bread?

A (bizarre) Australian food served at children's parties. Since I'm Australian and HotChocolate has a lot of 
project names with sweet tendencies, this seemed like a fun name for the project.

According to [wikipedia](https://en.wikipedia.org/wiki/Fairy_bread):
> "Fairy bread is sliced white bread spread with butter or margarine and covered with sprinkles or "hundreds and thousands", served at children's parties in Australia and New Zealand. It is typically cut into triangles."
