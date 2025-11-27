using System.Reflection;

namespace TaskManager;

public class TaskDispatcher
{
    private Dictionary<string, List<(MethodInfo method, object instance, int priority)>> _handlers;
    
    public TaskDispatcher()
    {
        _handlers = new Dictionary<string, List<(MethodInfo, object, int)>>();
        DiscoverHandlers();
    }
    
    private void DiscoverHandlers()
    {
        var classes = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var klasa in classes)
        {
            // NAJPIERW sprawdź atrybut - tylko klasy z TaskHandlerAttribute nas interesują!
            var taskHandlerAttr = klasa.GetCustomAttribute<TaskHandlerAttribute>();
            if (taskHandlerAttr is null) continue; // ← Pomijaj klasy bez atrybutu
        
            // DOPIERO TERAZ sprawdź konstruktor (tylko dla handlerów)
            var konstruktor = klasa.GetConstructor(Type.EmptyTypes);
            if (konstruktor is null)
            {
                // ZMIANA: Nie rzucaj wyjątku, tylko wypisz ostrzeżenie i kontynuuj
                Console.WriteLine($"⚠️ Klasa {klasa.Name} nie ma konstruktora bezparametrowego - pomijam");
                continue;
            }
        
            var instancja = konstruktor.Invoke(null);
        
            // Poprawiony atrybut na metodach
            var metody = klasa.GetMethods().Where(x => x.GetCustomAttribute<HandlesTaskAttribute>() != null);
        
            foreach (var metoda in metody)
            {
                var handlesTaskAttr = metoda.GetCustomAttribute<HandlesTaskAttribute>();
                var priority = metoda.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0;
            
                string taskType = handlesTaskAttr.TaskType;
            
                if (!_handlers.ContainsKey(taskType))
                {
                    _handlers[taskType] = new List<(MethodInfo method, object instance, int priority)>();
                }
            
                _handlers[taskType].Add((metoda, instancja, priority));
            }
        }
    
        // Sortowanie
        foreach (var taskType in _handlers.Keys)
        {
            _handlers[taskType] = _handlers[taskType]
                .OrderBy(h => h.priority)
                .ToList();
        }
        
        // TODO: Użyj refleksji aby:
        // 1. Znaleźć wszystkie klasy z TaskHandlerAttribute w obecnym assembly
        // 2. Dla każdej klasy stworzyć instancję
        // 3. Znaleźć metody z HandlesTaskAttribute
        // 4. Dla każdej metody pobrać TaskType i Priority
        // 5. Dodać do słownika _handlers pod kluczem TaskType
    }
    
    public void ExecuteTask(string taskType, params object[] parameters)
    {
        if (!_handlers.TryGetValue(taskType, out var handlers) || handlers == null || !handlers.Any())
        {
            throw new InvalidOperationException($"Nie znaleziono handlera dla typu zadania: {taskType}");
        }
    
        try
        {
            var handler = handlers.First();
            handler.method.Invoke(handler.instance, parameters);
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException($"Błąd podczas wykonywania zadania {taskType}: {ex.InnerException?.Message}");
        }
    }
    
    public void PrintDiscoveredHandlers()
    {
        foreach (var item in _handlers)
        {
            Console.WriteLine($"Kategoria: {item.Key}");
            foreach (var lista in item.Value)
            {
                Console.WriteLine($"  -  {lista.method} (Priorytet: {lista.priority}) -> {lista.instance}");
            }
        }
        // TODO: Wyświetl wszystkie odnalezione handlery w formacie:
        // Kategoria: Email
        //   - SendWelcome (Priority: 1) → EmailHandler.HandleWelcomeEmail
        //   - SendNotification (Priority: 2) → EmailHandler.HandleNotification
    }
}