// MiniTest/Assertions/Assert.cs
public static class Assert
{
    public static void ThrowsException<TException>(Action action, string message = "") 
        where TException : Exception
    {
        try
        {
            action();
            throw new AssertionException(
                $"Oczekiwano wyjątku typu <{typeof(TException)}> ale żaden wyjątek nie został wyrzucony. {message}");
        }
        catch (TException)
        {
            // Test passed - oczekiwany wyjątek
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Oczekiwano wyjątku typu <{typeof(TException)}> ale otrzymano <{ex.GetType()}>. {message}");
        }
    }
    
    public static void AreEqual<T>(T? expected, T? actual, string message = "")
    {
        if (!Equals(expected, actual))
        {
            throw new AssertionException(
                $"Oczekiwano: {expected}. Otrzymano: {actual}. {message}");
        }
    }
    
    public static void AreNotEqual<T>(T? notExpected, T? actual, string message = "")
    {
        if (Equals(notExpected, actual))
        {
            throw new AssertionException(
                $"Oczekiwano dowolnej wartości poza: {notExpected}. Otrzymano: {actual}. {message}");
        }
    }
    
    public static void IsTrue(bool condition, string message = "")
    {
        if (!condition)
        {
            throw new AssertionException(
                $"Warunek powinien być prawdziwy. {message}");
        }
    }
    
    public static void IsFalse(bool condition, string message = "")
    {
        if (condition)
        {
            throw new AssertionException(
                $"Warunek powinien być fałszywy. {message}");
        }
    }
    
    public static void Fail(string message = "")
    {
        throw new AssertionException($"Test oznaczony jako nieudany. {message}");
    }
}