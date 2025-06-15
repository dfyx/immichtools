using System.Net.Http.Json;
using System.Web;
using ImmichTools.Json;
using ImmichTools.ReplyData;

namespace ImmichTools.Tools;

internal class ToolBase
{
    protected static HttpClient CreateHttpClient(string host, string apiKey)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(host);
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        return client;
    }

    protected static async Task<IEnumerable<string>> GetDirectoriesRecursiveAsync(string directory, HttpClient client)
    {
        IEnumerable<string> directories = await client.GetFromJsonAsync("/api/view/folder/unique-paths", SerializerContext.Default.StringArray) ?? [ directory ];
        if (directory.StartsWith("/"))
        {
            directories = directories.Select(d => d.StartsWith("/") ? d : "/" + d);
        }
        return directories.Where(d => !Path.GetRelativePath(directory, d).StartsWith(".."));
    }

    protected static async Task<Asset[]> GetAssetsAsync(HttpClient client, string directory, bool recursive)
    {
        var directories = recursive
            ? await GetDirectoriesRecursiveAsync(directory, client)
            : [ directory ];

        var assetTasks = directories.Select(d => client.GetFromJsonAsync<Asset[]>(
            "/api/view/folder?path=" + HttpUtility.UrlEncode(d),
            SerializerContext.Default.AssetArray));
        var assetArrays = await Task.WhenAll(assetTasks);
        var assets = assetArrays.SelectMany(a => a ?? []).ToArray();
        return assets;
    }

    protected static string GetRelativePath(string directory, Asset asset)
    {
        return Path.GetRelativePath(directory, asset.OriginalPath).Replace(Path.DirectorySeparatorChar, '/');
    }
}