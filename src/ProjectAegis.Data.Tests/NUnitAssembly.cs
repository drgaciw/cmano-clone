using NUnit.Framework;

// xunit.runner.json set parallelizeAssembly=false and parallelizeTestCollections=false.
[assembly: NonParallelizable]
[assembly: LevelOfParallelism(1)]
