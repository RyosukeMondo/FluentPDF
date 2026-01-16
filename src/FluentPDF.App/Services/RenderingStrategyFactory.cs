using FluentPDF.App.Interfaces;

namespace FluentPDF.App.Services;

/// <summary>
/// Factory for creating and ordering rendering strategies by priority.
/// Strategies with lower priority values are tried first.
/// </summary>
public sealed class RenderingStrategyFactory
{
    private readonly IEnumerable<IRenderingStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingStrategyFactory"/> class.
    /// </summary>
    /// <param name="strategies">All registered rendering strategies injected from DI container.</param>
    public RenderingStrategyFactory(IEnumerable<IRenderingStrategy> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    /// <summary>
    /// Gets all rendering strategies ordered by priority (lowest priority value first).
    /// Primary strategies (Priority=0) are returned first, fallback strategies (Priority=10+) come later.
    /// </summary>
    /// <returns>Enumerable of strategies ordered by priority ascending.</returns>
    public IEnumerable<IRenderingStrategy> GetStrategies()
    {
        return _strategies
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.StrategyName); // Secondary sort by name for deterministic ordering
    }
}
