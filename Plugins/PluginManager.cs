using System.Reflection;

namespace Plugins;

public class PluginManager
{
    private readonly Assembly _assembly;
    
    public PluginManager(Assembly assembly)
    {
        _assembly = assembly;
    }
    
    // Etap 4.1: Pobierz wszystkie typy oznaczone atrybutem PluginAttribute
    public IEnumerable<Type> GetPluginTypes()
    {
        return _assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<PluginAttribute>(false).Any());
        // Uwaga: GetCustomAttributes zwraca kolekcję - trzeba sprawdzić czy jest niepusta
        // false oznacza, że nie szukamy atrybutów w klasach dziedziczących
        
        // TODO: Zwróć wszystkie typy z assembly oznaczone atrybutem PluginAttribute
    }
    
    // Etap 4.2: Pobierz typy dziedziczące po BasePlugin
    public IEnumerable<Type> GetBasePluginTypes()
    {
        return _assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(BasePlugin)) && !type.IsAbstract);
        // IsSubclassOf sprawdza dziedziczenie, !IsAbstract wyklucza klasy abstrakcyjne
        
        // TODO: Zwróć typy które dziedziczą po BasePlugin
    }
    
    // Etap 4.3: Pobierz typy implementujące IProcessor<T>
    public IEnumerable<Type> GetProcessorTypes()
    {
        return _assembly.GetTypes()
            .Where(type => type.GetInterfaces()
                .Any(interfaceType => interfaceType.IsGenericType && 
                                      interfaceType.GetGenericTypeDefinition() == typeof(IProcessor<>)));
        // Sprawdzamy czy typ implementuje generyczny interfejs IProcessor<>
        
        // TODO: Zwróć typy które implementują interfejs IProcessor<T>
    }
    
    // Etap 4.4: Pobierz właściwości oznaczone atrybutem ConfigurablePropertyAttribute
    public Dictionary<Type, List<PropertyInfo>> GetConfigurableProperties()
    {
        Dictionary<Type, List<PropertyInfo>> result = new Dictionary<Type, List<PropertyInfo>>();
        // Tylko typy z atrybutem Plugin (użyj metody z etapu 4.1)
        var pluginTypes = GetPluginTypes();
    
        foreach (var type in pluginTypes)
        {
            var configurableProperties = type.GetProperties()
                .Where(prop => prop.GetCustomAttribute<ConfigurablePropertyAttribute>() != null)
                .ToList();

            // Dodaj tylko jeśli typ ma konfigurowalne właściwości
            if (configurableProperties.Count > 0)
            {
                result.Add(type, configurableProperties);
            }
        }

        return result;
        // TODO: Dla każdego typu pluginu zwróć listę właściwości oznaczonych ConfigurablePropertyAttribute
    }
    
    // Etap 4.5: Utwórz instancje i skonfiguruj właściwości
    public List<object> CreateAndConfigurePlugins(Dictionary<string, object> configuration)
    {
        List<object> result = new List<object>();
        var plugins = GetPluginTypes();
        foreach (var plugin in plugins)
        {
            var constructor = plugin.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                Console.WriteLine($"Brak bezparametrowego konstruktora w {plugin.Name} - pomijam");
                continue; // zamiast throw
            }

            var instance = constructor.Invoke(null);
            var configurables = plugin.GetProperties()
                .Where(prop => prop.GetCustomAttribute<ConfigurablePropertyAttribute>() != null);

            foreach (var property in configurables)
            {
                string key = $"{plugin.Name}.{property.Name}";
                // Albo: string key = plugin.Name + "." + property.Name;
                object? y;
                var x = configuration.TryGetValue(key, out y);
                if (x)
                {
                    try
                    {
                        property.SetValue(instance, y);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd ustawiania {key}: {ex.Message}");
                    }
                }
            }
            result.Add(instance);
        }

        return result;
        // TODO: Utwórz instancje pluginów i ustaw wartości właściwości używając .SetValue()
    }
}