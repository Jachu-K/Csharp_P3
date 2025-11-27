using System.Reflection;
using Plugins;
public class Program
{
    public static void Main()
    {
        var manager = new PluginManager(typeof(PluginManager).Assembly);

        Console.WriteLine("=== Typy z atrybutem Plugin ===");
        foreach (var type in manager.GetPluginTypes())
        {
            Console.WriteLine($"- {type.Name}");
        }

        Console.WriteLine("\n=== Typy dziedziczące po BasePlugin ===");
        foreach (var type in manager.GetBasePluginTypes())
        {
            Console.WriteLine($"- {type.Name}");
        }

        Console.WriteLine("\n=== Typy implementujące IProcessor<T> ===");
        foreach (var type in manager.GetProcessorTypes())
        {
            Console.WriteLine($"- {type.Name}");
        }

        Console.WriteLine("\n=== Właściwości konfigurowalne ===");
        var configProps = manager.GetConfigurableProperties();
        foreach (var (type, properties) in configProps)
        {
            Console.WriteLine($"{type.Name}:");
            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<ConfigurablePropertyAttribute>();
                Console.WriteLine($"  - {prop.Name}: {attr.Description}");
            }
        }

        // Konfiguracja i tworzenie instancji
        var configuration = new Dictionary<string, object>
        {
            { "TextProcessor.Prefix", "MODIFIED: " },
            { "NumberPlugin.Multiplier", 5 },
            { "IntProcessor.AddValue", 25 }
        };

        Console.WriteLine("\n=== Tworzenie i konfiguracja pluginów ===");
        var plugins = manager.CreateAndConfigurePlugins(configuration);

        foreach (var plugin in plugins)
        {
            Console.WriteLine($"Utworzono: {plugin.GetType().Name}");

            // Testowanie processorów
            if (plugin is IProcessor<string> textProcessor)
            {
                var result = textProcessor.Process("hello");
                Console.WriteLine($"  TextProcessor result: {result}");
            }

            if (plugin is IProcessor<int> intProcessor)
            {
                var result = intProcessor.Process(10);
                Console.WriteLine($"  IntProcessor result: {result}");
            }
        }
    }
}