using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrencySimulator
{
    class Program
    {
        // Potrzebne pola
        private static int _activeTasksCount = 0;
        private static readonly object _lockObject = new object();
        private static List<string> _results = new List<string>();
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Symulator przetwarzania danych ===");

            var cts = new CancellationTokenSource();
            // Timeout po 30 sekundach
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            
            // Obsługa anulowania przez użytkownika
            Console.WriteLine("Naciśnij 'q' aby anulować przetwarzanie...\n");
            var userCancelTask = Task.Run(() =>
            {
                while (true)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("\nAnulowanie na żądanie użytkownika...");
                        cts.Cancel();
                        break;
                    }
                    Thread.Sleep(100);
                }
            });
            
            // TODO 1: Uruchom wątek monitorujący system
            Thread monitorThread = new Thread(() => SystemMonitor(cts.Token));
            monitorThread.Start();
            
            // TODO 2: Uruchom równoległe przetwarzanie danych
            Console.WriteLine("\nRozpoczynam równoległe przetwarzanie...");
            ProcessDataInParallel(Enumerable.Range(1, 100).ToList(), cts.Token);
            Console.WriteLine($"Przetworzono {_results.Count} elementów równolegle");
            
            // TODO 3: Uruchom asynchroniczne operacje
            Console.WriteLine("\nRozpoczynam zadania asynchroniczne...");
            
            var tasks = new List<Task<string>>();
            tasks.Add(SimulateWorkAsync("Praca 1", 3000, cts.Token));
            tasks.Add(SimulateWorkAsync("Praca 2", 1500, cts.Token));
            tasks.Add(SimulateWorkAsync("Praca 3", 2000, cts.Token));
            tasks.Add(SimulateWorkAsync("Praca 4", 2500, cts.Token));
            tasks.Add(SimulateWorkAsync("Praca 5", 1000, cts.Token));
            
            // TODO 4: Użyj Task.WhenAll i Task.WhenAny
            // Pierwsze zakończone zadanie
            var firstCompleted = await Task.WhenAny(tasks);
            Console.WriteLine($"Pierwsze zakończone zadanie: {firstCompleted.Result}");
            
            // Wszystkie zadania
            var allResults = await Task.WhenAll(tasks);
            Console.WriteLine($"Wszystkie zadania zakończone! Wyniki: {string.Join(", ", allResults)}");
            
            Console.WriteLine("\nNaciśnij Enter, aby zatrzymać symulację...");
            Console.ReadLine();
            
            cts.Cancel();
            
            // TODO: Zaczekaj na zakończenie wątku monitorującego
            monitorThread.Join(2000); // Czekaj maksymalnie 2 sekundy
            
            Console.WriteLine("Symulacja zakończona.");
        }
        
        // Metoda dla wątku monitorującego
        private static void SystemMonitor(CancellationToken token)
        {
            int iteration = 0;
            while (!token.IsCancellationRequested)
            {
                iteration++;
                lock (_lockObject)
                {
                    Console.WriteLine($"[Monitor] Iteracja: {iteration}, Aktywne taski: {_activeTasksCount}, Wyników: {_results.Count}");
                }
                
                // Symuluj monitorowanie - użyj Wait zamiast Sleep dla lepszej responsywności
                try
                {
                    Task.Delay(2000, token).Wait(token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[Monitor] Zatrzymywanie monitora...");
                    break;
                }
            }
        }
        
        // Metoda przetwarzania równoległego
        private static void ProcessDataInParallel(List<int> data, CancellationToken token)
        {
            var parallelOptions = new ParallelOptions 
            { 
                CancellationToken = token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            
            try
            {
                Parallel.ForEach(data, parallelOptions, item =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    
                    // Symulacja przetwarzania
                    int result;
                    if (item % 2 == 0)
                    {
                        result = item * item; // Kwadrat dla parzystych
                    }
                    else
                    {
                        result = item * 3; // Pomnóż przez 3 dla nieparzystych
                    }
                    
                    // Bezpieczny dostęp do współdzielonej listy
                    lock (_lockObject)
                    {
                        _results.Add($"{item}->{result}");
                    }
                    
                    // Symulacja pracy
                    Thread.Sleep(10);
                });
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Przetwarzanie równoległe anulowane.");
            }
        }
        
        // Asynchroniczne metody symulujące prace
        private static async Task<string> SimulateWorkAsync(string workName, int delay, CancellationToken token)
        {
            lock (_lockObject)
            {
                _activeTasksCount++;
            }
            
            try
            {
                Console.WriteLine($"Zadanie '{workName}' rozpoczęte (czas: {delay}ms)");
                await Task.Delay(delay, token);
                
                return $"'{workName}' ({delay}ms)";
            }
            catch (OperationCanceledException)
            {
                return $"'{workName}' ANULOWANE";
            }
            finally
            {
                lock (_lockObject)
                {
                    _activeTasksCount--;
                }
            }
        }
    }
}