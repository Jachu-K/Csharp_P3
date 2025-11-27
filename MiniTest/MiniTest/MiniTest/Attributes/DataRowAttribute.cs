// MiniTest/Attributes/DataRowAttribute.cs
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DataRowAttribute : Attribute
{
    public object?[] Data { get; }
    public string? DisplayName { get; }
    
    public DataRowAttribute(params object?[] data) => Data = data;
    public DataRowAttribute(string displayName, params object?[] data)
    {
        DisplayName = displayName;
        Data = data;
    }
}