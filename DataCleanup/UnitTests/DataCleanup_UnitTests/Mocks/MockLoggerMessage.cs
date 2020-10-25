using Microsoft.Extensions.Logging;
using System;

namespace DataCleanup_UnitTests.Mocks
{
    public sealed class MockLoggerMessage
    {
        public LogLevel LogLevel { get; internal set; }
        public EventId EventId { get; internal set; }
        public Exception Exception { get; internal set; }
        public Type StateType { get; internal set; }
        public object State { get; internal set; }
        public string Message { get; internal set; }
    }
}
