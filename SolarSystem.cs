using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

public record struct SolarSystem
(
    string Symbol,
    string SectorSymbol,
    SystemType Type,
    int X,
    int Y,
    SolarSystemWaypoint[] Waypoints,
    Faction[] Factions
)
{
    public static SolarSystem FromInternal(CosmosPeddler.SDK.Internal.SolarSystem data) =>
        new
        (
            Symbol: data.Symbol,
            SectorSymbol: data.SectorSymbol,
            Type: data.Type,
            X: data.X,
            Y: data.Y,
            Waypoints: data.Waypoints.Select(SolarSystemWaypoint.FromInternal).ToArray(),
            Factions: data.Factions.Select(Faction.FromInternal).ToArray()
        );
}

public record struct SolarSystemWaypoint
(
    string Symbol,
    WaypointType Type,
    int X,
    int Y
)
{
    public static SolarSystemWaypoint FromInternal(CosmosPeddler.SDK.Internal.SystemWaypoint data) =>
        new
        (
            Symbol: data.Symbol,
            Type: data.Type,
            X: data.X,
            Y: data.Y
        );
}
