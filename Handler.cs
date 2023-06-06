using System.Net.Http.Headers;
using System.Collections.Concurrent;

using Polly;
using Polly.Retry;

namespace CosmosPeddler.SDK;

public class SpaceTradersHandler : DelegatingHandler
{
    private readonly ICosmosLogger? logger;
    private readonly RetryPolicy retryHttpClonePolicy;
    private readonly ConcurrentDictionary<HttpRequestMessage, Task<HttpResponseMessage>> requests = new(new HttpRequestEqualityComparer());

    public SpaceTradersHandler(HttpMessageHandler innerHandler, ICosmosLogger? logger = null) : base(innerHandler)
    {
        this.logger = logger;
        retryHttpClonePolicy = Policy.Handle<Exception>().Retry(5, (e, i) => logger?.Warning($"Attempt {i} at cloning HTTP response failed"));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger?.Info($"Sending request to {request.RequestUri}");

        if (requests.TryGetValue(request, out var cachedResponse))
        {
            if (cachedResponse.IsCompleted)
            {
                requests.TryRemove(new KeyValuePair<HttpRequestMessage, Task<HttpResponseMessage>>(request, cachedResponse));
            }
            else
            {
                logger?.Info($"Returned cached response for {request.RequestUri}");
                return CloneHttpResponseMessage(await cachedResponse);
            }
        }

        var response = base.SendAsync(request, cancellationToken).ContinueWith(t =>
        {
            requests.TryRemove(new KeyValuePair<HttpRequestMessage, Task<HttpResponseMessage>>(request, t));

            return t.Result;
        });

        requests.TryAdd(request, response);

        return CloneHttpResponseMessage(await response);
    }

    private static MemoryStream HttpResponseMessageToStream(HttpResponseMessage response)
    {
        var stream = new MemoryStream();
        response.Content.CopyTo(stream, null, new CancellationToken());
        stream.Position = 0;
        return stream;
    }

    private HttpResponseMessage CloneHttpResponseMessage(HttpResponseMessage res)
    {
        return retryHttpClonePolicy.Execute(() =>
        {
            var clone = new HttpResponseMessage(res.StatusCode);

            // Copy the request's content (via a MemoryStream) into the cloned object
            if (res.Content != null)
            {
                lock(res)
                {
                    var ms = HttpResponseMessageToStream(res);

                    var ms2 = new MemoryStream();
                    ms.CopyTo(ms2);
                    ms.Position = 0;
                    ms2.Position = 0;

                    res.Content = new StreamContent(ms);
                    clone.Content = new StreamContent(ms2);
                }

                // Copy the content headers
                foreach (var h in res.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            clone.Version = res.Version;

            foreach (var header in res.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            foreach (var header in res.TrailingHeaders)
                clone.TrailingHeaders.TryAddWithoutValidation(header.Key, header.Value);

            clone.RequestMessage = res.RequestMessage;

            return clone;
        });
    }
}

class HttpRequestEqualityComparer : IEqualityComparer<HttpRequestMessage>
{
    public bool Equals(HttpRequestMessage? x, HttpRequestMessage? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        return x.RequestUri == y.RequestUri &&
            x.Method == y.Method &&
            ContentsEqual(x.Content, y.Content) &&
            HeadersEqual(x.Headers, y.Headers);
    }

    private static bool ContentsEqual(HttpContent? x, HttpContent? y)
    {
        if (x == null && y == null)
            return true;

        if (x == null || y == null)
            return false;

        return x.ReadAsStringAsync().Result == y.ReadAsStringAsync().Result;
    }

    private static bool HeadersEqual(HttpRequestHeaders x, HttpRequestHeaders y)
    {
        foreach (var header in x)
        {
            if (!y.TryGetValues(header.Key, out var values) || !header.Value.SequenceEqual(values))
                return false;
        }

        return true;
    }

    public int GetHashCode(HttpRequestMessage obj)
    {
        return (obj.RequestUri?.GetHashCode() ?? 0) ^
            obj.Method.GetHashCode() ^
            (obj.Content == null ? 0 : GetContentHashCode(obj.Content)) ^
            GetHeadersHashCode(obj.Headers);
    }

    private static int GetContentHashCode(HttpContent content)
    {
        return content.ReadAsStringAsync().Result.GetHashCode();
    }

    private static int GetHeadersHashCode(HttpRequestHeaders headers)
    {
        var hash = 0;

        foreach (var header in headers)
        {
            hash ^= header.Key.GetHashCode();
            hash ^= header.Value.GetHashCode();
        }

        return hash;
    }
}