using System.Text.Json;
using System.Reactive.Linq;

using Polly;
using Polly.Retry;

using CosmosPeddler.SDK.Observables;
using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

using AgentData = CosmosPeddler.SDK.Agent;
using SolarSystemData = CosmosPeddler.SDK.SolarSystem;

public class CosmosPeddlerClient
{
    private SpaceTradersClient client;

    private static AsyncRetryPolicy retry429Policy = Policy.Handle<ApiException>(e => e.StatusCode == 429)
        .WaitAndRetryForeverAsync
        (
            sleepDurationProvider: (retryCount, exception, _) =>
            {
                if (exception is ApiException apiException)
                {
                    return TimeSpan.FromSeconds(apiException.Headers.TryGetValue("Retry-After", out var retryAfter) ? float.Parse(retryAfter.First()) : 2);
                }
                else return TimeSpan.FromSeconds(2);
            },
            onRetryAsync: (_, _, _) => Task.CompletedTask
        );

    private ICosmosLogger? logger;

    public LazySubject<Agent> Agent => new
    (
        async () => await retry429Policy.ExecuteAsync(client.GetMyAgentAsync).ContinueWith(t =>
            AgentData.FromInternal(t.Result.Data))
    );

    public LazyUniqueBySubject<string, Ship> Ships { get; }

    public IndividualLazyUniqueBySubject<string, Cooldown> Cooldowns { get; }

    public LazyUniqueBySubject<string, SolarSystem> SolarSystems { get; }

    public IndividualLazyUniqueBySubject<string, Waypoint> Waypoints { get; }

    public static async Task<CosmosPeddlerClient> Register(string accountSymbol, string faction, string email = "")
    {
        var client = new SpaceTradersClient(new HttpClient(new SpaceTradersHandler(new HttpClientHandler())));

        var token = (await client.RegisterAsync
        (
            new Body()
            {
                Symbol = accountSymbol,
                Faction = faction,
                Email = email
            }
        )).Data.Token;

        return new CosmosPeddlerClient(token);
    }

    public static CosmosPeddlerClient FromToken(string token, ICosmosLogger? logger = null)
    {
        return new CosmosPeddlerClient(token, logger);
    }

    public static async Task<CosmosPeddlerClient> Load(string file, ICosmosLogger? logger = null)
    {
        var json = await File.ReadAllTextAsync(file);
        var data = JsonDocument.Parse(await File.ReadAllTextAsync(file)).RootElement;

        return CosmosPeddlerClient.FromToken(data.GetProperty("token").GetString()!, logger);
    }

    private static string ExtractSystemSymbolFromWaypointSymbol(string waypointSymbol)
    {
        // TODO Make this more robust.
        return waypointSymbol.Split('-').SkipLast(1).Aggregate((a, b) => $"{a}-{b}");
    }

    private CosmosPeddlerClient(string token, ICosmosLogger? logger = null)
    {
        client = new SpaceTradersClient(new HttpClient(new SpaceTradersHandler(new HttpClientHandler(), logger)), token);

        this.logger = logger;

        Ships = new
        (
            getKey: ship => ship.Symbol,
            firstValues: () => GetAllShips()
        );

        Cooldowns = new
        (
            valueFactory: shipSymbol => GetCooldown(shipSymbol)
        );

        SolarSystems = new
        (
            getKey: system => system.Symbol,
            firstValues: () => GetSolarSystems()
        );

        Waypoints = new
        (
            valueFactory: waypointSymbol => GetWaypoint(waypointSymbol)
        );
    }

    public async Task Save(string file)
    {
        var json = JsonSerializer.Serialize(new
        {
            token = client.Token
        });

        await File.WriteAllTextAsync(file, json);
    }

    public Task<bool> HasValidToken()
    {
        return retry429Policy.ExecuteAsync(client.GetMyAgentAsync).ContinueWith(t => t.IsCompletedSuccessfully);
    }

#region factions
    public static async IAsyncEnumerable<FullFaction> GetAllFactions(int pageSize = 20, ICosmosLogger? logger = null)
    {
        var client = new SpaceTradersClient(new HttpClient(new SpaceTradersHandler(new HttpClientHandler(), logger)));

        int total;
        int currentPage = 1;
        do
        {
            var data = await retry429Policy.ExecuteAsync(() => client.GetFactionsAsync(currentPage, pageSize));

            total = data.Meta.Total;
    
            foreach (var faction in data.Data.Select(FullFaction.FromInternal))
            {
                yield return faction;
            }
        }
        while (total > pageSize * currentPage++);
    }
#endregion

#region market
    public async Task<Market> GetMarket(string waypointSymbol)
    {
        var systemSymbol = ExtractSystemSymbolFromWaypointSymbol(waypointSymbol);

        var data = await retry429Policy.ExecuteAsync(() => client.GetMarketAsync(systemSymbol, waypointSymbol));

        return Market.FromInternal(data.Data);
    }
#endregion

#region shipyard
    public async Task<Shipyard> GetShipyard(string waypointSymbol)
    {
        var systemSymbol = ExtractSystemSymbolFromWaypointSymbol(waypointSymbol);

        var data = await retry429Policy.ExecuteAsync(() => client.GetShipyardAsync(systemSymbol, waypointSymbol));

        return Shipyard.FromInternal(data.Data);
    }
#endregion

#region fleet
    private async IAsyncEnumerable<Ship> GetAllShips(int pageSize = 20)
    {
        int total;
        int currentPage = 1;
        do
        {
            var data = await retry429Policy.ExecuteAsync(() => client.GetMyShipsAsync(currentPage, pageSize));

            total = data.Meta.Total;
    
            foreach (var ship in data.Data.Select(Ship.FromInternal))
            {
                yield return ship;
            }
        }
        while (total > pageSize * currentPage++);
    }    

    private async Task<Cooldown> GetCooldown(string shipSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.GetShipCooldownAsync(shipSymbol))).Data;
        return Cooldown.FromInternal(data);
    }

    public async Task<IObservable<Ship>> PurchaseShip(ShipType type, string waypointSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.PurchaseShipAsync
        (
            new Body3()
            {
                ShipType = type,
                WaypointSymbol = waypointSymbol
            }
        ))).Data;

        Agent.OnNext(AgentData.FromInternal(data.Agent));
        Ships.OnNext(Ship.FromInternal(data.Ship));

        return from keyValue in Ships
               where keyValue.Item1 == data.Ship.Symbol
               select keyValue.Item2;
    }

    public async Task SetFlightMode(string shipSymbol, ShipNavFlightMode flightMode)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.PatchShipNavAsync
        (
            new Body9()
            {
                FlightMode = flightMode
            },
            shipSymbol
        ))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Nav = Nav.FromInternal(data) });
        }
    }

    public async Task OrbitShip(string shipSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.OrbitShipAsync(shipSymbol))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Nav = Nav.FromInternal(data.Nav) });
        }
    }

    public async Task DockShip(string shipSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.DockShipAsync(shipSymbol))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Nav = Nav.FromInternal(data.Nav) });
        }
    }

    public async Task FlyShip(string shipSymbol, string destinationWaypointSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.NavigateShipAsync
        (
            new Body8()
            {
                WaypointSymbol = destinationWaypointSymbol
            },
            shipSymbol
        ))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext
            (
                ship with
                {
                    Nav = Nav.FromInternal(data.Nav),
                    Fuel = Fuel.FromInternal(data.Fuel)
                }
            );
        }
    }

    public async Task WarpShip(string shipSymbol, string destinationWaypointSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.WarpShipAsync
        (
            new Body10()
            {
                WaypointSymbol = destinationWaypointSymbol
            },
            shipSymbol
        ))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext
            (
                ship with
                {
                    Nav = Nav.FromInternal(data.Nav),
                    Fuel = Fuel.FromInternal(data.Fuel)
                }
            );
        }
    }

    // TODO Update the cargo from this.
    public async Task JumpShip(string shipSymbol, string destinationSystemSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.JumpShipAsync
        (
            new Body7()
            {
                SystemSymbol = destinationSystemSymbol
            },
            shipSymbol
        ))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Nav = Nav.FromInternal(data.Nav) });
        }

        Cooldowns.Set(shipSymbol, Cooldown.FromInternal(data.Cooldown));
    }

    public async Task RefuelShip(string shipSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.RefuelShipAsync(shipSymbol))).Data;

        Agent.OnNext(AgentData.FromInternal(data.Agent));

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Fuel = Fuel.FromInternal(data.Fuel) });
        }
    }

    public async Task RefineCargo(string shipSymbol, Body4Produce cargo)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.ShipRefineAsync
        (
            new Body4()
            {
                Produce = cargo
            },
            shipSymbol
        ))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Cargo = Cargo.FromInternal(data.Cargo) });
        }

        Cooldowns.Set(shipSymbol, Cooldown.FromInternal(data.Cooldown));
    }

    public async Task JettisonCargo(string shipSymbol, string cargoSymbol, int units)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.JettisonAsync
        (
            new Body6()
            {
                Symbol = cargoSymbol,
                Units = units
            },
            shipSymbol
        ))).Data;

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Cargo = Cargo.FromInternal(data.Cargo) });
        }
    }

    public async Task SellCargo(string shipSymbol, string cargoSymbol, int units)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.SellCargoAsync
        (
            new SellCargoRequest()
            {
                Symbol = cargoSymbol,
                Units = units
            },
            shipSymbol
        ))).Data;

        Agent.OnNext(AgentData.FromInternal(data.Agent));

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Cargo = Cargo.FromInternal(data.Cargo) });
        }
    }

    public async Task PurchaseCargo(string shipSymbol, string cargoSymbol, int units)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.PurchaseCargoAsync
        (
            new Body11()
            {
                Symbol = cargoSymbol,
                Units = units
            },
            shipSymbol
        ))).Data;

        Agent.OnNext(AgentData.FromInternal(data.Agent));

        if (Ships.TryGetValueInstant(shipSymbol, out var ship))
        {
            Ships.OnNext(ship with { Cargo = Cargo.FromInternal(data.Cargo) });
        }
    }

    public async Task<Survey[]> SurveyCurrentWaypoint(string shipSymbol)
    {
        var data = (await retry429Policy.ExecuteAsync(() => client.CreateSurveyAsync(shipSymbol))).Data;

        Cooldowns.Set(shipSymbol, Cooldown.FromInternal(data.Cooldown));

        return data.Surveys.Select(Survey.FromInternal).ToArray();
    }
#endregion

#region systems
    private async IAsyncEnumerable<SolarSystemData> GetSolarSystems()
    {
        var response = await client.HttpClient.GetAsync("https://api.spacetraders.io/v2/systems.json");

        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Internal.SolarSystem[]>(await response.Content.ReadAsStringAsync(), client.JsonSettings)!;

        foreach (var system in data)
        {
            yield return SolarSystemData.FromInternal(system);
        }
    }

    private async Task<Waypoint> GetWaypoint(string waypointSymbol)
    {
        var systemSymbol = ExtractSystemSymbolFromWaypointSymbol(waypointSymbol);

        var data = await retry429Policy.ExecuteAsync(() => client.GetWaypointAsync(systemSymbol, waypointSymbol));

        return Waypoint.FromInternal(data.Data);
    }
#endregion
}
