using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace FairyBread
{
    public static class ValidateInputObjectTypeDescriptorExtensions
    {
        public static IInputObjectTypeDescriptor UseValidation(
            this IInputObjectTypeDescriptor descriptor)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(inputTypeDefinition => Annotate(inputTypeDefinition));

            return descriptor;
        }

        public static IInputObjectTypeDescriptor<T> UseValidation<T>(
            this IInputObjectTypeDescriptor<T> descriptor)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(inputTypeDefinition => Annotate(inputTypeDefinition));

            return descriptor;
        }

        private static void Annotate(InputObjectTypeDefinition inputTypeDefinition)
        {
            inputTypeDefinition.ContextData.Add(ValidateAttribute.ValidateContextDataKey, true);
        }
    }
}
