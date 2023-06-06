namespace CosmosPeddler.SDK;

public record struct Cooldown
(
    DateTimeOffset Expiration
)
{
    public static Cooldown FromInternal(CosmosPeddler.SDK.Internal.Cooldown data) =>
        new
        (
            Expiration: data.Expiration
        );
}