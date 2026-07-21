using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using OpenTracing;
using OpenTracing.Util;

namespace Ocelot.Tracing.OpenTracing.UnitTests;

public class OcelotBuilderExtensionsTests
{
    [Fact]
    public void AddOpenTracing_IOcelotBuilder()
    {
        // Arrange
        ConfigurationRoot configRoot = new([]);
        IServiceCollection services = new ServiceCollection();
        IOcelotBuilder builder = services.AddOcelot(configRoot);

        // Act
        var actual = builder.AddOpenTracing();

        // Assert
        Assert.Same(builder, actual);

        ServiceDescriptor sd = services.Single(x => x.ServiceType == typeof(IOcelotTracer));
        Assert.Equal(ServiceLifetime.Singleton, sd.Lifetime);

        sd = services.Single(x => x.ServiceType == typeof(ITracer));
        Assert.Equal(ServiceLifetime.Singleton, sd.Lifetime);

        var provider = services.BuildServiceProvider(true);
        var actualTracer = provider.GetService<IOcelotTracer>();
        Assert.NotNull(actualTracer);
        Assert.IsType<OpenTracingTracer>(actualTracer);

        var nativeTracer = provider.GetService<ITracer>();
        Assert.NotNull(nativeTracer);
        Assert.IsType<GlobalTracer>(nativeTracer);
    }
}
