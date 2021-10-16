using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
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
                var needsValidationMiddleware = false;

                foreach (var argDef in fieldDef.Arguments)
                {
                    if (options.ShouldValidateArgument(objTypeDef, fieldDef, argDef))
                    {
                        // If we know what the arg's runtime type is and we don't have a validator for
                        // it, don't add the validation field middleware unnecessarily
                        if (TryGetArgRuntimeType(argDef) is { } argRuntimeType &&
                            !validatorRegistry.Cache.ContainsKey(argRuntimeType))
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
                // Could be a list
                if (extTypeRef.Type.IsArray)
                {

                }

                if (extTypeRef.Type.IsList)
                {

                }

                if (extTypeRef.Type.IsSchemaType)
                {
                    if (extTypeRef.Type.IsNullable)
                    {
                        //throw new NotImplementedException();
                    }
                }

                if (typeof(InputObjectType).IsAssignableFrom(extTypeRef.Type.Type))
                {
                    var currBaseType = extTypeRef.Type.Type.BaseType;
                    while (currBaseType is not null &&
                        currBaseType.BaseType != typeof(InputObjectType))
                    {
                        currBaseType = currBaseType.BaseType;
                    }

                    return currBaseType!.GenericTypeArguments[0];
                }

                if (typeof(ScalarType).IsAssignableFrom(extTypeRef.Type.Type))
                {
                    var currBaseType = extTypeRef.Type.Type.BaseType;
                    while (currBaseType is not null &&
                        currBaseType.BaseType != typeof(ScalarType))
                    {
                        currBaseType = currBaseType.BaseType;
                    }

                    if (currBaseType is null)
                    {
                        return null;
                    }

                    argRuntimeType = currBaseType.GenericTypeArguments[0];
                    if (argRuntimeType.IsValueType && extTypeRef.Type.IsNullable)
                    {
                        return typeof(Nullable<>).MakeGenericType(argRuntimeType);
                    }

                    return argRuntimeType;
                }
            }

            // Hmmf...
            return null;
        }
    }

    internal static class WellKnownContextData
    {
        public const string ShouldValidate = "FairyBread.ShouldValidate";
    }
}
