namespace Plugins;

public interface IProcessor<T>
{
    T Process(T input);
    bool CanProcess(Type type);
}

public abstract class BasePlugin
{
    public abstract string PluginType { get; }
    public abstract void Initialize();
}