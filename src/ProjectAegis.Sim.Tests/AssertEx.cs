using NUnit.Framework;

namespace ProjectAegis.Sim.Tests;

/// <summary>
/// xUnit <c>Assert.Single</c> returns the sole match, and <c>Assert.All</c> runs an action per item.
/// NUnit count constraints do neither.
/// </summary>
internal static class AssertEx
{
    /// <summary>Asserts <paramref name="items"/> has one element and returns it.</summary>
    public static T Single<T>(IEnumerable<T> items)
    {
        var list = items.ToList();
        Assert.That(list, Has.Exactly(1).Items);
        return list[0];
    }

    /// <summary>Asserts exactly one element matches <paramref name="predicate"/> and returns it.</summary>
    public static T Single<T>(IEnumerable<T> items, Func<T, bool> predicate)
    {
        var list = items.Where(predicate).ToList();
        Assert.That(list, Has.Exactly(1).Items);
        return list[0];
    }

    /// <summary>Runs <paramref name="action"/> on every element. The first assertion failure fails the test.</summary>
    public static void All<T>(IEnumerable<T> items, Action<T> action)
    {
        foreach (var item in items)
        {
            action(item);
        }
    }
}
