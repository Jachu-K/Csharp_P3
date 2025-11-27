// MiniTestRunner/Models/TestResult.cs
using System;
using System.Reflection;
using System.Runtime.Loader;
// Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MiniTest.Attributes;


public class TestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? FailureMessage { get; set; }
    public Exception? Exception { get; set; }
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
}

// MiniTestRunner/Models/TestClassResult.cs
public class TestClassResult
{
    public string ClassName { get; set; } = string.Empty;
    public List<TestResult> TestResults { get; set; } = new();
    public int TotalTests => TestResults.Count;
    public int PassedTests => TestResults.Count(r => r.Passed);
    public int FailedTests => TestResults.Count(r => !r.Passed);
}

// MiniTestRunner/Models/AssemblyResult.cs
public class AssemblyResult
{
    public string AssemblyName { get; set; } = string.Empty;
    public List<TestClassResult> ClassResults { get; set; } = new();
    public int TotalTests => ClassResults.Sum(c => c.TotalTests);
    public int PassedTests => ClassResults.Sum(c => c.PassedTests);
    public int FailedTests => ClassResults.Sum(c => c.FailedTests);
}

// MiniTestRunner/TestDiscoverer.cs
public class TestDiscoverer
{
    public List<TestClassInfo> DiscoverTests(Assembly assembly)
    {
        var testClasses = new List<TestClassInfo>();
        
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<TestClassAttribute>() == null)
                continue;
                
            // Sprawdź czy klasa ma konstruktor bezparametrowy
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"OSTRZEŻENIE: Klasa testowa {type.Name} nie ma konstruktora bezparametrowego - pominięta.");
                Console.ResetColor();
                continue;
            }
            
            var testClassInfo = new TestClassInfo
            {
                Type = type,
                Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description
            };
            
            DiscoverMethods(testClassInfo, type);
            testClasses.Add(testClassInfo);
        }
        
        return testClasses;
    }
    
    private void DiscoverMethods(TestClassInfo testClassInfo, Type type)
    {
        foreach (var method in type.GetMethods())
        {
            if (method.GetCustomAttribute<BeforeEachAttribute>() != null)
            {
                testClassInfo.BeforeEachMethod = method;
            }
            else if (method.GetCustomAttribute<AfterEachAttribute>() != null)
            {
                testClassInfo.AfterEachMethod = method;
            }
            else if (method.GetCustomAttribute<TestMethodAttribute>() != null)
            {
                DiscoverTestMethod(testClassInfo, method);
            }
        }
        
        // Sortuj testy według priorytetu i nazwy
        testClassInfo.TestMethods = testClassInfo.TestMethods
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Method.Name)
            .ToList();
    }
    
    private void DiscoverTestMethod(TestClassInfo testClassInfo, MethodInfo method)
    {
        var dataRows = method.GetCustomAttributes<DataRowAttribute>().ToList();
        
        if (dataRows.Any())
        {
            // Testy parametryzowane
            foreach (var dataRow in dataRows)
            {
                testClassInfo.TestMethods.Add(new TestMethodInfo
                {
                    Method = method,
                    Priority = method.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0,
                    Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    Data = dataRow.Data,
                    DisplayName = dataRow.DisplayName ?? $"({string.Join(", ", dataRow.Data)})"
                });
            }
        }
        else
        {
            // Zwykły test
            testClassInfo.TestMethods.Add(new TestMethodInfo
            {
                Method = method,
                Priority = method.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0,
                Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description
            });
        }
    }
}

// MiniTestRunner/TestExecutor.cs
public class TestExecutor
{
    public TestResult ExecuteTest(TestClassInfo testClass, TestMethodInfo testMethod)
    {
        var result = new TestResult 
        { 
            TestName = GetTestName(testMethod),
            Description = testMethod.Description
        };
        
        object? testInstance = null;
        var startTime = DateTime.Now;
        
        try
        {
            testInstance = Activator.CreateInstance(testClass.Type);
            
            // BeforeEach
            if (testClass.BeforeEachMethod != null)
            {
                testClass.BeforeEachMethod.Invoke(testInstance, null);
            }
            
            // Wykonaj test
            if (testMethod.Data != null)
            {
                testMethod.Method.Invoke(testInstance, testMethod.Data);
            }
            else
            {
                testMethod.Method.Invoke(testInstance, null);
            }
            
            result.Passed = true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is AssertionException)
        {
            result.Passed = false;
            result.FailureMessage = ex.InnerException.Message;
            result.Exception = ex.InnerException;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.FailureMessage = $"Nieoczekiwany wyjątek: {ex.InnerException?.Message ?? ex.Message}";
            result.Exception = ex.InnerException ?? ex;
        }
        finally
        {
            // AfterEach
            if (testInstance != null && testClass.AfterEachMethod != null)
            {
                try
                {
                    testClass.AfterEachMethod.Invoke(testInstance, null);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"OSTRZEŻENIE: AfterEach wyrzucił wyjątek: {ex.Message}");
                    Console.ResetColor();
                }
            }
            
            result.Duration = DateTime.Now - startTime;
        }
        
        return result;
    }
    
    private string GetTestName(TestMethodInfo testMethod)
    {
        var baseName = testMethod.Method.Name;
        return testMethod.DisplayName != null ? $"{baseName}{testMethod.DisplayName}" : baseName;
    }
}

// MiniTestRunner/Program.cs
class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Użycie: MiniTestRunner <ścieżka-do-assembly1> [ścieżka-do-assembly2 ...]");
            return 1;
        }
        
        var totalFailed = 0;
        var totalTests = 0;
        
        foreach (var assemblyPath in args)
        {
            if (!File.Exists(assemblyPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BŁĄD: Plik {assemblyPath} nie istnieje.");
                Console.ResetColor();
                continue;
            }
            
            try
            {
                var assemblyResult = RunTestsInAssembly(assemblyPath);
                totalTests += assemblyResult.TotalTests;
                totalFailed += assemblyResult.FailedTests;
                
                PrintAssemblySummary(assemblyResult);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BŁĄD podczas ładowania assembly {assemblyPath}: {ex.Message}");
                Console.ResetColor();
            }
        }
        
        PrintFinalSummary(totalTests, totalFailed);
        return totalFailed > 0 ? 1 : 0;
    }
    
    private static AssemblyResult RunTestsInAssembly(string assemblyPath)
    {
        var context = new AssemblyLoadContext(assemblyPath, isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        
        var discoverer = new TestDiscoverer();
        var executor = new TestExecutor();
        var testClasses = discoverer.DiscoverTests(assembly);
        
        var assemblyResult = new AssemblyResult { AssemblyName = Path.GetFileName(assemblyPath) };
        
        foreach (var testClass in testClasses)
        {
            var classResult = new TestClassResult { ClassName = testClass.Type.Name };
            
            if (!string.IsNullOrEmpty(testClass.Description))
            {
                Console.WriteLine($"\n📝 {testClass.Description}");
            }
            
            Console.WriteLine($"\n🏃 Uruchamianie testów w klasie: {testClass.Type.Name}");
            
            foreach (var testMethod in testClass.TestMethods)
            {
                var result = executor.ExecuteTest(testClass, testMethod);
                classResult.TestResults.Add(result);
                
                PrintTestResult(result);
            }
            
            PrintClassSummary(classResult);
            assemblyResult.ClassResults.Add(classResult);
        }
        
        context.Unload();
        return assemblyResult;
    }
    
    private static void PrintTestResult(TestResult result)
    {
        Console.ForegroundColor = result.Passed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write(result.Passed ? "  ✓ PASSED " : "  ✗ FAILED ");
        Console.ResetColor();
        
        Console.Write($"{result.TestName}");
        
        if (!string.IsNullOrEmpty(result.Description))
        {
            Console.Write($" - {result.Description}");
        }
        
        Console.WriteLine($" ({result.Duration.TotalMilliseconds:F2}ms)");
        
        if (!result.Passed && !string.IsNullOrEmpty(result.FailureMessage))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"      {result.FailureMessage}");
            Console.ResetColor();
        }
    }
    
    private static void PrintClassSummary(TestClassResult classResult)
    {
        Console.ForegroundColor = classResult.FailedTests == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"\n📊 Podsumowanie klasy {classResult.ClassName}:");
        Console.WriteLine($"   Testy: {classResult.PassedTests} passed, {classResult.FailedTests} failed, {classResult.TotalTests} total");
        Console.ResetColor();
    }
    
    private static void PrintAssemblySummary(AssemblyResult assemblyResult)
    {
        Console.ForegroundColor = assemblyResult.FailedTests == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"\n🎯 PODSUMOWANIE ASSEMBLY {assemblyResult.AssemblyName}:");
        Console.WriteLine($"   Testy: {assemblyResult.PassedTests} passed, {assemblyResult.FailedTests} failed, {assemblyResult.TotalTests} total");
        Console.ResetColor();
    }
    
    private static void PrintFinalSummary(int totalTests, int totalFailed)
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.ForegroundColor = totalFailed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"🏁 FINALNE PODSUMOWANIE:");
        Console.WriteLine($"   Testy: {totalTests - totalFailed} passed, {totalFailed} failed, {totalTests} total");
        Console.ResetColor();
    }
}