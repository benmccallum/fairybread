using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
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
    }

    internal static class WellKnownContextData
    {
        public const string ShouldValidate = "FairyBread.ShouldValidate";
    }
}
