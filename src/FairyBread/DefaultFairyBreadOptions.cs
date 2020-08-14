using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace FairyBread
{
    public class DefaultFairyBreadOptions : IFairyBreadOptions
    {
        public virtual IEnumerable<Assembly> AssembliesToScanForValidators { get; set; } = Enumerable.Empty<Assembly>();

        public virtual bool ThrowIfNoValidatorsFound { get; set; } = true;

        public virtual Func<IMiddlewareContext, Argument, bool> ShouldValidate { get; set; } = ShouldValidateImplementation;

        public static bool ShouldValidateImplementation(IMiddlewareContext context, Argument argument)
        {
            // If a mutation operation and this is an input object
            if (context.Operation.Operation == OperationType.Mutation &&
                argument.Type.InnerType() is InputObjectType)
            {
                return true;
            }

            if (ShouldValidateBasedOnValidateDescriptorImplementation(context, argument))
            {
                return true;
            }

            return false;
        }

        public static bool ShouldValidateBasedOnValidateDescriptorImplementation(IMiddlewareContext context, Argument argument)
        {
            // If argument itself was annotated
            if (IsValidateDescriptorApplied(argument.ContextData))
            {
                return true;
            }

            // If argument's input type was annotated
            if (argument.Type.InnerType() is InputObjectType inputType &&
                IsValidateDescriptorApplied(inputType))
            {
                return true;
            }

            // If argument's clr type was annotated
            if (ClrTypesMarkedWithValidate.Cache.GetOrAdd(
                    argument.ClrType,
                    clrType => clrType.GetCustomAttribute<ValidateAttribute>(inherit: true) != null))
            {
                return true;
            }

            return false;
        }

        private static bool IsValidateDescriptorApplied(IHasContextData thing)
        {
            return IsValidateDescriptorApplied(thing.ContextData);
        }

        private static bool IsValidateDescriptorApplied(IReadOnlyDictionary<string, object?> contextData)
        {
            return contextData.TryGetValue(ValidateAttribute.ValidateContextDataKey, out var isValidateDescriptorAppliedRaw) &&
               isValidateDescriptorAppliedRaw is bool isValidateDescriptorApplied &&
               isValidateDescriptorApplied;
        }
    }
}
