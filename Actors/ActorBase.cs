using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Actors;

public abstract class ActorBase<TId, TMessageType, TActorRef> : IActor<TId, TMessageType, TActorRef>
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
    where TActorRef : IActorRef<TId, TMessageType, TActorRef>, IEquatable<TActorRef>
{
    protected readonly ILogger _logger;
    private const string SendFailedLoggerTemplate = "Could not send message to actor {ActorId}: {ExceptionMessage}";
    private readonly Channel<TMessageType> _mailbox;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _messageProcessingTask;
    private long _disposed; // 1 for true, 0 for false ('long' allows Interlocked usage)

    public TActorRef Reference { get; }

    protected ActorBase(IActorRefFactory<TId, TMessageType, TActorRef> factory, ILogger logger)
    {
        _logger = logger;
        _mailbox = Channel.CreateUnbounded<TMessageType>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _cancellationTokenSource = new();
        _messageProcessingTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
        Reference = factory.Create(SendAsync);
    }

    protected abstract Task HandleMessageAsync(TMessageType message);
    protected abstract ValueTask DisposeActorAsync();

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (TMessageType message in _mailbox.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await HandleMessageAsync(message);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Actor {ActorId} encountered an error while processing a message: {ExceptionMessage}", Reference.Id, ex.Message);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Actor {ActorId} message processing was cancelled.", Reference.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Actor {ActorId} message processing loop terminated unexpectedly: {ExceptionMessage}", Reference.Id, ex.Message);
        }
    }

    private async ValueTask SendAsync(TMessageType message)
    {
        try
        {
            if (Interlocked.Read(ref _disposed) == 1)
            {
                _logger.LogDebug(SendFailedLoggerTemplate, Reference.Id, "Actor is disposed.");
                return;
            }

            await _mailbox.Writer.WriteAsync(message, _cancellationTokenSource.Token);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug(SendFailedLoggerTemplate, Reference.Id, "Actor is disposed.");
        }
        catch (OperationCanceledException cancelled)
        {
            _logger.LogDebug(SendFailedLoggerTemplate, Reference.Id, cancelled.Message);
        }
        catch (InvalidOperationException invalidOperation)
        {
            _logger.LogWarning(invalidOperation, SendFailedLoggerTemplate, Reference.Id, invalidOperation.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, SendFailedLoggerTemplate, Reference.Id, exception.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _logger.LogDebug("Disposing actor {ActorId}", Reference.Id);

        try
        {
            _mailbox.Writer.TryComplete();

            if (Equals(Task.CurrentId, _messageProcessingTask.Id))
            {
                _cancellationTokenSource.Cancel();
                return;
            }
            await _messageProcessingTask;
            await DisposeActorAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected, do nothing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while disposing actor {ActorId}: {ExceptionMessage}", Reference.Id, ex.Message);
        }
        finally
        {
            _cancellationTokenSource.Dispose();
            _logger.LogDebug("Disposed actor {ActorId} successfully.", Reference.Id);
            GC.SuppressFinalize(this);
        }
    }
}