using System.Collections.Concurrent;

namespace UpdateManager.Services;

public class DownloadQueueSingleston
{
    private readonly ConcurrentQueue<Action> _tasks = new ConcurrentQueue<Action>();
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, 1);
    public int PercentComplete { get; set; } = 0;
    public void QueueTask(Action task)
    {
        _tasks.Enqueue(task);
        _signal.Release();
    }

    public async Task<Action> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        if (_tasks.TryDequeue(out Action task))
        {
            return task;
        }
        throw new InvalidOperationException("No tasks available in the queue.");
    }
}