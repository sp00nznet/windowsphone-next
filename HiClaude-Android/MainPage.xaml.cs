using ClaudeCodeAndroid.ViewModels;

namespace ClaudeCodeAndroid;

public partial class MainPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public MainPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Subscribe to collection changes to auto-scroll
        _viewModel.Messages.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    MessagesCollectionView.ScrollTo(_viewModel.Messages.Count - 1);
                });
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

// Converters
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}

public class ListeningIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isListening)
            return isListening ? "⏹" : "🎤";
        return "🎤";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ListeningColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isListening)
            return Color.FromArgb(isListening ? "#EF4444" : "#374151");
        return Color.FromArgb("#374151");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
