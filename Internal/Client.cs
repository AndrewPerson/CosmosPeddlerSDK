using System.Net.Http.Headers;

namespace CosmosPeddler.SDK.Internal;

public partial class SpaceTradersClient
{
    public string Token => token;
    private string token;

    public SpaceTradersClient(HttpClient client, string token)
    {
        this._httpClient = client;
        this.token = token;
    }

    partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
    {
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}