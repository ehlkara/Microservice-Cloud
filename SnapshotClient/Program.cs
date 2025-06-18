using System;
using System.Net.Http;
using System.Threading.Tasks;
using MongoDB.Driver;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new HttpClient();
        await client.PostAsync("http://localhost:5230/snapshot/start", null);
        Console.WriteLine("Snapshot started.");

        var mongoClient = new MongoClient("mongodb://localhost:27017");
        var database = mongoClient.GetDatabase("SnapshotDB");
        var collection = database.GetCollection<LocalState>("LocalStates");
        var localStates = await collection.Find(_ => true).ToListAsync();
        foreach (var state in localStates)
        {
            Console.WriteLine($"Process Count: {state.ProcessCount}, Queue Messages: {string.Join(", ", state.QueueMessages)}");
        }
    }
}

public class LocalState
{
    public int ProcessCount { get; set; }
    public List<string> QueueMessages { get; set; } = new List<string>();
}
