using System.Net.Http.Json;
using System.Text.Json;
using ImmichTools.Json;
using ImmichTools.ReplyData;
using ImmichTools.RequestData;

namespace ImmichTools.Tools;

internal class DateFix : ToolBase
{
    public static async Task RunAsync(
        string host,
        string apiKey,
        string directory,
        string dateString,
        bool recursive,
        bool relative)
    {
        if (!DateTime.TryParse(dateString, out var date))
        {
            Console.WriteLine("ERROR: Cannot parse date {0}. Aborting.", dateString);
            return;
        }
        
        var client = CreateHttpClient(host, apiKey);
        var assets = await GetAssetsAsync(client, directory, recursive);

        if(assets.Length == 0)
        {
            return;
        }
        
        DateTime? baseDate = null;
        if (relative)
        {
            baseDate = assets.Where(a => a.ExifInfo?.DateTimeOriginal != null).Min(a => a.ExifInfo!.DateTimeOriginal);
        }

        await Task.WhenAll(assets.Select(a => UpdateAssetDateAsync(client, directory, a, baseDate, date)));
    }

    private static async Task UpdateAssetDateAsync(HttpClient client, string directory, Asset asset, DateTime? baseDate, DateTime date)
    {
        var newDate = (baseDate.HasValue ? date + (asset.ExifInfo?.DateTimeOriginal - baseDate.Value) : date) ?? date;
        if (newDate == asset.ExifInfo?.DateTimeOriginal)
        {
            return;
        }
        
        Console.WriteLine("Changing date of {0} to {1}", GetRelativePath(directory, asset), newDate.ToString("O"));
        var response = await client.PutAsJsonAsync(
            $"/api/assets/{asset.Id}",
            new UpdateAsset
            {
                DateTimeOriginal = newDate
            },
            SerializerContext.Default.UpdateAsset);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("ERROR: Failed to update asset date of {0}: {1}", asset.Id, response.StatusCode);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();
        }
    }
}