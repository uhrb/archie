using System;
using System.IO;
using archie.io;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace units;

public class VirtualLogger<T> : ILogger<T>
{
    private readonly TextWriter _writer;

    public VirtualLogger(TextWriter writer)
    {
        _writer = writer;
    }

    public VirtualLogger(IStreams streams) 
    {
        _writer = streams.Stdout;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        _writer.WriteLine($"VirtualLogger {typeof(T).Name} {state}");
        _writer.Flush();
        return new DummyScope();
    }

    private class DummyScope : IDisposable
    {
        public void Dispose()
        {
        }
    }


    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _writer
            .WriteLine($"VirtualLogger {typeof(T).Name} {logLevel} {eventId} {state} {exception} {formatter(state, exception)}");
        _writer.Flush();
    }

}