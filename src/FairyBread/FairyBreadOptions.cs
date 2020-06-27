using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FairyBread
{
    public interface IFairyBreadOptions
    {
        IEnumerable<Assembly> AssembliesToScanForValidators { get; set; }

        bool ThrowIfNoValidatorsFound { get; set; }
    }

    public class FairyBreadOptions : IFairyBreadOptions
    {
        public virtual IEnumerable<Assembly> AssembliesToScanForValidators { get; set; } = Enumerable.Empty<Assembly>();

        public virtual bool ThrowIfNoValidatorsFound { get; set; } = true;
    }
}
