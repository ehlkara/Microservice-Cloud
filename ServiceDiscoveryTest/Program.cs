using ServiceDiscovery;

var ring = new ChordRing();

// Add 3 nodes to the ring
ring.Join("node1", "http://localhost:5001");
ring.Join("node2", "http://localhost:5002");
ring.Join("node3", "http://localhost:5003");

Console.WriteLine("\nNodes in the ring:");
foreach (var node in ring.GetNodes())
{
    Console.WriteLine($"Node: {node.Id}, Address: {node.Address}, Hash: {node.Hash}");
}

// Test service lookups
var services = new[] { "order-service", "invoice-service", "payment-service" };

Console.WriteLine("\nService lookups:");
foreach (var service in services)
{
    var address = ring.Lookup(service);
    Console.WriteLine($"Service: {service} -> Node Address: {address}");
}

// Test node leaving
Console.WriteLine("\nTesting node leaving...");
ring.Leave("node2");

Console.WriteLine("\nNodes in the ring after node2 left:");
foreach (var node in ring.GetNodes())
{
    Console.WriteLine($"Node: {node.Id}, Address: {node.Address}, Hash: {node.Hash}");
}

Console.WriteLine("\nService lookups after node2 left:");
foreach (var service in services)
{
    var address = ring.Lookup(service);
    Console.WriteLine($"Service: {service} -> Node Address: {address}");
}
