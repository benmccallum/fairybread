using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace FairyBread
{

    /// <summary>
    /// Instructs FairyBread to run the given validators for the annotated argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValidateAttribute : ArgumentDescriptorAttribute
    {
        public Type[] ValidatorTypes;

        public ValidateAttribute(params Type[] validatorTypes)
        {
            ValidatorTypes = validatorTypes;
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            if (parameter.GetCustomAttribute<ValidateAttribute>() is not ValidateAttribute attr)
            {
                return;
            }

            descriptor.Extend().OnBeforeNaming((completionContext, argDef) =>
            {
                if (attr.ValidatorTypes.Length != attr.ValidatorTypes.Distinct().Count())
                {
                    throw new Exception("Duplicate validators added. Likely a mistake.");
                    // TODO: Report this better using below?
                    //completionContext.ReportError()
                }

                // TODO: Validate that validator is of correct type

                argDef.ContextData[WellKnownContextData.ExplicitValidatorTypes] = attr.ValidatorTypes;
            });
        }
    }
}
