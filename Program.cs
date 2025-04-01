using ImmichTools.Tools;
using System.CommandLine;

namespace ImmichTools
{
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

            var autoStackCommand = new Command("autostack", "Automatically combines assets with matching basename into stacks");

            var directoryArgument = new Argument<string>("directory", "The directory in which the assets should be searched");
            autoStackCommand.AddArgument(directoryArgument);

            var recursiveOption = new Option<bool>("-r", "Recursively include subdirectories");
            autoStackCommand.AddOption(recursiveOption);

            var copyMetadataOption = new Option<bool>("-m", "Copy metadata from raw image to edited versions");
            autoStackCommand.AddOption(copyMetadataOption);

            autoStackCommand.SetHandler(AutoStack.RunAsync, hostOption, apiKeyOption, directoryArgument, recursiveOption, copyMetadataOption);
            rootCommand.AddCommand(autoStackCommand);

            await rootCommand.InvokeAsync(args);
        }
    }
}