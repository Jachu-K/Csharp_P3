namespace TaskManager;

// Oznacza klasę jako handler zadań
[AttributeUsage(AttributeTargets.Class)]
public class TaskHandlerAttribute : Attribute
{
    public string Category { get; }
    public TaskHandlerAttribute(string category) => Category = category;
}

// Oznacza metodę jako obsługę konkretnego typu zadania
[AttributeUsage(AttributeTargets.Method)]
public class HandlesTaskAttribute : Attribute
{
    public string TaskType { get; }
    public HandlesTaskAttribute(string taskType) => TaskType = taskType;
}

// Określa priorytet wykonania (niższa liczba = wyższy priorytet)
[AttributeUsage(AttributeTargets.Method)]
public class PriorityAttribute : Attribute
{
    public int Priority { get; }
    public PriorityAttribute(int priority) => Priority = priority;
}