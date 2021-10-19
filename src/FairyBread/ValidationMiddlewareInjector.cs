﻿using System;
using System.Collections.Generic;
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

                    // 3. the arg actually has a validator for its runtime type
                    // (note: if we can't figure out the runtime type, doesn't add)
                    if (options.OptimizeMiddlewarePlacement)
                    {
                        var argRuntimeType = TryGetArgRuntimeType(objTypeDef, fieldDef, argDef);
                        if (argRuntimeType is null ||
                            !validatorRegistry.Cache.ContainsKey(argRuntimeType))
                        {
                            continue;
                        }
                    }

                    needsValidationMiddleware = true;
                    argDef.ContextData[WellKnownContextData.ShouldValidate] = true;
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

        private static Type? TryGetArgRuntimeType(
            ObjectTypeDefinition objTypeDef,
            ObjectFieldDefinition fieldDef,
            ArgumentDefinition argDef)
        {
            if (argDef.Parameter?.ParameterType is { } argRuntimeType)
            {
                return argRuntimeType;
            }

            //if (argDef.Type is SyntaxTypeReference)
            //{
            //    return null;
            //}

            if (argDef.Type is ExtendedTypeReference extTypeRef)
            {
                try
                {
                    return TryGetRuntimeType(extTypeRef.Type);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Problem getting runtime type for argument '{argDef.Name}' " +
                        $"in field '{fieldDef.Name}' on object type '{objTypeDef.Name}'. " +
                        $"Disable {nameof(IFairyBreadOptions.OptimizeMiddlewarePlacement)} in " +
                        $"options and report the issue on GitHub.",
                        ex);
                }
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

        private static IReadOnlyList<ValidatorDescriptor> DetermineValidatorsForArg(
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
