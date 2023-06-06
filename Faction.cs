using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

public record struct Faction
(
    string Symbol
)
{
    public static Faction FromInternal(WaypointFaction data) =>
        new
        (
            Symbol: data.Symbol
        );

    public static Faction FromInternal(SystemFaction data) =>
        new
        (
            Symbol: data.Symbol

        );
}

public record struct FullFaction
(
    string Symbol,
    string Name,
    string Description,
    string Headquarters,
    FactionTrait[] Traits,
    bool IsRecruiting
)
{
    public static FullFaction FromInternal(Internal.Faction data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Headquarters: data.Headquarters,
            Traits: data.Traits.Select(FactionTrait.FromInternal).ToArray(),
            IsRecruiting: data.IsRecruiting
        );
}

public record struct FactionTrait
(
    FactionTraitSymbol Symbol,
    string Name,
    string Description
)
{
    public static FactionTrait FromInternal(Internal.FactionTrait data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description
        );
}
