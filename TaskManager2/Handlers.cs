namespace TaskManager;

[TaskHandler("Email")]
public class EmailHandler
{
    [HandlesTask("SendWelcome")]
    [Priority(1)]
    public void HandleWelcomeEmail(string recipient)
    {
        Console.WriteLine($"📧 Wysyłam powitalny email do: {recipient}");
    }
    
    [HandlesTask("SendNotification")]
    [Priority(2)]
    public void HandleNotification(string message)
    {
        Console.WriteLine($"🔔 Powiadomienie: {message}");
    }
}

[TaskHandler("File")]
public class FileHandler
{
    [HandlesTask("CreateBackup")]
    [Priority(1)]
    public void CreateBackup(string filePath)
    {
        Console.WriteLine($"💾 Tworzę backup pliku: {filePath}");
    }
    
    [HandlesTask("CleanTemp")]
    [Priority(3)]
    public void CleanTempFiles(string directory)
    {
        Console.WriteLine($"🧹 Czyszczę pliki tymczasowe w: {directory}");
    }
}