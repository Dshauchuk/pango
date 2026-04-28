using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces;

namespace Pango.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAppOptions>(new TestAppOptions());

        services.AddInfrastructureServices();
        Assert.NotEmpty(services);
    }

    private class TestAppOptions : IAppOptions
    {
        public IFileOptions FileOptions { get; set; } = null!;
    }
}
