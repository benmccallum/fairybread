using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
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
            var validatorRegistry = completionContext.Services
                .GetRequiredService<IValidatorRegistry>();

            foreach (var fieldDef in objTypeDef.Fields)
            {
                // Don't add validation middleware unless:
                // 1. we have args
                var needsValidationMiddleware = false;

                foreach (var argDef in fieldDef.Arguments)
                {
                    var argCoord = new FieldCoordinate(objTypeDef.Name, fieldDef.Name, argDef.Name);

                    // 2. the argument should be validated according to options func
                    if (!options.ShouldValidateArgument(objTypeDef, fieldDef, argDef))
                    {
                        continue;
                    }

                    // 3. there's validators for it
                    List<ValidatorDescriptor> validatorDescs;
                    try
                    {
                        validatorDescs = DetermineValidatorsForArg(validatorRegistry, argDef);
                        if (validatorDescs.Count < 1)
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"Problem getting runtime type for argument '{argDef.Name}' " +
                            $"in field '{fieldDef.Name}' on object type '{objTypeDef.Name}'.",
                            ex);
                    }

                    // TODO: Set validatordescriptors on the context data for easier retrieval later
                    // in the validator provider, which would now just need to loop over those
                    validatorDescs.TrimExcess();
                    //validators.ToReadOnlyList();
                    needsValidationMiddleware = true;
                    argDef.ContextData[WellKnownContextData.ValidatorDescriptors] = validatorDescs;
                }

                if (needsValidationMiddleware)
                {
                    if (_validationFieldMiddleware is null)
                    {
                        _validationFieldMiddleware = FieldClassMiddlewareFactory
                            .Create<ValidationMiddleware>();
                    }

                    fieldDef.MiddlewareComponents.Insert(0, _validationFieldMiddleware);
                }
            }
        }

        private static List<ValidatorDescriptor> DetermineValidatorsForArg(
            IValidatorRegistry validatorRegistry,
            ArgumentDefinition argDef)
        {
            var validators = new List<ValidatorDescriptor>();

            // Grab explicit attribute/s
            ValidateAttribute? validateAttr = null;
            if (argDef.ContextData.TryGetValue(WellKnownContextData.ValidateAttribute, out var rawAttr) &&
                rawAttr is ValidateAttribute validateAttribute)
            {
                validateAttr = validateAttribute;

                // Remove now we're done with marker
                argDef.ContextData.Remove(WellKnownContextData.ValidateAttribute);
            }

            // Include implicit validator/s first (if allowed)            
            if (validateAttr is null || validateAttr.RunImplicitValidators)
            {
                // And if we can figure out the arg's runtime type
                var argRuntimeType = TryGetArgRuntimeType(argDef);
                if (argRuntimeType is not null)
                {
                    if (validatorRegistry.Cache.TryGetValue(argRuntimeType, out var implicitValidators) &&
                        implicitValidators is not null)
                    {
                        validators.AddRange(implicitValidators);
                    }
                }
            }

            // Include explicit validator/s (that aren't already added implicitly)
            if (validateAttr is not null)
            {
                foreach (var validatorType in validateAttr.ValidatorTypes)
                {
                    if (validators.Any(v => v.ValidatorType == validatorType))
                    {
                        continue;
                    }

                    var requiresOwnScope = validatorRegistry.ShouldBeResolvedInOwnScope(validatorType);
                    validators.Add(new ValidatorDescriptor(validatorType, requiresOwnScope));
                }
            }

            return validators;
        }

        private static Type? TryGetArgRuntimeType(ArgumentDefinition argDef)
        {
            if (argDef.Parameter?.ParameterType is { } argRuntimeType)
            {
                return argRuntimeType;
            }

            if (argDef.Type is ExtendedTypeReference extTypeRef)
            {
                return TryGetRuntimeType(extTypeRef.Type);
            }

            return null;
        }

        private static Type? TryGetRuntimeType(IExtendedType extType)
        {
            // Array (though not sure what produces this scenario as seems to always be list)
            if (extType.IsArray)
            {
                if (extType.ElementType is null)
                {
                    return null;
                }

                var elementRuntimeType = TryGetRuntimeType(extType.ElementType);
                if (elementRuntimeType is null)
                {
                    return null;
                }

                return Array.CreateInstance(elementRuntimeType, 0).GetType();
            }

            // List
            if (extType.IsList)
            {
                if (extType.ElementType is null)
                {
                    return null;
                }

                var elementRuntimeType = TryGetRuntimeType(extType.ElementType);
                if (elementRuntimeType is null)
                {
                    return null;
                }

                return typeof(List<>).MakeGenericType(elementRuntimeType);
            }

            // Input object
            if (typeof(InputObjectType).IsAssignableFrom(extType))
            {
                var currBaseType = extType.Type.BaseType;
                while (currBaseType is not null &&
                    (!currBaseType.IsGenericType ||
                    currBaseType.GetGenericTypeDefinition() != typeof(InputObjectType<>)))
                {
                    currBaseType = currBaseType.BaseType;
                }

                if (currBaseType is null)
                {
                    return null;
                }

                return currBaseType!.GenericTypeArguments[0];
            }

            // Singular scalar
            if (typeof(ScalarType).IsAssignableFrom(extType))
            {
                var currBaseType = extType.Type.BaseType;
                while (currBaseType is not null &&
                    (!currBaseType.IsGenericType ||
                    currBaseType.GetGenericTypeDefinition() != typeof(ScalarType<>)))
                {
                    currBaseType = currBaseType.BaseType;
                }

                if (currBaseType is null)
                {
                    return null;
                }

                var argRuntimeType = currBaseType.GenericTypeArguments[0];
                if (argRuntimeType.IsValueType && extType.IsNullable)
                {
                    return typeof(Nullable<>).MakeGenericType(argRuntimeType);
                }

                return argRuntimeType;
            }

            return null;
        }
    }
}
