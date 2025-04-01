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
        rootCommand.AddCommand(CreateDateFixCommand(hostOption, apiKeyOption));

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

    private static Command CreateDateFixCommand(Option<string> hostOption, Option<string> apiKeyOption)
    {
        var command = new Command("datefix", "Fixes asset dates");

        var directoryArgument = CreateDirectoryArgument();
        command.AddArgument(directoryArgument);

        var dateArgument = new Argument<string>("The date and time to set");
        command.AddArgument(dateArgument);

        var recursiveOption = CreateRecursiveOption();
        command.AddOption(recursiveOption);

        var relativeOption = new Option<bool>("-R", "Set date and time relative to earliest asset");
        command.AddOption(relativeOption);

        command.SetHandler(DateFix.RunAsync, hostOption, apiKeyOption, directoryArgument, dateArgument, recursiveOption, relativeOption);
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