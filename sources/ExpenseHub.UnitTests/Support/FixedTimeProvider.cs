using System;

namespace ExpenseHub.UnitTests.Support;

/// <summary>
/// Relógio controlado pelos testes.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    /// <summary>Instante padrão usado nos testes.</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    private DateTimeOffset _now = DefaultNow;

    /// <summary>Data atual (UTC) do relógio.</summary>
    public DateOnly Today => DateOnly.FromDateTime(_now.UtcDateTime);

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>
    /// Avança o relógio.
    /// </summary>
    /// <param name="interval">Intervalo a avançar.</param>
    public void Advance(TimeSpan interval) => _now = _now.Add(interval);
}
