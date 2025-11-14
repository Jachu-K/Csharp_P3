using System.Globalization;

namespace ShopEvents;

/// <summary>
/// Main program to run the shop simulation.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var product = new Product { Name = "Laptop", Price = 1199.90m };
        var notifier = new Notifier();
        var product2 = new Product { Name = "PC", Price = 299.90m };
        var notifier2 = new Notifier();
        product.PriceChanged += notifier.HandlePriceChanged;
        product2.PriceChanged += notifier2.HandlePriceChanged;

        product.Price -= 200.0m;
        product2.Price += 150.0m;

        // Unsubscribing, to avoid memory leaks:
        product.PriceChanged -= notifier.HandlePriceChanged;
        product2.PriceChanged -= notifier2.HandlePriceChanged;
    }
}
