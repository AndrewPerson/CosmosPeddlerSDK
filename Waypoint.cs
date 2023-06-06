using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

public record struct Waypoint
(
    string SystemSymbol,
    string Symbol,
    WaypointType Type,
    int X,
    int Y,
    Orbital[] Orbitals,
    Faction Faction,
    Trait[] Traits
)
{
    public static Waypoint FromInternal(CosmosPeddler.SDK.Internal.Waypoint data) =>
        new
        (
            SystemSymbol: data.SystemSymbol,
            Symbol: data.Symbol,
            Type: data.Type,
            X: data.X,
            Y: data.Y,
            Orbitals: data.Orbitals.Select(Orbital.FromInternal).ToArray(),
            Faction: Faction.FromInternal(data.Faction),
            Traits: data.Traits.Select(Trait.FromInternal).ToArray()
        );
}

public record struct Orbital
(
    string Symbol
)
{
    public static Orbital FromInternal(CosmosPeddler.SDK.Internal.WaypointOrbital data) =>
        new
        (
            Symbol: data.Symbol
        );
}

public record struct Trait
(
    WaypointTraitSymbol Symbol,
    string Name,
    string Description
)
{
    public static Trait FromInternal(CosmosPeddler.SDK.Internal.WaypointTrait data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description
        );
}