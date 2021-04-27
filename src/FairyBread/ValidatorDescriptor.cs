using System;

namespace FairyBread
{
    /// <summary>
    /// Description of a validator configuration to be stored in the validator descriptor cache.
    /// </summary>
    public class ValidatorDescriptor
    {
        /// <summary>
        /// Type of the validator.
        /// </summary>
        public Type ValidatorType { get; }

        /// <summary>
        /// Does the validator inherit <see cref="IRequiresOwnScopeValidator"/>? 
        /// If so, this means it should be resolved from the service provider in it's own scope.
        /// </summary>
        public bool RequiresOwnScope { get; }

        /// <summary>
        /// Instantiates a new <see cref="ValidatorDescriptor"/>.
        /// </summary>
        /// <param name="validatorType">The validator.</param>
        public ValidatorDescriptor(Type validatorType, bool requiresOwnScope)
        {
            ValidatorType = validatorType;
            RequiresOwnScope = requiresOwnScope;
        }
    }
}
