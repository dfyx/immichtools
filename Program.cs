using ImmichTools.Tools;
using System.CommandLine;

namespace ImmichTools;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("Tools for interacting with the Immich API");

        var hostOption = new Option<string>("--host", "The Immich host to talk to");
        hostOption.AddAlias("-h");
        rootCommand.AddGlobalOption(hostOption);

        var apiKeyOption = new Option<string>("--api-key", "The Immich API key");
        apiKeyOption.AddAlias("-k");
        rootCommand.AddGlobalOption(apiKeyOption);

        rootCommand.AddCommand(CreateAutoStackCommand(hostOption, apiKeyOption));

        await rootCommand.InvokeAsync(args);
    }

    private static Command CreateAutoStackCommand(Option<string> hostOption, Option<string> apiKeyOption)
    {
        var command = new Command("autostack", "Automatically combines assets with matching basename into stacks");

        var directoryArgument = CreateDirectoryArgument();
        command.AddArgument(directoryArgument);

        var recursiveOption = CreateRecursiveOption();
        command.AddOption(recursiveOption);

        var copyMetadataOption = new Option<bool>("-m", "Copy metadata from raw image to edited versions");
        command.AddOption(copyMetadataOption);

        command.SetHandler(AutoStack.RunAsync, hostOption, apiKeyOption, directoryArgument, recursiveOption, copyMetadataOption);
        return command;
    }

    private static Argument<string> CreateDirectoryArgument()
    {
        return new Argument<string>("directory", "The directory in which the assets should be searched");
    }

    private static Option<bool> CreateRecursiveOption()
    {
        return new Option<bool>("-r", "Recursively include subdirectories");
    }
}