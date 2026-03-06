using Butterfly.Client.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Testing;
using Ocelot.Tracing.Butterfly;
using Ocelot.Tracing.OpenTracing;
using OpenTracing;
using Shouldly;
using System.Net;
using TestStack.BDDfy;
using TestStack.BDDfy.Xunit;
using Xunit.Abstractions;

namespace Ocelot.Tracing.OpenTracing.Acceptance;

public sealed class OpenTracingTests : AcceptanceSteps
{
    private readonly ITestOutputHelper _output;
    private static readonly FileHttpHandlerOptions UseTracing = new()
    {
        UseTracing = true,
    };

    public OpenTracingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [BddfyFact]
    public void Should_forward_tracing_information_from_ocelot_and_downstream_services()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/api001/values", "/api/values");
        var route2 = GivenRoute(port2, "/api002/values", "/api/values");
        route1.HttpHandlerOptions = route2.HttpHandlerOptions = UseTracing;
        var configuration = GivenConfiguration(route1, route2);
        var tracingPort = PortFinder.GetRandomPort();
        var fakeTracer = new FakeTracer();
        this.Given(_ => GivenFakeOpenTracing(tracingPort))
            .And(_ => GivenServiceIsRunning(port1, "/api/values", HttpStatusCode.OK, "Hello from Laura", tracingPort, "Service One"))
            .And(_ => GivenServiceIsRunning(port2, "/api/values", HttpStatusCode.OK, "Hello from Tom", tracingPort, "Service Two"))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningUsingOpenTracing(fakeTracer))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api002/values"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .And(_ => ThenTheTracerIsCalled(fakeTracer))
            .BDDfy();
    }

    [BddfyFact]
    public void Should_return_tracing_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/api001/values", "/api/values");
        route.HttpHandlerOptions = UseTracing;
        route.DownstreamHeaderTransform = new Dictionary<string, string>
        {
            {"Trace-Id", "{TraceId}"},
            {"Tom", "Laura"},
        };
        var configuration = GivenConfiguration(route);
        var butterflyPort = PortFinder.GetRandomPort();
        var fakeTracer = new FakeTracer();
        this.Given(x => GivenFakeOpenTracing(butterflyPort))
            .And(x => GivenServiceIsRunning(port, "/api/values", HttpStatusCode.OK, "Hello from Laura", butterflyPort, "Service One"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningUsingOpenTracing(fakeTracer))
            .When(x => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheResponseHeaderExists("Trace-Id"))
            .And(x => ThenTheResponseHeaderIs("Tom", "Laura"))
            .BDDfy();
    }

    private void GivenOcelotIsRunningUsingOpenTracing(ITracer fakeTracer)
    {
        GivenOcelotIsRunning(s =>
        {
            s.AddOcelot().AddOpenTracing();
            s.AddSingleton(fakeTracer); // WTF ?
        });
    }

    private void ThenTheTracerIsCalled(FakeTracer fakeTracer)
    {
        var commandOnAllStateMachines = Wait.For(10_000).Until(() => fakeTracer.BuildSpanCalled >= 2);
        _output.WriteLine($"fakeTracer.BuildSpanCalled is {fakeTracer.BuildSpanCalled}");
        commandOnAllStateMachines.ShouldBeTrue();
    }

    private void GivenServiceIsRunning(int port, string basePath, HttpStatusCode statusCode, string responseBody, int butterflyPort, string serviceName)
    {
        void WithButterfly(IServiceCollection services) => services
            .AddButterfly(option =>
            {
                option.CollectorUrl = DownstreamUrl(butterflyPort);
                option.Service = serviceName;
                option.IgnoredRoutesRegexPatterns = Array.Empty<string>();
            });
        handler.GivenThereIsAServiceRunningOn(DownstreamUrl(port), basePath, WithButterfly, context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }

    private void GivenFakeOpenTracing(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, context => context.Response.WriteAsync("OK..."));
    }
}
