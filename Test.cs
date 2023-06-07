using CosmosPeddler.SDK;

public class Program
{
    public static async Task Main(string[] args)
    {
        var client = await CosmosPeddlerClient.Load($"/home/andrew/.local/share/godot/app_userdata/Cosmos-Peddler/cosmos-peddler.json");
        
        if (await client.HasValidToken())
        {
            Console.WriteLine("Agent is valid");
        }

        client.SolarSystems.Subscribe(tuple =>
        {
            Console.WriteLine($"System: {tuple.Item2.Symbol}");
        });

        Console.WriteLine("Press any key to exit");

        Console.ReadKey();
    }
}