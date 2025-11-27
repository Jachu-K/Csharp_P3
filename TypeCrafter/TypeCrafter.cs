using System.Reflection;

namespace TypeCrafter;

public class ParseException : Exception
{
    public ParseException() { }

    public ParseException(string message) 
        : base(message) { }

    public ParseException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public static class TypeCrafter
{
    public static T CraftInstance<T>()
    {
        Type t_type = typeof(T);
        var constructors = t_type.GetConstructor(Type.EmptyTypes);
        if (constructors == null)
        {
            throw new InvalidOperationException("Brak bezparametrowego konstruktora");
        }
        var newObject = (T)constructors.Invoke(null);
        var properties = t_type.GetProperties();

        foreach (var property in properties)
        {
            var ipars = typeof(IParsable<>);
            var isParsable = property.PropertyType.GetInterfaces()
                .Any(t => t.IsGenericType && t.GetGenericArguments()[0] == property.PropertyType);
            if (property.PropertyType == typeof(string))
            {
                Console.WriteLine($"Proba wczytania stringa dla {property.PropertyType}");
                string s = Console.ReadLine();
                property.SetValue(newObject,s);
            }else if (isParsable)
            {
                Console.WriteLine($"Proba tryparse dla {property.PropertyType}");
                string s = Console.ReadLine();
                var parseMethod = property.PropertyType
                    .GetMethod(
                        nameof(int.TryParse),
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: [typeof(string), typeof(IFormatProvider), property.PropertyType.MakeByRefType()],
                        modifiers: null);
                if (parseMethod == null)
                {
                    throw new InvalidOperationException("Nie ma metody TryParse");
                }

                var args = new object[] { s, null!, null! };

                if (parseMethod.Invoke(null, args) is bool status && status)
                {
                    property.SetValue(newObject, args[2]);
                }
                else 
                {
                    throw new ParseException($"Could not parse {s} to {property.PropertyType}.");
                }
            }
            else
            {
                Console.WriteLine($"Proba wywolania rekurencyjnego dla {property.PropertyType}");
                
                var craftMethod = typeof(TypeCrafter).GetMethod(nameof(CraftInstance), BindingFlags.Public | BindingFlags.Static);
                var genericMethod = craftMethod?.MakeGenericMethod(property.PropertyType);
                var complexProperty = genericMethod?.Invoke(null, null);
                
                property.SetValue(newObject, complexProperty);
            }
        }

        return newObject;
    }
}