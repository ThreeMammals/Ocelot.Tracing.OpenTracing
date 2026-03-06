using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace Ocelot.Tracing.OpenTracing.Acceptance;

internal class FakeTracer : ITracer
{
    public IScopeManager ScopeManager => throw new NotImplementedException();
    public ISpan ActiveSpan => throw new NotImplementedException();

    public ISpanBuilder BuildSpan(string operationName)
    {
        BuildSpanCalled++;
        return new FakeSpanBuilder();
    }

    public int BuildSpanCalled { get; set; }

    public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
    {
        ExtractCalled++;
        return null;
    }

    public int ExtractCalled { get; set; }

    public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
    {
        InjectCalled++;
    }

    public int InjectCalled { get; set; }
}

internal class FakeSpanBuilder : ISpanBuilder
{
    public ISpanBuilder AddReference(string referenceType, ISpanContext referencedContext) => throw new NotImplementedException();
    public ISpanBuilder AsChildOf(ISpanContext parent) => throw new NotImplementedException();
    public ISpanBuilder AsChildOf(ISpan parent) => throw new NotImplementedException();
    public ISpanBuilder IgnoreActiveSpan() => throw new NotImplementedException();
    public ISpan Start() => throw new NotImplementedException();
    public IScope StartActive() => throw new NotImplementedException();
    public IScope StartActive(bool finishSpanOnDispose) => new FakeScope(finishSpanOnDispose);
    public ISpanBuilder WithStartTimestamp(DateTimeOffset timestamp) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, string value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, bool value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, int value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, double value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(BooleanTag tag, bool value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(IntOrStringTag tag, string value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(IntTag tag, int value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(StringTag tag, string value) => throw new NotImplementedException();
}

internal class FakeScope : IScope
{
    private readonly bool finishSpanOnDispose;

    public FakeScope(bool finishSpanOnDispose)
    {
        this.finishSpanOnDispose = finishSpanOnDispose;
    }

    public ISpan Span { get; } = new FakeSpan();

    public void Dispose()
    {
        if (finishSpanOnDispose)
        {
            Span.Finish();
        }
    }
}

internal class FakeSpan : ISpan
{
    public ISpanContext Context => new FakeSpanContext();
    public void Finish() { }
    public void Finish(DateTimeOffset finishTimestamp) => throw new NotImplementedException();
    public string GetBaggageItem(string key) => throw new NotImplementedException();
    public ISpan Log(IEnumerable<KeyValuePair<string, object>> fields) => this;
    public ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields) => throw new NotImplementedException();
    public ISpan Log(string @event) => throw new NotImplementedException();
    public ISpan Log(DateTimeOffset timestamp, string @event) => throw new NotImplementedException();
    public ISpan SetBaggageItem(string key, string value) => throw new NotImplementedException();
    public ISpan SetOperationName(string operationName) => throw new NotImplementedException();
    public ISpan SetTag(string key, string value) => this;
    public ISpan SetTag(string key, bool value) => this;
    public ISpan SetTag(string key, int value) => this;
    public ISpan SetTag(string key, double value) => this;
    public ISpan SetTag(BooleanTag tag, bool value) => this;
    public ISpan SetTag(IntOrStringTag tag, string value) => this;
    public ISpan SetTag(IntTag tag, int value) => this;
    public ISpan SetTag(StringTag tag, string value) => this;
}

internal class FakeSpanContext : ISpanContext
{
    public static string FakeTraceId = "FakeTraceId";
    public static string FakeSpanId = "FakeSpanId";
    public string TraceId => FakeTraceId;
    public string SpanId => FakeSpanId;
    public IEnumerable<KeyValuePair<string, string>> GetBaggageItems() => throw new NotImplementedException();
}
