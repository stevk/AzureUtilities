using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockLogger : ILogger
    {
        private readonly object _lock = new object();
        public IList<MockLoggerMessage> Messages { get; } = new List<MockLoggerMessage>();
        public string CategoryName { get; }
        public MockLogger(string categoryName)
        {
            CategoryName = categoryName;
        }

        public MockLogger()
        {

        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_lock)
            {
                this.Messages.Add(new MockLoggerMessage
                {
                    LogLevel = logLevel,
                    EventId = eventId,
                    StateType = typeof(TState),
                    State = state,
                    Exception = exception,
                    Message = formatter?.Invoke(state, exception)
                });
            }
        }
    }

    public sealed class MockLogger<T> : MockLogger, ILogger<T>
    {
        public MockLogger()
            : base(typeof(T).Name)
        {
        }

        public IList<MockLoggerMessage> LoggedMessages
        {
            get
            {
                return Messages;
            }
        }
    }
}
