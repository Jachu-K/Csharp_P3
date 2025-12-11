using System.Text.Json;

var files = Directory.EnumerateFiles(".", "*.json");

foreach (var file in files)
{
    using var stream = File.OpenRead(file);
    var data = await JsonSerializer.DeserializeAsync<Data>(stream);
    Console.WriteLine($"Deserialized: {data?.Id}");
}

public record Data(int Id, string? Name);