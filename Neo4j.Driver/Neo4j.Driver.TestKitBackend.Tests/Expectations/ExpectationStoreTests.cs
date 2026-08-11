// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Expectations;

public class ExpectationStoreTests
{
    private readonly RecordingLogger _logger = new();
    private readonly ExpectationStore _store;

    public ExpectationStoreTests()
    {
        _store = new ExpectationStore(_logger);
    }

    [Fact]
    public async Task Expect_completes_with_the_value_a_matching_Fulfil_provides()
    {
        var pending = _store.Expect<string>("key-1");

        _store.Fulfil("key-1", "value-1");

        var value = await WithTimeoutAsync(pending);
        value.Should().Be("value-1");
    }

    [Fact]
    public async Task Expect_throws_naming_the_key_when_Fulfil_provides_a_value_of_the_wrong_type()
    {
        var pending = _store.Expect<string>("key-1");

        _store.Fulfil("key-1", 42);

        Func<Task> act = () => WithTimeoutAsync(pending);
        await act.Should().ThrowAsync<TestKitProtocolException>().WithMessage("*key-1*");
    }

    [Fact]
    public async Task Fail_makes_the_pending_expectation_throw_the_given_exception()
    {
        var exception = new InvalidOperationException("boom");
        var pending = _store.Expect<string>("key-1");

        _store.Fail("key-1", exception);

        Func<Task> act = () => WithTimeoutAsync(pending);
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(exception);
    }

    [Fact]
    public void Fulfil_on_an_unknown_key_throws_naming_the_key()
    {
        var act = () => _store.Fulfil("missing-key", "value");

        act.Should().Throw<TestKitProtocolException>().WithMessage("*missing-key*");
    }

    [Fact]
    public void Fail_on_an_unknown_key_throws_naming_the_key()
    {
        var act = () => _store.Fail("missing-key", new InvalidOperationException());

        act.Should().Throw<TestKitProtocolException>().WithMessage("*missing-key*");
    }

    [Fact]
    public void Expect_on_an_already_pending_key_throws_naming_the_key()
    {
        _store.Expect<string>("key-1");

        var act = () => _store.Expect<string>("key-1");

        act.Should().Throw<TestKitProtocolException>().WithMessage("*key-1*");
    }

    [Fact]
    public void Fulfilling_is_one_shot_a_second_Fulfil_on_the_same_key_is_an_unknown_key()
    {
        _store.Expect<string>("key-1");
        _store.Fulfil("key-1", "value-1");

        var act = () => _store.Fulfil("key-1", "value-2");

        act.Should().Throw<TestKitProtocolException>().WithMessage("*key-1*");
    }

    [Fact]
    public async Task Expect_after_CancelAll_returns_an_already_cancelled_task()
    {
        _store.CancelAll();

        var pending = _store.Expect<string>("key-1");

        Func<Task> act = () => WithTimeoutAsync(pending);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Disposing_cancels_all_outstanding_expectations()
    {
        var pending = _store.Expect<string>("key-1");

        _store.Dispose();

        Func<Task> act = () => WithTimeoutAsync(pending);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Disposing_with_an_outstanding_expectation_logs_a_warning_naming_the_key()
    {
        _store.Expect<string>("key-1");

        _store.Dispose();

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("key-1");
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(task);
        return await task;
    }

    private class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}
