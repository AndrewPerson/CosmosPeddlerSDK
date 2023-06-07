using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace CosmosPeddler.SDK.Internal;

public partial class SpaceTradersClient
{
    public JsonSerializerSettings JsonSettings => JsonSerializerSettings;
    public HttpClient HttpClient => _httpClient;

    public string Token => token;
    private string token;

    public SpaceTradersClient(HttpClient client, string token) : this(client)
    {
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