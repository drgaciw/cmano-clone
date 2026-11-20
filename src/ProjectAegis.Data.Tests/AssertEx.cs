using NUnit.Framework;

namespace ProjectAegis.Data.Tests;

/// <summary>
/// xUnit <c>Assert.Single</c> returns the sole matching item. NUnit's count constraint does not.
/// </summary>
internal static class AssertEx
{
    public static T Single<T>(IEnumerable<T> items)
    {
        var list = items.ToList();
        Assert.That(list, Has.Exactly(1).Items);
        return list[0];
    }

    public static T Single<T>(IEnumerable<T> items, Func<T, bool> predicate)
    {
        var list = items.Where(predicate).ToList();
        Assert.That(list, Has.Exactly(1).Items);
        return list[0];
    }
}
