using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowsPhoneNext.Calculator
{
    public partial class MainWindow : Window
    {
        private double _currentValue = 0;
        private double _previousValue = 0;
        private string _currentOperator = "";
        private bool _isNewEntry = true;
        private bool _hasDecimal = false;
        private string _expression = "";
        private bool _isLandscape = false;

        // Portrait: 720x1560, Landscape: 1560x720
        private const int PortraitWidth = 720;
        private const int PortraitHeight = 1560;
        private const int LandscapeWidth = 1560;
        private const int LandscapeHeight = 720;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplay();
        }

        public void SetOrientation(bool isLandscape)
        {
            _isLandscape = isLandscape;

            if (isLandscape)
            {
                Width = LandscapeWidth;
                Height = LandscapeHeight;
                PortraitButtons.Visibility = Visibility.Collapsed;
                LandscapeButtons.Visibility = Visibility.Visible;
            }
            else
            {
                Width = PortraitWidth;
                Height = PortraitHeight;
                PortraitButtons.Visibility = Visibility.Visible;
                LandscapeButtons.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateDisplay()
        {
            DisplayText.Text = FormatNumber(_currentValue);
            ExpressionText.Text = _expression;
        }

        private string FormatNumber(double value)
        {
            if (double.IsNaN(value))
                return "Error";
            if (double.IsInfinity(value))
                return "∞";

            // Format with up to 10 decimal places, removing trailing zeros
            string formatted = value.ToString("G10", CultureInfo.InvariantCulture);

            // If the number is too long, use scientific notation
            if (formatted.Length > 15)
            {
                formatted = value.ToString("E5", CultureInfo.InvariantCulture);
            }

            return formatted;
        }

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string digit = button.Content.ToString()!;
                AppendDigit(digit);
            }
        }

        private void AppendDigit(string digit)
        {
            if (_isNewEntry)
            {
                _currentValue = double.Parse(digit);
                _isNewEntry = false;
                _hasDecimal = false;
            }
            else
            {
                string currentStr = DisplayText.Text;
                if (currentStr == "0" && digit != "0")
                {
                    currentStr = digit;
                }
                else if (currentStr != "0" || _hasDecimal)
                {
                    currentStr += digit;
                }

                if (double.TryParse(currentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    _currentValue = result;
                }
            }

            UpdateDisplay();
        }

        private void Decimal_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasDecimal)
            {
                _hasDecimal = true;
                if (_isNewEntry)
                {
                    _currentValue = 0;
                    _isNewEntry = false;
                }
                DisplayText.Text = DisplayText.Text + ".";
            }
        }

        private void Operator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string op)
            {
                SetOperator(op);
            }
        }

        private void SetOperator(string op)
        {
            if (!_isNewEntry && !string.IsNullOrEmpty(_currentOperator))
            {
                Calculate();
            }

            _previousValue = _currentValue;
            _currentOperator = op;
            _isNewEntry = true;
            _hasDecimal = false;

            string opSymbol = op switch
            {
                "+" => "+",
                "-" => "-",
                "*" => "×",
                "/" => "÷",
                "^" => "^",
                _ => op
            };

            _expression = $"{FormatNumber(_previousValue)} {opSymbol}";
            UpdateDisplay();
        }

        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            Calculate();
            _expression = "";
            _currentOperator = "";
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Calculate()
        {
            if (string.IsNullOrEmpty(_currentOperator))
                return;

            double result = _currentOperator switch
            {
                "+" => _previousValue + _currentValue,
                "-" => _previousValue - _currentValue,
                "*" => _previousValue * _currentValue,
                "/" => _currentValue != 0 ? _previousValue / _currentValue : double.NaN,
                "^" => Math.Pow(_previousValue, _currentValue),
                _ => _currentValue
            };

            _currentValue = result;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = 0;
            _previousValue = 0;
            _currentOperator = "";
            _isNewEntry = true;
            _hasDecimal = false;
            _expression = "";
            UpdateDisplay();
        }

        private void Negate_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = -_currentValue;
            UpdateDisplay();
        }

        private void Percent_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = _currentValue / 100.0;
            UpdateDisplay();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            string currentStr = DisplayText.Text;
            if (currentStr.Length > 1)
            {
                currentStr = currentStr.Substring(0, currentStr.Length - 1);
                if (currentStr == "-")
                    currentStr = "0";

                if (double.TryParse(currentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    _currentValue = result;
                    _hasDecimal = currentStr.Contains('.');
                }
            }
            else
            {
                _currentValue = 0;
                _hasDecimal = false;
            }
            UpdateDisplay();
        }

        private void Sqrt_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = Math.Sqrt(_currentValue);
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Square_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = _currentValue * _currentValue;
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Reciprocal_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = _currentValue != 0 ? 1.0 / _currentValue : double.NaN;
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Power_Click(object sender, RoutedEventArgs e)
        {
            SetOperator("^");
        }

        private void Sin_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = Math.Sin(_currentValue * Math.PI / 180.0); // Degrees
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Cos_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = Math.Cos(_currentValue * Math.PI / 180.0);
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Tan_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = Math.Tan(_currentValue * Math.PI / 180.0);
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Pi_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = Math.PI;
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void E_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = Math.E;
            _isNewEntry = true;
            UpdateDisplay();
        }

        private void Parenthesis_Click(object sender, RoutedEventArgs e)
        {
            // Simplified parenthesis handling - just add to expression display
            if (sender is Button button && button.Tag is string paren)
            {
                _expression += paren;
                ExpressionText.Text = _expression;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D0 or Key.NumPad0:
                    AppendDigit("0");
                    break;
                case Key.D1 or Key.NumPad1:
                    AppendDigit("1");
                    break;
                case Key.D2 or Key.NumPad2:
                    AppendDigit("2");
                    break;
                case Key.D3 or Key.NumPad3:
                    AppendDigit("3");
                    break;
                case Key.D4 or Key.NumPad4:
                    AppendDigit("4");
                    break;
                case Key.D5 or Key.NumPad5:
                    AppendDigit("5");
                    break;
                case Key.D6 or Key.NumPad6:
                    AppendDigit("6");
                    break;
                case Key.D7 or Key.NumPad7:
                    AppendDigit("7");
                    break;
                case Key.D8 or Key.NumPad8:
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                        SetOperator("*");
                    else
                        AppendDigit("8");
                    break;
                case Key.D9 or Key.NumPad9:
                    AppendDigit("9");
                    break;
                case Key.Add:
                    SetOperator("+");
                    break;
                case Key.Subtract:
                    SetOperator("-");
                    break;
                case Key.Multiply:
                    SetOperator("*");
                    break;
                case Key.Divide:
                    SetOperator("/");
                    break;
                case Key.OemPlus:
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                        SetOperator("+");
                    else
                        Equals_Click(sender, e);
                    break;
                case Key.OemMinus:
                    SetOperator("-");
                    break;
                case Key.Enter:
                    Equals_Click(sender, e);
                    break;
                case Key.Back:
                    Backspace_Click(sender, e);
                    break;
                case Key.Delete or Key.C:
                    Clear_Click(sender, e);
                    break;
                case Key.OemPeriod or Key.Decimal:
                    Decimal_Click(sender, e);
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
            e.Handled = true;
        }
    }
}
