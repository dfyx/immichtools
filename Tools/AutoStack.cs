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
                var rawImageAsset = sortedAssets.LastOrDefault(a => GetFileTypePriority(a) == 0);
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
    private static readonly HashSet<string> RawExtensions = [
        ".3fr",
        ".ari",
        ".arw",
        ".cap",
        ".cin",
        ".cr2",
        ".cr3",
        ".crw",
        ".dcr",
        ".dng",
        ".erf",
        ".fff",
        ".iiq",
        ".k25",
        ".kdc",
        ".mrw",
        ".nef",
        ".nrw",
        ".orf",
        ".ori",
        ".pef",
        ".psd",
        ".raf",
        ".raw",
        ".rw2",
        ".rwl",
        ".sr2",
        ".srf",
        ".srw",
        ".x3f",
    ];

    private static int GetFileTypePriority(Asset asset)
    {
        var extension = Path.GetExtension(asset.OriginalFileName).ToLowerInvariant();
        return RawExtensions.Contains(extension) ? 0 : 1;
    }

    private static readonly Regex BaseNameRegex = new Regex("\\A(?<BaseName>[a-zA-Z]+_[0-9]+)([_-].*)?\\Z");

    private static string GetBaseName(Asset asset)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(asset.OriginalFileName);
        var match = BaseNameRegex.Match(withoutExtension);
        return match.Success ? match.Groups["BaseName"].Value : withoutExtension;
    }
}
