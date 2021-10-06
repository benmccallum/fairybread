using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace FairyBread
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValidateAttribute : ArgumentDescriptorAttribute
    {
        public Type[] ValidatorTypes;

        /// <summary>
        /// If true (default), implicit validators will still be
        /// run for the targeted argument.
        /// </summary>
        public bool RunImplicitValidators { get; set; } = true;

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

            descriptor.Extend().OnBeforeCompletion((completionContext, argDef) =>
            {
                if (attr.ValidatorTypes.Length != attr.ValidatorTypes.Distinct().Count())
                {
                    throw new Exception("Duplicate validators added. Likely a mistake.");
                    // TODO: Report this better using below?
                    //completionContext.ReportError()
                }

                // TODO: Validate that validator is of correct type

                argDef.ContextData[WellKnownContextData.ValidateAttribute] = attr;
            });
        }
    }
}
