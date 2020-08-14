using HotChocolate.Types;

namespace FairyBread
{
    public static class ValidateArgumentDescriptorExtensions
    {
        internal const string ValidateContextDataKey = "FairyBread_Validate";
         
        public static IArgumentDescriptor UseValidation(
            this IArgumentDescriptor descriptor)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(argDef => argDef.ContextData.Add(ValidateContextDataKey, true));

            return descriptor;
        }
    }
}
