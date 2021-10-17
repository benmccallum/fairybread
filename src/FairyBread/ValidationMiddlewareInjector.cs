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
            "FairyBread could not determine runtime type of field argument to decide whether " +
            "or not to add the validation middleware. If you encounter this exception, set the " +
            $"{nameof(IFairyBreadOptions.ThrowIfArgumentRuntimeTypeCouldNotBeDeterminedWhileOptimizingMiddlewarePlacement)} " +
            $"option to false and report the issue on GitHub. FairyBread will still optimize middleware placement where it can.";

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
                    // 2. the argument should be validated according to options func
                    if (!options.ShouldValidateArgument(objTypeDef, fieldDef, argDef))
                    {
                        continue;
                    }

                    // 3. the arg actually has a validator for it's runtime type
                    if (options.OptimizeMiddlewarePlacement)
                    {
                        var argRuntimeType = TryGetArgRuntimeType(argDef);
                        if (argRuntimeType is null)
                        {
                            if (options.ThrowIfArgumentRuntimeTypeCouldNotBeDeterminedWhileOptimizingMiddlewarePlacement)
                            {
                                throw new Exception(_runtimeTypeCrawlErrorMsg);
                            }
                        }
                        else if (!validatorRegistry.Cache.ContainsKey(argRuntimeType))
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
