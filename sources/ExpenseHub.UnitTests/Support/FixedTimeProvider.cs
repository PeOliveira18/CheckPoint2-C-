using System;

namespace ExpenseHub.UnitTests.Support;

internal sealed class FixedTimeProvider : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    private DateTimeOffset _now = DefaultNow;

    public DateOnly Today => DateOnly.FromDateTime(_now.UtcDateTime);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan interval) => _now = _now.Add(interval);
}
