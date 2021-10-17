using System;
using System.Collections.Generic;
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

        private static readonly string _runtimeTypeCrawlErrorMsg =
            "FairyBread could not determine runtime type of field argument to decide whether or not to " +
            "add the validation middleware. If you encounter this exception, you can turn " +
            $"this off with the {nameof(IFairyBreadOptions.ThrowIfArgumentRuntimeTypeCouldNotBeDeterminedDuringPruning)}" +
            $" option and  FairyBread will prune where it can still and the problem argument will always get the validation " +
            $"middleware. Or, less ideal, you can turn off pruning entirely with " +
            $"{nameof(IFairyBreadOptions.PruneMiddlewarePlacement)}. " +
            "In either case, please report the issue in GitHub.";

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
                var needsValidationMiddleware = false;

                foreach (var argDef in fieldDef.Arguments)
                {
                    if (options.ShouldValidateArgument(objTypeDef, fieldDef, argDef))
                    {
                        // If we know what the arg's runtime type is and we don't have a validator for
                        // it, don't add the validation field middleware unnecessarily
                        var argRuntimeType = TryGetArgRuntimeType(argDef);
                        if (argRuntimeType is null)
                        {
                            if (options.ThrowIfArgumentRuntimeTypeCouldNotBeDeterminedDuringPruning)
                            {
                                throw new ApplicationException(_runtimeTypeCrawlErrorMsg);
                            }
                        }
                        else if (!validatorRegistry.Cache.ContainsKey(argRuntimeType))
                        {
                            continue;
                        }

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

                    fieldDef.MiddlewareComponents.Insert(0, _validationFieldMiddleware);
                }
            }
        }

        private static Type? TryGetArgRuntimeType(ArgumentDefinition argDef)
        {
            if (argDef.Parameter?.ParameterType is { } argRuntimeType)
            {
                return argRuntimeType;
            }

            if (argDef.Type is ExtendedTypeReference extTypeRef)
            {
                return GetRuntimeType(extTypeRef.Type);
            }

            return null;
        }

        private static Type? GetRuntimeType(IExtendedType extType)
        {
            // Array (though not sure what produces this scenario as seems to always be list)
            if (extType.IsArray)
            {
                if (extType.ElementType is null)
                {
                    return null;
                }

                var elementRuntimeType = GetRuntimeType(extType.ElementType);
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

                var elementRuntimeType = GetRuntimeType(extType.ElementType);
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
                    currBaseType.BaseType != typeof(InputObjectType))
                {
                    currBaseType = currBaseType.BaseType;
                }

                return currBaseType!.GenericTypeArguments[0];
            }

            // Singular scalar
            if (typeof(ScalarType).IsAssignableFrom(extType))
            {
                var currBaseType = extType.Type.BaseType;
                while (currBaseType is not null &&
                    currBaseType.BaseType != typeof(ScalarType))
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

    internal static class WellKnownContextData
    {
        public const string ShouldValidate = "FairyBread.ShouldValidate";
    }
}
