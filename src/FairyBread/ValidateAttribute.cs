using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace FairyBread
{
    /// <summary>
    /// Used to annotate that a resolver argument should always be validated.
    /// </summary>
    public class ValidateAttribute : ArgumentDescriptorAttribute
    {
        public override void OnConfigure(IDescriptorContext context, IArgumentDescriptor descriptor, ParameterInfo parameter)
        {
            descriptor.UseValidation();
        }
    }
}
