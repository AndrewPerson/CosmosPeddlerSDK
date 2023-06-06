namespace CosmosPeddler.SDK;

public record struct Agent
(
    string Id,
    string Symbol,
    string Headquarters,
    long Credits,
    string StartingFaction
)
{
    public static Agent FromInternal(CosmosPeddler.SDK.Internal.Agent data) =>
        new
        (
            Id: data.AccountId,
            Symbol: data.Symbol,
            Headquarters: data.Headquarters,
            Credits: data.Credits,
            StartingFaction: data.StartingFaction
        );
}