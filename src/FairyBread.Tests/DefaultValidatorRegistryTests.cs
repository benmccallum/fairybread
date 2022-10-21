namespace FairyBread.Tests;

public class DefaultValidatorRegistryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Respect_ThrowIfNoValidatorsFound_Option(bool throwIfNoValidatorsFound)
    {
        // Arrange
        var services = new ServiceCollection();

        var options = new DefaultFairyBreadOptions
        {
            ThrowIfNoValidatorsFound = throwIfNoValidatorsFound
        };

        Func<DefaultValidatorRegistry> func = () => new DefaultValidatorRegistry(services, options);

        // Act
        if (throwIfNoValidatorsFound)
        {
            var ex = Should.Throw<Exception>(func);
            ex.Message.ShouldContain("No validators were found by FairyBread.");
        }
        else
        {
            Should.NotThrow(func);
        }
    }
}
