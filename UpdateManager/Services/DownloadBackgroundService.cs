namespace UpdateManager.Services;

public class DownloadBackgroundService : BackgroundService

{
    private readonly ILogger<DownloadBackgroundService> _logger;
    private readonly DownloadQueueSingleston _taskQueue;

    public DownloadBackgroundService(ILogger<DownloadBackgroundService> logger, DownloadQueueSingleston taskQueue)
    {
        _logger = logger;
        _taskQueue = taskQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _taskQueue.DequeueAsync(stoppingToken);
                _taskQueue.Status = "Downloading";
                _taskQueue.PercentComplete = 0;
                task.Invoke();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading in background task");
            }
        }
    }
}