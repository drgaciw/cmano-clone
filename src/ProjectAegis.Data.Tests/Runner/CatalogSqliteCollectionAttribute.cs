using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace ProjectAegis.Data.Tests.Runner;

/// <summary>
/// xUnit <c>[Collection("CatalogSqlite", DisableParallelization = true)]</c>.
/// Creates one <see cref="Catalog.CatalogSqliteEnvFixture"/> for the fixture
/// (clears <c>AEGIS_PUBLIC_CORPUS</c>) and disposes it after that fixture's tests.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CatalogSqliteCollectionAttribute : Attribute, ITestAction
{
    private Catalog.CatalogSqliteEnvFixture? _fixture;

    /// <inheritdoc />
    public void BeforeTest(ITest test)
    {
        if (test.IsSuite)
        {
            _fixture = new Catalog.CatalogSqliteEnvFixture();
        }
    }

    /// <inheritdoc />
    public void AfterTest(ITest test)
    {
        if (!test.IsSuite)
        {
            return;
        }

        _fixture?.Dispose();
        _fixture = null;
    }

    /// <inheritdoc />
    public ActionTargets Targets => ActionTargets.Suite;
}
