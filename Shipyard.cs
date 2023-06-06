using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

public record struct Shipyard
(
    ShipyardShip[] Ships
)
{
    public static Shipyard FromInternal(CosmosPeddler.SDK.Internal.Shipyard data) =>
        new
        (
            Ships: data.Ships.Select(ShipyardShip.FromInternal).ToArray()
        );
}

public record struct ShipyardShip
(
    ShipType Type,
    string Name,
    string Description,
    int PurchasePrice,
    Frame Frame,
    Engine Engine,
    Module[] Modules,
    Mount[] Mounts
)
{
    public static ShipyardShip FromInternal(CosmosPeddler.SDK.Internal.ShipyardShip data) =>
        new
        (
            Type: data.Type,
            Name: data.Name,
            Description: data.Description,
            PurchasePrice: data.PurchasePrice,
            Frame: Frame.FromInternal(data.Frame),
            Engine: Engine.FromInternal(data.Engine),
            Modules: data.Modules.Select(Module.FromInternal).ToArray(),
            Mounts: data.Mounts.Select(Mount.FromInternal).ToArray()
        );
}