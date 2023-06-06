using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

public record struct Ship
(
    string Symbol,
    Registration Registration,
    Nav Nav,
    Crew Crew,
    Frame Frame,
    Reactor Reactor,
    Engine Engine,
    Module[] Modules,
    Mount[] Mounts,
    Cargo Cargo,
    Fuel Fuel
)
{
    public static Ship FromInternal(CosmosPeddler.SDK.Internal.Ship data) =>
        new
        (
            Symbol: data.Symbol,
            Registration: Registration.FromInternal(data.Registration),
            Nav: Nav.FromInternal(data.Nav),
            Crew: Crew.FromInternal(data.Crew),
            Frame: Frame.FromInternal(data.Frame),
            Reactor: Reactor.FromInternal(data.Reactor),
            Engine: Engine.FromInternal(data.Engine),
            Modules: data.Modules.Select(Module.FromInternal).ToArray(),
            Mounts: data.Mounts.Select(Mount.FromInternal).ToArray(),
            Cargo: Cargo.FromInternal(data.Cargo),
            Fuel: Fuel.FromInternal(data.Fuel)
        );
}

public record struct Registration
(
    string Name,
    string FactionSymbol,
    ShipRole Role
)
{
    public static Registration FromInternal(ShipRegistration data) =>
        new
        (
            Name: data.Name,
            FactionSymbol: data.FactionSymbol,
            Role: data.Role
        );
}

public record struct Nav
(
    string SystemSymbol,
    string WaypointSymbol,
    Route Route,
    ShipNavStatus Status,
    ShipNavFlightMode FlightMode
)
{
    public static Nav FromInternal(ShipNav data) =>
        new
        (
            SystemSymbol: data.SystemSymbol,
            WaypointSymbol: data.WaypointSymbol,
            Route: Route.FromInternal(data.Route),
            Status: data.Status,
            FlightMode: data.FlightMode
        );
}

public record struct Route
(
    RouteWaypoint Departure,
    RouteWaypoint Destination,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime
)
{
    public static Route FromInternal(ShipNavRoute data) =>
        new
        (
            Departure: RouteWaypoint.FromInternal(data.Departure),
            Destination: RouteWaypoint.FromInternal(data.Destination),
            DepartureTime: data.DepartureTime,
            ArrivalTime: data.Arrival
        );
}

public record struct RouteWaypoint
(
    string SystemSymbol,
    string WaypointSymbol,
    WaypointType Type,
    int X,
    int Y
)
{
    public static RouteWaypoint FromInternal(ShipNavRouteWaypoint data) =>
        new
        (
            SystemSymbol: data.SystemSymbol,
            WaypointSymbol: data.Symbol,
            Type: data.Type,
            X: data.X,
            Y: data.Y
        );
}

public record struct Crew
(
    int Current,
    int Required,
    int Capacity,
    ShipCrewRotation Rotation,
    int Morale,
    int Wages
)
{
    public static Crew FromInternal(ShipCrew data) =>
        new
        (
            Current: data.Current,
            Required: data.Required,
            Capacity: data.Capacity,
            Rotation: data.Rotation,
            Morale: data.Morale,
            Wages: data.Wages
        );
}

public record struct Frame
(
    ShipFrameSymbol Symbol,
    string Name,
    string Description,
    int Condition,
    int ModuleSlots,
    int MountingPoints,
    int FuelCapacity,
    Requirements Requirements
)
{
    public static Frame FromInternal(ShipFrame data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Condition: data.Condition,
            ModuleSlots: data.ModuleSlots,
            MountingPoints: data.MountingPoints,
            FuelCapacity: data.FuelCapacity,
            Requirements: Requirements.FromInternal(data.Requirements)
        );
}

public record struct Reactor
(
    ShipReactorSymbol Symbol,
    string Name,
    string Description,
    int Condition,
    int PowerOutput,
    Requirements Requirements
)
{
    public static Reactor FromInternal(ShipReactor data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Condition: data.Condition,
            PowerOutput: data.PowerOutput,
            Requirements: Requirements.FromInternal(data.Requirements)
        );
}

public record struct Engine
(
    ShipEngineSymbol Symbol,
    string Name,
    string Description,
    int Condition,
    double Speed,
    Requirements Requirements
)
{
    public static Engine FromInternal(ShipEngine data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Condition: data.Condition,
            Speed: data.Speed,
            Requirements: Requirements.FromInternal(data.Requirements)
        );
}

public record struct Module
(
    ShipModuleSymbol Symbol,
    string Name,
    string Description,
    int Range,
    int Capacity,
    Requirements Requirements
)
{
    public static Module FromInternal(ShipModule data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Range: data.Range,
            Capacity: data.Capacity,
            Requirements: Requirements.FromInternal(data.Requirements)
        );
}

public record struct Mount
(
    ShipMountSymbol Symbol,
    string Name,
    string Description,
    int Strength,
    // TODO Make own enum.
    Deposits[] Deposits,
    Requirements Requirements
)
{
    public static Mount FromInternal(ShipMount data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Strength: data.Strength,
            Deposits: data.Deposits.ToArray(),
            Requirements: Requirements.FromInternal(data.Requirements)
        );
}

public record struct Requirements
(
    int Power,
    int Crew,
    int Slots
)
{
    public static Requirements FromInternal(ShipRequirements data) =>
        new
        (
            Power: data.Power,
            Crew: data.Crew,
            Slots: data.Slots
        );
}

public record struct Cargo
(
    int Capacity,
    CargoItem[] Inventory
)
{
    public static Cargo FromInternal(ShipCargo data) =>
        new
        (
            Capacity: data.Capacity,
            Inventory: data.Inventory.Select(CargoItem.FromInternal).ToArray()
        );
}

public record struct CargoItem
(
    string Symbol,
    string Name,
    string Description,
    int Units
)
{
    public static CargoItem FromInternal(ShipCargoItem data) =>
        new
        (
            Symbol: data.Symbol,
            Name: data.Name,
            Description: data.Description,
            Units: data.Units
        );
}

public record struct Fuel
(
    int Current,
    int Capacity
)
{
    public static Fuel FromInternal(ShipFuel data) =>
        new
        (
            Current: data.Current,
            Capacity: data.Capacity
        );
}