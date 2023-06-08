namespace CosmosPeddler.SDK;

public static class Util
{
    public static string ExtractSystemSymbolFromWaypointSymbol(this string waypointSymbol)
    {
        return waypointSymbol.Split('-').SkipLast(1).Aggregate((a, b) => $"{a}-{b}");
    }
}