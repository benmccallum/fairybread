using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace FairyBread.Tests
{
    [UsesVerify]
    public class ValidateAttributeTests
    {
        [Fact]
        public async Task Defaults()
        {
            var defaultAttr = new ValidateAttribute(
                typeof(ValidateAttributeTests),
                typeof(ValidateAttributeTests));

            var overrideAttr = new ValidateAttribute(
                typeof(ValidateAttributeTests))
            {
                RunImplicitValidators = false
            };

            await Verifier.Verify(new
            {
                defaultAttr,
                overrideAttr
            });
        }
    }
}
