using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

namespace FairyBread
{
    internal class ValidationMiddlewareInjector : TypeInterceptor
    {
        private FieldMiddleware? _validationFieldMiddleware;

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is not ObjectTypeDefinition objTypeDef)
            {
                return;
            }

            var options = completionContext.Services
                .GetRequiredService<IFairyBreadOptions>();

            foreach (var fieldDef in objTypeDef.Fields)
            {
                var needsValidationMiddleware = false;

                foreach (var argDef in fieldDef.Arguments)
                {
                    if (options.ShouldValidateArgument(objTypeDef, fieldDef, argDef))
                    {
                        var argCoord = new FieldCoordinate(objTypeDef.Name, fieldDef.Name, argDef.Name);

                        needsValidationMiddleware = true;
                        argDef.ContextData[WellKnownContextData.ShouldValidate] = true;
                    }
                }

                if (needsValidationMiddleware)
                {
                    if (_validationFieldMiddleware is null)
                    {
                        _validationFieldMiddleware = FieldClassMiddlewareFactory
                            .Create<ValidationMiddleware>();
                    }

                    fieldDef.MiddlewareDefinitions.Insert(
                        0,
                        new FieldMiddlewareDefinition(_validationFieldMiddleware));
                }
            }
        }

        private IReadOnlyList<ValidatorDescriptor> DetermineValidatorsForArg(
            ObjectTypeDefinition objTypeDef,
            ObjectFieldDefinition fieldDef,
            ArgumentDefinition argDef)
        {
            // Explicit validate attribute
            ValidateAttribute? validateAttr = null;
            if (argDef.ContextData.TryGetValue(WellKnownContextData.ValidateAttribute, out var rawAttr) &&
                rawAttr is ValidateAttribute valAttr)
            {
                validateAttr = valAttr;
                var isInited = ValidatorRegistry.CacheByFieldCoord.ContainsKey(argument.Coordinate);
                if (!isInited)
                {
                    var validatorDescriptors =
                        ValidatorRegistry.CacheByFieldCoord[argument.Coordinate] =
                        new List<ValidatorDescriptor>();

                    // Unravel them into the cache
                    foreach (var validatorType in validateAttr.ValidatorTypes)
                    {
                        // TODO: v8.0.0, if validator has already been resolved in implicit validators, don't add it again
                        // (In case someone has added an explicit one without realising it'd already be picked up implicitly)

                        var requiresOwnScope = ValidatorRegistry.ShouldBeResolvedInOwnScope(validatorType);
                        validatorDescriptors.Add(new ValidatorDescriptor(validatorType, requiresOwnScope));
                    }
                }

                // Clear now we're done with marker
                argDef.ContextData[WellKnownContextData.ValidateAttribute] = null;
            }

            // Implicit validator/s
            if (validateAttr is null || validateAttr.RunImplicitValidators)
            {
                if (ValidatorRegistry.CacheByArgType.TryGetValue(argument.RuntimeType, out var validatorDescriptors) &&
                    validatorDescriptors is not null)
                {
                    ProcessDescriptors(context, validators, validatorDescriptors);
                }
            }

            // Explicit validator/s
            if (validateAttr is not null)
            {
                var validatorDescriptors = ValidatorRegistry.CacheByFieldCoord[argument.Coordinate];
                ProcessDescriptors(context, validators, validatorDescriptors);
            }
        }
    }
}
