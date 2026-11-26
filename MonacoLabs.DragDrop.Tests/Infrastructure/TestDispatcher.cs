using Avalonia.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MonacoLabs.DragDrop.Tests;

public static class TestDispatcher
{
    /// <summary>
    /// Runs dispatcher jobs and waits until <paramref name="condition"/> holds, or throws <see cref="TimeoutException"/> after
    /// <paramref name="timeout"/> (2 s by default).
    /// </summary>
    /// <remarks>Headless <see cref="DispatcherTimer"/>s fire only while the dispatcher waits, never from <see cref="Dispatcher.RunJobs()"/> alone.</remarks>
    public static async Task PumpUntil(Func<bool> condition, TimeSpan? timeout = null, [CallerArgumentExpression(nameof(condition))] string conditionExpression = "")
    {
        var limit = timeout ?? TimeSpan.FromSeconds(2);
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition())
                return;

            if (elapsed.Elapsed > limit)
                throw new TimeoutException($"{conditionExpression} did not hold within {limit.TotalMilliseconds} ms.");

            await Task.Delay(10);
        }
    }
}
