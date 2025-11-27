using TaskManager;

class Program
{
    static void Main()
    {
        var dispatcher = new TaskDispatcher();
        
        Console.WriteLine("🎯 Odnalezione handlery:");
        dispatcher.PrintDiscoveredHandlers();
        
        Console.WriteLine("\n🚀 Wykonywanie zadań:");
        dispatcher.ExecuteTask("SendWelcome", "anna@example.com");
        dispatcher.ExecuteTask("SendNotification", "Nowa wersja systemu dostępna!");
        dispatcher.ExecuteTask("CreateBackup", @"C:\data\important.txt");
        dispatcher.ExecuteTask("CleanTemp", @"C:\temp");
        
        // To powinno wyrzucić wyjątek - nieznany typ zadania
        try
        {
            dispatcher.ExecuteTask("UnknownTask", "parametr");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Błąd: {ex.Message}");
        }
    }
}