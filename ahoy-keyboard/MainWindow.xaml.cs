using ahoy_keyboard;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;

namespace AhoyKeyboard
{
    public partial class MainWindow : Window
    {
        private KeyboardManager _keyboardManager = new KeyboardManager();

        public MainWindow()
        {
            InitializeComponent();
            _keyboardManager.KeyboardsChanged += KeyboardManager_KeyboardsChanged;
            RefreshKeyboards();
        }

        private void RefreshKeyboards()
        {
            _keyboardManager.RefreshKeyboards();
            KeyboardListView.ItemsSource = _keyboardManager.Keyboards;
            UpdateIndicators();
        }

        private void KeyboardManager_KeyboardsChanged(object sender, EventArgs e)
        {
            UpdateIndicators();
        }

        private void UpdateIndicators()
        {
            UsbIndicator.Fill = _keyboardManager.IsUsbKeyboardConnected() ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            InternalKeyboardIndicator.Fill = _keyboardManager.IsInternalKeyboardActive() ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        }

        private void Keyboard_CheckChanged(object sender, RoutedEventArgs e)
        {
            KeyboardDevice device = ((FrameworkElement)sender).DataContext as KeyboardDevice;
            if (device != null)
            {
                _keyboardManager.ToggleKeyboardState(device);
                UpdateIndicators();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshKeyboards();
        }
    }

    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Enabled" : "Disabled";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
