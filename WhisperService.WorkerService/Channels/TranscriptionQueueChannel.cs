using System.Collections.Concurrent;
using System.Threading.Channels;
using WhisperService.WorkerService.Contracts;

namespace WhisperService.WorkerService.Channels;

public class TranscriptionQueueChannel
{
    private readonly Channel<TranscriptionJobContract> _channel;
    private readonly ConcurrentDictionary<string, byte> _activeKeys = new();

    public TranscriptionQueueChannel()
    {
        var options = new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait, 
            SingleReader = true,                  
            SingleWriter = true                  
        };
        
        _channel = Channel.CreateBounded<TranscriptionJobContract>(options);
    }

    public ChannelReader<TranscriptionJobContract> Reader => _channel.Reader;
    public async ValueTask<bool> WriteAsync(TranscriptionJobContract job, CancellationToken cancellationToken = default)
    {
        var key = job.AudioJob.FileKey;

        if (!_activeKeys.TryAdd(key, 0))
        {
            return false;
        }

        try
        {
            await _channel.Writer.WriteAsync(job, cancellationToken);
            return true;
        }
        catch
        {
            _activeKeys.TryRemove(key, out _);
            throw;
        }
    }

    public void CompleteProcessing(string key)
    {
        _activeKeys.TryRemove(key, out _);
    }
}
