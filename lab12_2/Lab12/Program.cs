using System.Text.Json;
using HttpSimulator;

namespace Lab12;

internal class Program
{
    static async Task Main(string[] args)
    {
        
        var cities = new[] { "New York", "London", "Tokyo", "Sydney", "Berlin" };
        var tasks = new List<Task>();

        // TODO: Change this code and make it fully asynchronous
        //       See Data.cs for useful data structures
        //       MockWeatherEndpoint.cs and fix bug in ForecastEndpoint method
        List<ProcessedCityWeather> lista = new List<ProcessedCityWeather>();
        foreach (var city in cities)
        {
            
            tasks.Add(ProcessAndAverageAsync(city, lista));
            
        }
        await Task.WhenAll(tasks);
        var maks = lista.OrderBy(item => (-1)*item.AverageTemperature).First();
        Console.WriteLine($"Miasto : {maks.City} ma najwięszką średnią temperaturę : {maks.AverageTemperature}");
    }

    public static async Task ProcessAsync(string city, List<ProcessedCityWeather> lista)
    {
        var client = await newClient();  // Teraz to jest poprawne
        var apiUrl = $"https://127.0.0.1:2137/api/v13/forecast?city={city}&daily=temperature";
        
        // Użyj await zamiast .Result
        string response = await client.GetStringAsync(apiUrl);
        WeatherApiResponse? pogoda = JsonSerializer.Deserialize<WeatherApiResponse>(response);
        if (pogoda != null)
        {
            foreach (var x in pogoda.Daily.Temperature)
            {
                Console.Write(x);
                Console.Write(' ');
            }
        }
        Console.Write('\n');
    }
    public static async Task ProcessAndAverageAsync(string city, List<ProcessedCityWeather> lista)
    {
        var lockObj = new Lock();
        var client = await newClient();  // Teraz to jest poprawne
        var apiUrl = $"https://127.0.0.1:2137/api/v13/forecast?city={city}&daily=temperature";
        
        // Użyj await zamiast .Result
        string response = await client.GetStringAsync(apiUrl);
        WeatherApiResponse? pogoda = JsonSerializer.Deserialize<WeatherApiResponse>(response);
        ProcessedCityWeather wyn = new ProcessedCityWeather();
        wyn.ExtremeWeatherDays = new List<string>();
        wyn.City = city;
        double average = 0;
        int count = 0;
        
        if (pogoda != null)
        {
            Parallel.ForEach(pogoda.Daily.Temperature, (double i) =>
            {
                average += i;
                count++;
                if (i > 30 || i < 0)
                {
                    wyn.ExtremeWeatherDays.Add(count.ToString());
                }
            });
            average /= count;
            wyn.AverageTemperature = average;
        }

        lock (lockObj)
        {
            lista.Add(wyn);
        }
        Console.WriteLine($"Average : {wyn.AverageTemperature}, Extreme : {wyn.ExtremeWeatherDays.Count}, in City : {wyn.City}");
    }
    public static async Task<HttpClient> newClient()
    {
        return new HttpClient(MockHttpMessageHandlerSingleton.Instance);
    }
}
/*
using System.Text.Json;
   using HttpSimulator;
   
   namespace Lab12;
   
   internal class Program
   {
       private static readonly HttpClient _httpClient = new HttpClient(MockHttpMessageHandlerSingleton.Instance)
       {
           BaseAddress = new Uri("https://127.0.0.1:2137/api/v13/")
       };
   
       static async Task Main(string[] args)
       {
           var cities = new[] { "New York", "London", "Tokyo", "Sydney", "Berlin" };
           
           try
           {
               // Zadanie 1 i 2: Pobierz i przetwarzaj dane asynchronicznie
               var processingTasks = cities.Select(ProcessCityWeatherAsync);
               var results = await Task.WhenAll(processingTasks);
               
               // Zadanie 3: Znajdź miasto z najwyższą średnią temperaturą
               var cityWithHighestAvg = results
                   .Where(r => r != null)
                   .OrderByDescending(r => r.AverageTemperature)
                   .FirstOrDefault();
               
               if (cityWithHighestAvg != null)
               {
                   Console.WriteLine($"Miasto: {cityWithHighestAvg.City} ma najwyższą średnią temperaturę: {cityWithHighestAvg.AverageTemperature:F2}°C");
                   
                   // Wyświetl dni z ekstremalną temperaturą
                   Console.WriteLine($"Dni z ekstremalną temperaturą ({cityWithHighestAvg.ExtremeWeatherDays.Count}):");
                   foreach (var day in cityWithHighestAvg.ExtremeWeatherDays)
                   {
                       Console.WriteLine($"  - Dzień {day}");
                   }
               }
           }
           catch (Exception ex)
           {
               Console.WriteLine($"Wystąpił błąd: {ex.Message}");
           }
       }
   
       public static async Task<ProcessedCityWeather?> ProcessCityWeatherAsync(string city)
       {
           try
           {
               // Zadanie 1: Pobierz dane asynchronicznie
               var apiUrl = $"forecast?city={Uri.EscapeDataString(city)}&daily=temperature";
               var response = await _httpClient.GetStringAsync(apiUrl);
               
               // Deserializuj odpowiedź
               var weatherData = JsonSerializer.Deserialize<WeatherApiResponse>(response);
               
               if (weatherData?.Daily?.Temperature == null || !weatherData.Daily.Temperature.Any())
               {
                   Console.WriteLine($"Brak danych temperatury dla miasta: {city}");
                   return null;
               }
   
               // Zadanie 2: Oblicz średnią temperaturę
               var temperatures = weatherData.Daily.Temperature;
               var averageTemp = temperatures.Average();
               
               // Zadanie 3: Znajdź dni z ekstremalną temperaturą
               var extremeDays = new List<string>();
               for (int i = 0; i < temperatures.Count; i++)
               {
                   if (temperatures[i] > 30 || temperatures[i] < 0)
                   {
                       extremeDays.Add($"Dzień {i + 1}: {temperatures[i]}°C");
                   }
               }
   
               var result = new ProcessedCityWeather
               {
                   City = city,
                   AverageTemperature = averageTemp,
                   ExtremeWeatherDays = extremeDays,
                   TotalDays = temperatures.Count
               };
   
               Console.WriteLine($"Przetworzono {city}: Średnia = {averageTemp:F2}°C, " +
                               $"Ekstremalne dni = {extremeDays.Count}/{temperatures.Count}");
               
               return result;
           }
           catch (HttpRequestException ex)
           {
               Console.WriteLine($"Błąd sieci dla miasta {city}: {ex.Message}");
               return null;
           }
           catch (JsonException ex)
           {
               Console.WriteLine($"Błąd parsowania JSON dla miasta {city}: {ex.Message}");
               return null;
           }
           catch (Exception ex)
           {
               Console.WriteLine($"Nieoczekiwany błąd dla miasta {city}: {ex.Message}");
               return null;
           }
       }
   }
   */