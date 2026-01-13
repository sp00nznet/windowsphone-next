using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Dsp;

namespace WindowsPhoneNext.Music;

public partial class MainWindow : Window
{
    // Audio playback
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _visualizerTimer;
    private bool _isDraggingSlider;

    // Visualizer
    private const int SpectrumBars = 64;
    private const int FftLength = 2048;
    private readonly float[] _spectrumData = new float[SpectrumBars];
    private readonly float[] _peakData = new float[SpectrumBars];
    private readonly int[] _peakHold = new int[SpectrumBars];
    private readonly float[] _fftBuffer = new float[FftLength];
    private readonly Complex[] _fftComplex = new Complex[FftLength];
    private SampleAggregator? _sampleAggregator;

    // Player state
    private bool _isPlaying;
    private bool _isShuffle;
    private int _repeatMode; // 0=off, 1=all, 2=one

    public MainWindow()
    {
        InitializeComponent();

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        _visualizerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
        };
        _visualizerTimer.Tick += VisualizerTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize visualizer with empty bars
        InitializeVisualizer();
        _visualizerTimer.Start();
    }

    private void InitializeVisualizer()
    {
        SpectrumCanvas.Children.Clear();

        double barWidth = (SpectrumCanvas.ActualWidth - (SpectrumBars - 1) * 2) / SpectrumBars;
        if (barWidth < 1) barWidth = 8;

        for (int i = 0; i < SpectrumBars; i++)
        {
            // Main bar
            var bar = new Rectangle
            {
                Width = barWidth,
                Height = 0,
                Fill = CreateBarGradient(),
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(bar, i * (barWidth + 2));
            Canvas.SetBottom(bar, 0);
            SpectrumCanvas.Children.Add(bar);

            // Peak indicator
            var peak = new Rectangle
            {
                Width = barWidth,
                Height = 3,
                Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7))
            };
            Canvas.SetLeft(peak, i * (barWidth + 2));
            Canvas.SetBottom(peak, 0);
            SpectrumCanvas.Children.Add(peak);
        }
    }

    private LinearGradientBrush CreateBarGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0.5, 1),
            EndPoint = new Point(0.5, 0),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(0, 60, 130), 0),
                new GradientStop(Color.FromRgb(0, 120, 212), 0.5),
                new GradientStop(Color.FromRgb(0, 200, 255), 1)
            }
        };
    }

    private void VisualizerTimer_Tick(object? sender, EventArgs e)
    {
        if (SpectrumCanvas.Children.Count < SpectrumBars * 2) return;

        double maxHeight = SpectrumCanvas.ActualHeight - 20;
        if (maxHeight < 10) return;

        for (int i = 0; i < SpectrumBars; i++)
        {
            // Get current spectrum value
            float targetValue = _spectrumData[i];

            // Smooth decay
            if (targetValue < _spectrumData[i])
            {
                _spectrumData[i] = Math.Max(0, _spectrumData[i] - 0.05f);
            }

            // Update bar
            if (SpectrumCanvas.Children[i * 2] is Rectangle bar)
            {
                double height = _spectrumData[i] * maxHeight;
                bar.Height = Math.Max(0, height);
            }

            // Update peak
            if (_spectrumData[i] > _peakData[i])
            {
                _peakData[i] = _spectrumData[i];
                _peakHold[i] = 30; // Hold for 30 frames
            }
            else if (_peakHold[i] > 0)
            {
                _peakHold[i]--;
            }
            else
            {
                _peakData[i] = Math.Max(0, _peakData[i] - 0.02f);
            }

            if (SpectrumCanvas.Children[i * 2 + 1] is Rectangle peak)
            {
                Canvas.SetBottom(peak, _peakData[i] * maxHeight);
            }
        }

        // Draw oscilloscope
        DrawOscilloscope();
    }

    private void DrawOscilloscope()
    {
        OscilloscopeCanvas.Children.Clear();

        if (!_isPlaying || _sampleAggregator == null) return;

        double width = OscilloscopeCanvas.ActualWidth;
        double height = 100;
        double centerY = 60;

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromArgb(120, 0, 200, 255)),
            StrokeThickness = 1.5
        };

        // Simple sine wave simulation when playing
        for (int i = 0; i < width; i++)
        {
            double x = i;
            double sample = Math.Sin(i * 0.05 + DateTime.Now.Ticks / 1000000.0) *
                           _spectrumData[Math.Min(i / 10, SpectrumBars - 1)] * 0.5;
            double y = centerY + sample * height / 2;
            polyline.Points.Add(new Point(x, y));
        }

        OscilloscopeCanvas.Children.Add(polyline);
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_audioFile != null && !_isDraggingSlider)
        {
            ProgressSlider.Value = _audioFile.CurrentTime.TotalSeconds /
                                   _audioFile.TotalTime.TotalSeconds * 100;
            CurrentTimeText.Text = FormatTime(_audioFile.CurrentTime);
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio Files|*.mp3;*.wav;*.wma;*.aac;*.m4a;*.flac;*.ogg|All Files|*.*",
            Title = "Select Audio File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadAndPlay(dialog.FileName);
        }
    }

    private void LoadAndPlay(string filePath)
    {
        try
        {
            StopPlayback();

            _audioFile = new AudioFileReader(filePath);
            _sampleAggregator = new SampleAggregator(_audioFile, FftLength);
            _sampleAggregator.FftCalculated += SampleAggregator_FftCalculated;

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_sampleAggregator);
            _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            _waveOut.Play();

            _isPlaying = true;
            PlayPauseIcon.Text = "\u23F8"; // Pause icon
            _updateTimer.Start();

            // Update track info
            TrackTitleText.Text = System.IO.Path.GetFileNameWithoutExtension(filePath);
            ArtistText.Text = "Now Playing";
            TotalTimeText.Text = FormatTime(_audioFile.TotalTime);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SampleAggregator_FftCalculated(object? sender, FftEventArgs e)
    {
        // Convert FFT data to spectrum bars
        int samplesPerBar = e.Result.Length / 2 / SpectrumBars;

        for (int i = 0; i < SpectrumBars; i++)
        {
            float sum = 0;
            int startIndex = i * samplesPerBar;

            for (int j = 0; j < samplesPerBar && startIndex + j < e.Result.Length / 2; j++)
            {
                var c = e.Result[startIndex + j];
                float magnitude = (float)Math.Sqrt(c.X * c.X + c.Y * c.Y);
                sum += magnitude;
            }

            // Apply frequency weighting and scale
            float weight = 1.0f + (float)i / SpectrumBars * 1.5f;
            float value = (sum / samplesPerBar) * weight * 10;
            value = Math.Min(1.0f, value);

            // Smooth transition
            if (value > _spectrumData[i])
                _spectrumData[i] = value;
        }
    }

    private void WaveOut_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_repeatMode == 2 && _audioFile != null) // Repeat one
            {
                _audioFile.Position = 0;
                _waveOut?.Play();
            }
            else
            {
                _isPlaying = false;
                PlayPauseIcon.Text = "\u25B6"; // Play icon

                // Clear visualizer
                for (int i = 0; i < SpectrumBars; i++)
                {
                    _spectrumData[i] = 0;
                    _peakData[i] = 0;
                }
            }
        });
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_waveOut == null)
        {
            // No file loaded, open file dialog
            OpenFileButton_Click(sender, e);
            return;
        }

        if (_isPlaying)
        {
            _waveOut.Pause();
            _isPlaying = false;
            PlayPauseIcon.Text = "\u25B6";
        }
        else
        {
            _waveOut.Play();
            _isPlaying = true;
            PlayPauseIcon.Text = "\u23F8";
        }
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFile != null)
        {
            _audioFile.Position = 0;
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        // In a full implementation, this would load the next track
        StopPlayback();
    }

    private void ShuffleButton_Click(object sender, RoutedEventArgs e)
    {
        _isShuffle = !_isShuffle;
        ShuffleButton.Opacity = _isShuffle ? 1.0 : 0.5;
    }

    private void RepeatButton_Click(object sender, RoutedEventArgs e)
    {
        _repeatMode = (_repeatMode + 1) % 3;
        RepeatButton.Opacity = _repeatMode == 0 ? 0.5 : 1.0;
    }

    private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = true;
    }

    private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = false;
        if (_audioFile != null)
        {
            var newPosition = TimeSpan.FromSeconds(
                ProgressSlider.Value / 100 * _audioFile.TotalTime.TotalSeconds);
            _audioFile.CurrentTime = newPosition;
        }
    }

    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Updated via PreviewMouseUp for seeking
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        StopPlayback();
        Close();
    }

    private void StopPlayback()
    {
        _updateTimer.Stop();
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _audioFile?.Dispose();
        _audioFile = null;
        _isPlaying = false;
        PlayPauseIcon.Text = "\u25B6";
    }

    protected override void OnClosed(EventArgs e)
    {
        StopPlayback();
        _visualizerTimer.Stop();
        base.OnClosed(e);
    }
}

// Sample aggregator for FFT analysis
public class SampleAggregator : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _fftLength;
    private readonly Complex[] _fftBuffer;
    private int _fftPosition;

    public event EventHandler<FftEventArgs>? FftCalculated;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public SampleAggregator(ISampleProvider source, int fftLength)
    {
        _source = source;
        _fftLength = fftLength;
        _fftBuffer = new Complex[fftLength];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        for (int i = 0; i < samplesRead; i++)
        {
            _fftBuffer[_fftPosition].X = buffer[offset + i];
            _fftBuffer[_fftPosition].Y = 0;
            _fftPosition++;

            if (_fftPosition >= _fftLength)
            {
                _fftPosition = 0;
                FastFourierTransform.FFT(true, (int)Math.Log2(_fftLength), _fftBuffer);
                FftCalculated?.Invoke(this, new FftEventArgs(_fftBuffer));
            }
        }

        return samplesRead;
    }
}

public class FftEventArgs : EventArgs
{
    public Complex[] Result { get; }

    public FftEventArgs(Complex[] result)
    {
        Result = result;
    }
}
