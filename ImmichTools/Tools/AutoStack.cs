using ImmichTools.Json;
using ImmichTools.ReplyData;
using ImmichTools.RequestData;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ImmichTools.Helpers;

namespace ImmichTools.Tools;

internal class AutoStack : ToolBase
{
    internal static async Task RunAsync(string host, string apiKey, string directory, bool recursive, bool copyMetadata)
    {
        var client = CreateHttpClient(host, apiKey);
        var assets = await GetAssetsAsync(client, directory, recursive);

        if(assets.Length == 0)
        {
            return;
        }

        var groupedAssets = assets.GroupBy(GetBaseName).Where(g => g.Count() > 1).ToArray();
        var stackCount = groupedAssets.Length;
        var i = 1;
        foreach (var group in groupedAssets)
        {
            var sortedAssets = group.OrderByDescending(FilePrioritizer.GetFilePriority)
                .ThenByDescending(a => Path.GetDirectoryName(Path.GetRelativePath(directory, a.OriginalPath)))
                .ThenBy(a => Path.GetFileNameWithoutExtension(a.OriginalFileName))
                .ToArray();

            Console.WriteLine("Stack {0}/{1}: {2}", i, stackCount, string.Join(", ", sortedAssets.Select(a => GetRelativePath(directory, a)).ToArray()));
            await client.PostAsJsonAsync(
                "/api/stacks",
                new CreateStack { AssetIds = sortedAssets.Select(a => a.Id).ToList() },
                SerializerContext.Default.CreateStack);

            if (copyMetadata)
            {
                var rawImageAsset = sortedAssets.LastOrDefault(a => FilePrioritizer.GetFilePriority(a) == FilePrioritizer.RawPriority);
                if (rawImageAsset != null)
                {
                    foreach (var asset in sortedAssets.Where(a => a.LocalDateTime != rawImageAsset.LocalDateTime))
                    {
                        Console.WriteLine("Copying metadata from {0} to {1}", GetRelativePath(directory, rawImageAsset), GetRelativePath(directory, asset));
                        await client.PutAsJsonAsync(
                            $"/api/assets/{asset.Id}",
                            new UpdateAsset
                            {
                                DateTimeOriginal = rawImageAsset.ExifInfo?.DateTimeOriginal ?? asset.ExifInfo?.DateTimeOriginal,
                                Latitude = rawImageAsset.ExifInfo?.Latitude ?? asset.ExifInfo?.Latitude,
                                Longitude = rawImageAsset.ExifInfo?.Longitude ?? asset.ExifInfo?.Latitude
                            },
                            SerializerContext.Default.UpdateAsset);
                    }
                }
            }
            i++;
        }
    }

    private static readonly Regex BaseNameRegex = new(@"\A(?<BaseName>[a-zA-Z]+(?:_[0-9]+)+)([_-].*)?\Z");

    private static string GetBaseName(Asset asset)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(asset.OriginalFileName);
        var firstBlock = withoutExtension.Split('.').FirstOrDefault(b => !string.IsNullOrEmpty(b)) ?? withoutExtension;
        var match = BaseNameRegex.Match(firstBlock);
        return match.Success ? match.Groups["BaseName"].Value : firstBlock;
    }
}
