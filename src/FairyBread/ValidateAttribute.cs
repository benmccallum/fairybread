using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace FairyBread
{
    /// <summary>
    /// Used to annotate arguments, input types or CLR types that should always be validated.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Parameter,
        Inherited = true,
        AllowMultiple = false)]
    public class ValidateAttribute : DescriptorAttribute
    {
        internal const string ValidateContextDataKey = "FairyBread_Validate";

        protected override void TryConfigure(IDescriptorContext context, IDescriptor descriptor, ICustomAttributeProvider element)
        {
            switch (descriptor)
            {
                case IInputObjectTypeDescriptor inputObjectTypeDescriptor:
                    inputObjectTypeDescriptor.UseValidation();
                    break;
                case IArgumentDescriptor argumentDescriptor:
                    argumentDescriptor.UseValidation();
                    break;
            }
        }
    }
}
