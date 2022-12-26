﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private FieldMiddlewareDefinition? _validationFieldMiddlewareDef;
        private FieldMiddlewareDefinition? _validationFieldMiddlewareDefParams;

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
                var needsValidationMiddlewareParams = false;

                foreach (var argDef in fieldDef.Arguments)
                {
                    var argCoord = new FieldCoordinate(objTypeDef.Name, fieldDef.Name, argDef.Name);

                    // 2. the argument should be validated according to options func
                    if (!options.ShouldValidateArgument(objTypeDef, fieldDef, argDef))
                    {
                        continue;
                    }

                    // 3. there's validators for it
                    Dictionary<string, List<ValidatorDescriptor>> validatorDescs;
                    var usingArgs = true;
                    try
                    {
                        var validatorDescsArgs = DetermineValidatorsForArg(validatorRegistry, argDef);
                        if (validatorDescsArgs.Any())
                            validatorDescs = new Dictionary<string, List<ValidatorDescriptor>>() { { "Args", validatorDescsArgs } };
                        else
                        {
                            if (fieldDef.Arguments.Count == 1) // MutationConventions always use one argument
                            {
                                var type = fieldDef.ResolverMember as MethodInfo;
                                var parameters = type?.GetParameters();
                                if (parameters is null)
                                    continue;
                                validatorDescs = DetermineValidatorsForParameters(validatorRegistry, parameters);
                                if (!validatorDescs.Any())
                                    continue;
                                usingArgs = false;
                            }
                            else
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

                    // Cleanup context now we're done with these
                    foreach (var key in argDef.ContextData.Keys)
                    {
                        if (key.StartsWith(WellKnownContextData.Prefix))
                        {
                            argDef.ContextData.Remove(key);
                        }
                    }

                    if (usingArgs)
                    {
                        needsValidationMiddleware = true;
                        argDef.ContextData[WellKnownContextData.ValidatorDescriptors] = validatorDescs.First().Value.AsReadOnly();
                    }
                    else
                    {
                        needsValidationMiddlewareParams = true;
                        argDef.ContextData[WellKnownContextData.ValidatorDescriptorsParams] = validatorDescs;
                    }
                }

                if (needsValidationMiddleware)
                {
                    if (_validationFieldMiddlewareDef is null)
                    {
                        _validationFieldMiddlewareDef = new FieldMiddlewareDefinition(
                            FieldClassMiddlewareFactory.Create<ValidationMiddleware>());
                    }

                    fieldDef.MiddlewareDefinitions.Insert(0, _validationFieldMiddlewareDef);
                }
                else if (needsValidationMiddlewareParams)
                {
                    if (_validationFieldMiddlewareDefParams is null)
                    {
                        _validationFieldMiddlewareDefParams = new FieldMiddlewareDefinition(
                            FieldClassMiddlewareFactory.Create<ValidationMiddlewareParams>());
                    }

                    fieldDef.MiddlewareDefinitions.Add(_validationFieldMiddlewareDefParams);
                }
            }
        }

        private static Dictionary<string, List<ValidatorDescriptor>> DetermineValidatorsForParameters(IValidatorRegistry validatorRegistry, ParameterInfo[] parameters)
        {
            var validators = new Dictionary<string, List<ValidatorDescriptor>>();


            foreach (var parameter in parameters)
            {
                var paramVals = new List<ValidatorDescriptor>();
                // If validation is explicitly disabled, return none so validation middleware won't be added
                if (parameter.CustomAttributes.Any(x => x.AttributeType == typeof(DontValidateAttribute)))
                {
                    continue;
                }


                // Include implicit validator/s first (if allowed)
                if (!parameter.CustomAttributes.Any(x => x.AttributeType == typeof(DontValidateImplicitlyAttribute)))
                {
                    // And if we can figure out the arg's runtime type
                    var argRuntimeType = parameter.ParameterType;
                    if (argRuntimeType is not null)
                    {
                        if (validatorRegistry.Cache.TryGetValue(argRuntimeType, out var implicitValidators) &&
                            implicitValidators is not null)
                        {
                            paramVals.AddRange(implicitValidators);
                        }
                    }
                }

                // Include explicit validator/s (that aren't already added implicitly)
                var explicitValidators = parameter.GetCustomAttributes().Where(x => x.GetType() == typeof(ValidateAttribute)).Cast<ValidateAttribute>().ToList();
                if (explicitValidators.Any())
                {
                    var validatorTypes = explicitValidators.SelectMany(x => x.ValidatorTypes);
                    // TODO: Potentially check and throw if there's a validator being explicitly applied for the wrong runtime type

                    foreach (var validatorType in validatorTypes)
                    {
                        if (paramVals.Any(v => v.ValidatorType == validatorType))
                        {
                            continue;
                        }

                        paramVals.Add(new ValidatorDescriptor(validatorType));
                    }
                }

                if (paramVals.Any())
                {
                    paramVals.TrimExcess();
                    validators[parameter.Name] = paramVals;
                }
            }
            return validators;
        }


        private static List<ValidatorDescriptor> DetermineValidatorsForArg(
            IValidatorRegistry validatorRegistry,
            ArgumentDefinition argDef)
        {
            // If validation is explicitly disabled, return none so validation middleware won't be added
            if (argDef.ContextData.ContainsKey(WellKnownContextData.DontValidate))
            {
                return new List<ValidatorDescriptor>(0);
            }

            var validators = new List<ValidatorDescriptor>();

            // Include implicit validator/s first (if allowed)
            if (!argDef.ContextData.ContainsKey(WellKnownContextData.DontValidateImplicitly))
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
            if (argDef.ContextData.TryGetValue(WellKnownContextData.ExplicitValidatorTypes, out var explicitValidatorTypesRaw) &&
                explicitValidatorTypesRaw is IEnumerable<Type> explicitValidatorTypes)
            {
                // TODO: Potentially check and throw if there's a validator being explicitly applied for the wrong runtime type

                foreach (var validatorType in explicitValidatorTypes)
                {
                    if (validators.Any(v => v.ValidatorType == validatorType))
                    {
                        continue;
                    }

                    validators.Add(new ValidatorDescriptor(validatorType));
                }
            }

            return validators;
        }

        private static Type? TryGetArgRuntimeType(
            ArgumentDefinition argDef)
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
            // It's already a runtime type, .Type(typeof(int))
            if (extType.Kind == ExtendedTypeKind.Runtime)
            {
                return extType.Source;
            }

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
