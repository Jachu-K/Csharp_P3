namespace Plugins;

// STEP 1
    [AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    
    public PluginAttribute(string name, string version)
    {
        Name = name;
        Version = version;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ConfigurablePropertyAttribute : Attribute
{
    public string Description { get; }
    
    public ConfigurablePropertyAttribute(string description)
    {
        Description = description;
    }
}