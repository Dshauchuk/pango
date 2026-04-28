using Microsoft.Extensions.DependencyInjection;

namespace Pango.Application.Tests.Common;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationServices_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        Assert.NotEmpty(services);

        Assert.Contains(services, d =>
            d.ServiceType.Name.Contains("IMediator") ||
            d.ServiceType.Name.Contains("ISender"));
    }
}
