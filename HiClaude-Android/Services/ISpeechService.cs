namespace ClaudeCodeAndroid.Services;

public interface ISpeechService
{
    bool IsListening { get; }
    event EventHandler<string>? SpeechRecognized;
    event EventHandler<string>? SpeechError;
    event EventHandler? ListeningStarted;
    event EventHandler? ListeningStopped;
    Task<bool> StartListeningAsync();
    Task StopListeningAsync();
}

public class SpeechService : ISpeechService
{
    private bool _isListening;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsListening => _isListening;

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler<string>? SpeechError;
    public event EventHandler? ListeningStarted;
    public event EventHandler? ListeningStopped;

    public async Task<bool> StartListeningAsync()
    {
        if (_isListening)
            return false;

        try
        {
            // Check for microphone permission
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    SpeechError?.Invoke(this, "Microphone permission denied");
                    return false;
                }
            }

            // Check for speech recognition permission
            var speechStatus = await Permissions.CheckStatusAsync<Permissions.Speech>();
            if (speechStatus != PermissionStatus.Granted)
            {
                speechStatus = await Permissions.RequestAsync<Permissions.Speech>();
                if (speechStatus != PermissionStatus.Granted)
                {
                    SpeechError?.Invoke(this, "Speech recognition permission denied");
                    return false;
                }
            }

            _isListening = true;
            _cancellationTokenSource = new CancellationTokenSource();
            ListeningStarted?.Invoke(this, EventArgs.Empty);

            // Use MAUI speech-to-text (requires platform-specific implementation)
            await StartPlatformSpeechRecognitionAsync(_cancellationTokenSource.Token);

            return true;
        }
        catch (Exception ex)
        {
            _isListening = false;
            SpeechError?.Invoke(this, ex.Message);
            return false;
        }
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening)
            return;

        _cancellationTokenSource?.Cancel();
        _isListening = false;
        ListeningStopped?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    private async Task StartPlatformSpeechRecognitionAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Using Android's SpeechRecognizer through MAUI essentials
            var locales = await TextToSpeech.Default.GetLocalesAsync();

            // Note: For full speech-to-text, you need the Community Toolkit Speech-to-Text
            // or platform-specific implementation using Android.Speech.SpeechRecognizer
            // This is a simplified version that shows the pattern

            // Simulate listening for demo purposes
            // In production, use Plugin.Maui.Audio or CommunityToolkit.Maui.Media
            await Task.Delay(5000, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                // In real implementation, this would come from the speech recognizer
                SpeechRecognized?.Invoke(this, "Speech recognition result would appear here");
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled - this is expected
        }
        catch (Exception ex)
        {
            SpeechError?.Invoke(this, $"Speech recognition failed: {ex.Message}");
        }
        finally
        {
            _isListening = false;
            ListeningStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
