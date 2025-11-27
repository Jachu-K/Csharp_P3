namespace Plugins;

// Plugin 1 - dziedziczy po BasePlugin i implementuje IProcessor<string>
[Plugin("TextProcessor", "1.0")]
public class TextProcessor : BasePlugin, IProcessor<string>
{
    [ConfigurableProperty("Prefix dodawany do tekstu")]
    public string Prefix { get; set; } = "PROCESSED: ";
    
    public override string PluginType => "Text";
    
    public override void Initialize()
    {
        Console.WriteLine("TextProcessor initialized");
    }
    
    public string Process(string input)
    {
        return Prefix + input.ToUpper();
    }
    
    public bool CanProcess(Type type) => type == typeof(string);
}

// Plugin 2 - tylko dziedziczy po BasePlugin
[Plugin("NumberPlugin", "2.0")]
public class NumberPlugin : BasePlugin
{
    [ConfigurableProperty("Mnożnik dla liczb")]
    public int Multiplier { get; set; } = 2;
    
    public override string PluginType => "Number";
    
    public override void Initialize()
    {
        Console.WriteLine("NumberPlugin initialized");
    }
}

// Plugin 3 - implementuje tylko IProcessor<int>
[Plugin("IntProcessor", "1.5")]
public class IntProcessor : IProcessor<int>
{
    [ConfigurableProperty("Wartość dodawana do liczby")]
    public int AddValue { get; set; } = 10;
    
    public int Process(int input)
    {
        return input + AddValue;
    }
    
    public bool CanProcess(Type type) => type == typeof(int);
}