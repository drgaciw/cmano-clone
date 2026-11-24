using ProjectAegis.Ai.Host.Cli;

namespace ProjectAegis.Ai.Host;

internal static class Program
{
    private static Task<int> Main(string[] args) =>
        AiHostCliRunner.RunAsync(args, Console.In, Console.Out, Console.Error);
}
