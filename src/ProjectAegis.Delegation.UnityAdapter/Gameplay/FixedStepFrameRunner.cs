namespace ProjectAegis.Delegation.UnityAdapter.Gameplay;

/// <summary>
/// Converts elapsed presentation time into fixed simulation steps. Acceleration belongs
/// here exactly once; individual simulation steps must not accelerate themselves.
/// </summary>
public sealed class FixedStepFrameRunner
{
    private readonly double _fixedDeltaSeconds;
    private readonly int _maxStepsPerFrame;
    private double _accumulatedSeconds;

    /// <summary>Creates a scheduler with a positive fixed delta and bounded per-frame work.</summary>
    public FixedStepFrameRunner(double fixedDeltaSeconds = 1.0 / 60.0, int maxStepsPerFrame = 512)
    {
        if (double.IsNaN(fixedDeltaSeconds) || double.IsInfinity(fixedDeltaSeconds) || fixedDeltaSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedDeltaSeconds));
        }

        if (maxStepsPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStepsPerFrame));
        }

        _fixedDeltaSeconds = fixedDeltaSeconds;
        _maxStepsPerFrame = maxStepsPerFrame;
    }

    /// <summary>
    /// Advances at most the configured frame budget, retaining running backlog. Checks
    /// <paramref name="canStep"/> between steps so a new pause interrupts the same frame.
    /// Paused time and any backlog at interruption are discarded rather than replayed on resume.
    /// </summary>
    public int Advance(double elapsedSeconds, int accelerationFactor, Func<bool> canStep, Action step)
    {
        if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }

        if (canStep is null)
        {
            throw new ArgumentNullException(nameof(canStep));
        }

        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (!canStep())
        {
            Reset();
            return 0;
        }

        var acceleratedSeconds = elapsedSeconds * Math.Clamp(accelerationFactor, 1, 256);
        var accumulated = _accumulatedSeconds + acceleratedSeconds;
        if (double.IsInfinity(accumulated))
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }

        _accumulatedSeconds = accumulated;
        var steps = 0;
        // Permit arithmetic roundoff only, not a meaningful fraction of a simulation step.
        var tolerance = _fixedDeltaSeconds * 1e-9;
        while (steps < _maxStepsPerFrame && _accumulatedSeconds + tolerance >= _fixedDeltaSeconds)
        {
            _accumulatedSeconds = Math.Max(0, _accumulatedSeconds - _fixedDeltaSeconds);
            step();
            steps++;
            if (!canStep())
            {
                Reset();
                break;
            }
        }

        return steps;
    }

    /// <summary>Discards fractional accumulation and backlog on pause, restart or scenario change.</summary>
    public void Reset() => _accumulatedSeconds = 0;
}
