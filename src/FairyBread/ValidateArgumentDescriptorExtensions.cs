using HotChocolate.Types;

namespace FairyBread
{
    public static class ValidateArgumentDescriptorExtensions
    {
        public static IArgumentDescriptor UseValidation(
            this IArgumentDescriptor descriptor)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(definition => definition.ContextData.Add(ValidateAttribute.ValidateContextDataKey, true));

            return descriptor;
        }
    }
}
