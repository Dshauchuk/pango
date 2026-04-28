using Microsoft.Extensions.DependencyInjection;

namespace Pango.Persistence.File.IntegrationTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddFileStorage_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddFileStorage();
        Assert.NotEmpty(services);
    }
}
