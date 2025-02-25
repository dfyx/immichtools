using ImmichTools.Json;
using ImmichTools.ReplyData;
using ImmichTools.RequestData;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace ImmichTools.Tools;

internal class AutoStack
{
    private static HttpClient CreateHttpClient(string host, string apiKey)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(host);
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        return client;
    }

    internal static async Task RunAsync(string host, string apiKey, string directory, bool recursive, bool copyMetadata)
    {
        var client = CreateHttpClient(host, apiKey);
        var directories = recursive
            ? await GetDirectoriesRecursiveAsync(directory, client)
            : [ directory ];

        var assetTasks = directories.Select(d => client.GetFromJsonAsync<Asset[]>(
            "/api/view/folder?path=" + HttpUtility.UrlEncode(d),
            SerializerContext.Default.AssetArray));
        var assetArrays = await Task.WhenAll(assetTasks);
        var assets = assetArrays.SelectMany(a => a ?? []).ToArray();

        if(assets == null || assets.Length == 0)
        {
            return;
        }

        var groupedAssets = assets.GroupBy(GetBaseName).Where(g => g.Count() > 1).ToArray();
        var stackCount = groupedAssets.Length;
        var i = 1;
        foreach (var group in groupedAssets)
        {
            var sortedAssets = group.OrderByDescending(GetFileTypePriority)
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
                var rawImageAsset = sortedAssets.LastOrDefault(a => GetFileTypePriority(a) == -1);
                if (rawImageAsset != null)
                {
                    foreach (var asset in sortedAssets.Where(a => a.LocalDateTime != rawImageAsset.LocalDateTime))
                    {
                        Console.WriteLine("Copying metadata from {0} to {1}", GetRelativePath(directory, rawImageAsset), GetRelativePath(directory, asset));
                        await client.PutAsJsonAsync(
                            $"/api/assets/{asset.Id}",
                            new UpdateAsset
                            {
                                DateTimeOriginal = rawImageAsset.LocalDateTime,
                                Latitude = rawImageAsset.ExifInfo.Latitude,
                                Longitude = rawImageAsset.ExifInfo.Longitude
                            },
                            SerializerContext.Default.UpdateAsset);
                    }
                }
            }
            i++;
        }
    }

    private static string GetRelativePath(string directory, Asset asset)
    {
        return Path.GetRelativePath(directory, asset.OriginalPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static async Task<IEnumerable<string>> GetDirectoriesRecursiveAsync(string directory, HttpClient client)
    {
        IEnumerable<string> directories = await client.GetFromJsonAsync("/api/view/folder/unique-paths", SerializerContext.Default.StringArray) ?? [ directory ];
        if (directory.StartsWith("/"))
        {
            directories = directories.Select(d => d.StartsWith("/") ? d : "/" + d);
        }
        return directories.Where(d => !Path.GetRelativePath(directory, d).StartsWith(".."));
    }

    // List taken from https://github.com/immich-app/immich/blob/main/server/src/utils/mime-types.ts
    private static readonly Dictionary<string, int> FileTypePriorities = new() {
        { ".3fr", -1 },
        { ".ari", -1 },
        { ".arw", -1 },
        { ".cap", -1 },
        { ".cin", -1 },
        { ".cr2", -1 },
        { ".cr3", -1 },
        { ".crw", -1 },
        { ".dcr", -1 },
        { ".dng", -1 },
        { ".erf", -1 },
        { ".fff", -1 },
        { ".iiq", -1 },
        { ".k25", -1 },
        { ".kdc", -1 },
        { ".mrw", -1 },
        { ".nef", -1 },
        { ".nrw", -1 },
        { ".orf", -1 },
        { ".ori", -1 },
        { ".pef", -1 },
        { ".raf", -1 },
        { ".raw", -1 },
        { ".rw2", -1 },
        { ".rwl", -1 },
        { ".sr2", -1 },
        { ".srf", -1 },
        { ".srw", -1 },
        { ".x3f", -1 },

        // Consider .psd edited
        { ".psd", 1 }
    };

    private static int GetFileTypePriority(Asset asset)
    {
        var extension = Path.GetExtension(asset.OriginalFileName).ToLowerInvariant();
        return FileTypePriorities.TryGetValue(extension, out var priority) ? priority : 0;
    }

    private static readonly Regex BaseNameRegex = new Regex("\\A(?<BaseName>[a-zA-Z]+_[0-9]+)([_-].*)?\\Z");

    private static string GetBaseName(Asset asset)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(asset.OriginalFileName);
        var match = BaseNameRegex.Match(withoutExtension);
        return match.Success ? match.Groups["BaseName"].Value : withoutExtension;
    }
}
