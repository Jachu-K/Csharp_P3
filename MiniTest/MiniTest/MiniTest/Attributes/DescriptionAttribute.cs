// MiniTest/Attributes/DescriptionAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DescriptionAttribute : Attribute
{
    public string Description { get; }
    public DescriptionAttribute(string description) => Description = description;
}