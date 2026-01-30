using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sync;
class Program
{
    static readonly object LockObj = new object();
    static SemaphoreSlim WaitingHall = new SemaphoreSlim(2, 2);
    static Barrier SyncBarrier = new Barrier(3);
    static ManualResetEventSlim canProceed = new ManualResetEventSlim(false);
    static int howManyTimes = 2;
    
    static void Main()
    {
        lock (LockObj)
        {
            //costam
        }
        
        Task[] Waiters = new Task[4];
        for (int i = 0; i < 4; i++)
        {
            int id = i + 1;
            Waiters[i] = Task.Run(() => Waiter(id));
        }

        Task[] Sleepers = new Task[2];
        for (int i = 0; i < 2; i++)
        {
            int id = i + 1;
            Sleepers[i] = Task.Run(() => Sleeper(id));
        }

        Task.Run(() => SyncSleep());

        Task.WaitAll(Waiters);
    }

    static void Waiter(int id)
    {
        WaitingHall.Wait();
        try
        {
            Thread.Sleep(1000);
        }
        finally
        {
            WaitingHall.Release();
        }
        canProceed.Wait();

    }

    static void Sleeper(int id)
    {
        for (int i = 1; i <= howManyTimes; i++)
        {
            Thread.Sleep(500);
            
            SyncBarrier.SignalAndWait();
            
            Thread.Sleep(300);
        }
    }

    static void SyncSleep()
    {
        for (int i = 1; i <= howManyTimes; i++)
        {
            SyncBarrier.SignalAndWait();
            
            Thread.Sleep(500);
            
            if (i == howManyTimes)
            {
                canProceed.Set();
            }
        }
    }
}