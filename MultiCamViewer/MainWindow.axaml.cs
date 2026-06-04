using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;

namespace MultiCamViewer
{
    public partial class MainWindow : Window
    {
        private const int CameraTotal = 4;

        private readonly IReadOnlyList<Border> _cameraPanels;
        private readonly IReadOnlyList<ToggleButton> _countButtons;
        private readonly IReadOnlyList<ToggleButton> _layoutButtons;
        private readonly IReadOnlyList<ToggleButton> _cameraEnabledButtons;
        private readonly IReadOnlyList<StackPanel> _cameraSettingsFields;
        private readonly IReadOnlyList<TextBox> _cameraUrlTextBoxes;
        private readonly IReadOnlyList<TextBox> _cameraLoginTextBoxes;
        private readonly IReadOnlyList<TextBox> _cameraPasswordTextBoxes;
        private readonly IReadOnlyList<ComboBox> _cameraStreamComboBoxes;
        private readonly IReadOnlyList<ComboBox> _cameraTransportComboBoxes;
        private readonly IReadOnlyList<TextBlock> _cameraUrlPreviewTexts;
        private readonly DispatcherTimer _controlsHideTimer;
        private readonly bool[] _cameraEnabled = [true, true, true, true];
        private int _cameraCount = 4;
        private CameraLayout _cameraLayout = CameraLayout.Grid;

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.FullScreen;
            Cursor = new Cursor(StandardCursorType.None);

            _cameraPanels =
            [
                Camera1Panel,
                Camera2Panel,
                Camera3Panel,
                Camera4Panel
            ];

            _countButtons =
            [
                CameraCount1Button,
                CameraCount2Button,
                CameraCount3Button,
                CameraCount4Button
            ];

            _layoutButtons =
            [
                GridLayoutButton,
                HorizontalLayoutButton,
                VerticalLayoutButton
            ];

            _cameraEnabledButtons =
            [
                Camera1EnabledButton,
                Camera2EnabledButton,
                Camera3EnabledButton,
                Camera4EnabledButton
            ];

            _cameraSettingsFields =
            [
                Camera1SettingsFields,
                Camera2SettingsFields,
                Camera3SettingsFields,
                Camera4SettingsFields
            ];

            _cameraUrlTextBoxes =
            [
                Camera1UrlTextBox,
                Camera2UrlTextBox,
                Camera3UrlTextBox,
                Camera4UrlTextBox
            ];

            _cameraLoginTextBoxes =
            [
                Camera1LoginTextBox,
                Camera2LoginTextBox,
                Camera3LoginTextBox,
                Camera4LoginTextBox
            ];

            _cameraPasswordTextBoxes =
            [
                Camera1PasswordTextBox,
                Camera2PasswordTextBox,
                Camera3PasswordTextBox,
                Camera4PasswordTextBox
            ];

            _cameraStreamComboBoxes =
            [
                Camera1StreamComboBox,
                Camera2StreamComboBox,
                Camera3StreamComboBox,
                Camera4StreamComboBox
            ];

            _cameraTransportComboBoxes =
            [
                Camera1TransportComboBox,
                Camera2TransportComboBox,
                Camera3TransportComboBox,
                Camera4TransportComboBox
            ];

            _cameraUrlPreviewTexts =
            [
                Camera1UrlPreviewText,
                Camera2UrlPreviewText,
                Camera3UrlPreviewText,
                Camera4UrlPreviewText
            ];

            _controlsHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _controlsHideTimer.Tick += (_, _) => HideControlsOverlay();

            LoadSettings();
            ApplyCameraLayout();
        }

        private void RootView_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            ShowControlsOverlay();
        }

        private void SettingsPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
        }

        private void CameraCountButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowControlsOverlay();

            _cameraCount = sender switch
            {
                ToggleButton button when button == CameraCount1Button => 1,
                ToggleButton button when button == CameraCount2Button => 2,
                ToggleButton button when button == CameraCount3Button => 3,
                ToggleButton button when button == CameraCount4Button => 4,
                _ => _cameraCount
            };

            ApplyCameraLayout();
        }

        private void LayoutButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowControlsOverlay();

            _cameraLayout = sender switch
            {
                ToggleButton button when button == GridLayoutButton => CameraLayout.Grid,
                ToggleButton button when button == HorizontalLayoutButton => CameraLayout.Horizontal,
                ToggleButton button when button == VerticalLayoutButton => CameraLayout.Vertical,
                _ => _cameraLayout
            };

            ApplyCameraLayout();
        }

        private void CameraEnabledButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowControlsOverlay();
            ReadCameraEnabledFromUi();
            ApplyCameraLayout();
        }

        private void SettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            SettingsPanel.IsVisible = true;
            ShowControlsOverlay();
        }

        private void CloseSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            SettingsPanel.IsVisible = false;
            ShowControlsOverlay();
        }

        private void SaveSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            ReadCameraEnabledFromUi();
            UpdateCameraUrlPreviews();
            SaveSettings();
            ApplyCameraLayout();
            ShowControlsOverlay();
        }

        private void ExitApplicationButton_Click(object? sender, RoutedEventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void ShowControlsOverlay()
        {
            Cursor = new Cursor(StandardCursorType.Arrow);
            ControlsOverlay.IsVisible = true;
            _controlsHideTimer.Stop();
            _controlsHideTimer.Start();
        }

        private void HideControlsOverlay()
        {
            _controlsHideTimer.Stop();
            ControlsOverlay.IsVisible = false;

            if (!SettingsPanel.IsVisible)
            {
                Cursor = new Cursor(StandardCursorType.None);
            }
        }

        private void ReadCameraEnabledFromUi()
        {
            for (var index = 0; index < _cameraEnabledButtons.Count; index++)
            {
                _cameraEnabled[index] = _cameraEnabledButtons[index].IsChecked == true;
                _cameraSettingsFields[index].IsEnabled = _cameraEnabled[index];
            }
        }

        private void ApplyCameraLayout()
        {
            CameraGrid.RowDefinitions.Clear();
            CameraGrid.ColumnDefinitions.Clear();

            var activePanels = new List<Border>();

            for (var index = 0; index < _cameraPanels.Count; index++)
            {
                if (_cameraEnabled[index])
                {
                    activePanels.Add(_cameraPanels[index]);
                }
            }

            var activeCameraCount = activePanels.Count;
            if (_cameraCount > activeCameraCount)
            {
                _cameraCount = activeCameraCount;
            }
            else if (_cameraCount == 0 && activeCameraCount > 0)
            {
                _cameraCount = 1;
            }

            var rows = 1;
            var columns = 1;

            if (_cameraCount > 0)
            {
                switch (_cameraLayout)
                {
                    case CameraLayout.Horizontal:
                        columns = _cameraCount;
                        break;
                    case CameraLayout.Vertical:
                        rows = _cameraCount;
                        break;
                    default:
                        if (_cameraCount == 1)
                        {
                            rows = 1;
                            columns = 1;
                        }
                        else if (_cameraCount == 2)
                        {
                            rows = 1;
                            columns = 2;
                        }
                        else
                        {
                            rows = 2;
                            columns = 2;
                        }
                        break;
                }
            }

            for (var row = 0; row < rows; row++)
            {
                CameraGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            }

            for (var column = 0; column < columns; column++)
            {
                CameraGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            for (var index = 0; index < _cameraPanels.Count; index++)
            {
                var panel = _cameraPanels[index];
                panel.IsVisible = false;
                Grid.SetRow(panel, 0);
                Grid.SetColumn(panel, 0);
            }

            for (var index = 0; index < activePanels.Count; index++)
            {
                var panel = activePanels[index];
                panel.IsVisible = index < _cameraCount;

                if (panel.IsVisible)
                {
                    Grid.SetRow(panel, index / columns);
                    Grid.SetColumn(panel, index % columns);
                }
            }

            UpdateCameraCountButtons(activeCameraCount);
            UpdateCheckedButton(_countButtons, _cameraCount - 1);
            UpdateCheckedButton(_layoutButtons, (int)_cameraLayout);
            UpdateStatusText(rows, columns);
        }

        private void UpdateCameraCountButtons(int activeCameraCount)
        {
            for (var index = 0; index < _countButtons.Count; index++)
            {
                var isAvailable = index < activeCameraCount;
                _countButtons[index].IsVisible = isAvailable;
                _countButtons[index].IsEnabled = isAvailable;
            }
        }

        private static void UpdateCheckedButton(IReadOnlyList<ToggleButton> buttons, int checkedIndex)
        {
            for (var index = 0; index < buttons.Count; index++)
            {
                buttons[index].IsChecked = index == checkedIndex;
            }
        }

        private void UpdateStatusText(int rows, int columns)
        {
            if (_cameraCount == 0)
            {
                LayoutStatusText.Text = "0 cameras - No active cameras";
                return;
            }

            var cameraWord = _cameraCount == 1 ? "camera" : "cameras";
            var layoutName = _cameraLayout switch
            {
                CameraLayout.Horizontal => "Horizontal",
                CameraLayout.Vertical => "Vertical",
                _ => "Grid"
            };

            LayoutStatusText.Text = $"{_cameraCount} {cameraWord} - {layoutName} {columns}x{rows}";
        }

        private void LoadSettings()
        {
            var settings = LoadSettingsFromDisk();
            ApplySettings(settings);
        }

        private AppSettings LoadSettingsFromDisk()
        {
            var path = GetSettingsPath();

            if (!File.Exists(path))
            {
                return AppSettings.CreateDefault();
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.CreateDefault();
            }
            catch
            {
                return AppSettings.CreateDefault();
            }
        }

        private void SaveSettings()
        {
            var settings = CaptureSettings();
            var path = GetSettingsPath();
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        private AppSettings CaptureSettings()
        {
            var settings = new AppSettings
            {
                CameraCount = _cameraCount,
                Layout = _cameraLayout.ToString()
            };

            for (var index = 0; index < CameraTotal; index++)
            {
                settings.Cameras.Add(new CameraSettings
                {
                    Enabled = _cameraEnabled[index],
                    Url = _cameraUrlTextBoxes[index].Text ?? string.Empty,
                    Login = _cameraLoginTextBoxes[index].Text ?? string.Empty,
                    Password = _cameraPasswordTextBoxes[index].Text ?? string.Empty,
                    StreamIndex = _cameraStreamComboBoxes[index].SelectedIndex,
                    TransportIndex = _cameraTransportComboBoxes[index].SelectedIndex
                });
            }

            return settings;
        }

        private void ApplySettings(AppSettings settings)
        {
            _cameraCount = Math.Clamp(settings.CameraCount, 0, CameraTotal);
            _cameraLayout = Enum.TryParse<CameraLayout>(settings.Layout, out var layout)
                ? layout
                : CameraLayout.Grid;

            for (var index = 0; index < CameraTotal; index++)
            {
                var cameraSettings = index < settings.Cameras.Count
                    ? settings.Cameras[index]
                    : CameraSettings.CreateDefault(index);

                _cameraEnabled[index] = cameraSettings.Enabled;
                _cameraEnabledButtons[index].IsChecked = cameraSettings.Enabled;
                _cameraSettingsFields[index].IsEnabled = cameraSettings.Enabled;
                _cameraUrlTextBoxes[index].Text = cameraSettings.Url;
                _cameraLoginTextBoxes[index].Text = cameraSettings.Login;
                _cameraPasswordTextBoxes[index].Text = cameraSettings.Password;
                _cameraStreamComboBoxes[index].SelectedIndex = Math.Clamp(cameraSettings.StreamIndex, 0, 2);
                _cameraTransportComboBoxes[index].SelectedIndex = Math.Clamp(cameraSettings.TransportIndex, 0, 1);
            }

            UpdateCameraUrlPreviews();
        }

        private void UpdateCameraUrlPreviews()
        {
            for (var index = 0; index < _cameraUrlPreviewTexts.Count; index++)
            {
                _cameraUrlPreviewTexts[index].Text = _cameraUrlTextBoxes[index].Text ?? string.Empty;
            }
        }

        private static string GetSettingsPath()
        {
            var configRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (string.IsNullOrWhiteSpace(configRoot))
            {
                configRoot = AppContext.BaseDirectory;
            }

            return Path.Combine(configRoot, "MultiCamViewer", "settings.json");
        }

        private enum CameraLayout
        {
            Grid,
            Horizontal,
            Vertical
        }

        private sealed class AppSettings
        {
            public int CameraCount { get; set; } = 4;

            public string Layout { get; set; } = nameof(CameraLayout.Grid);

            public List<CameraSettings> Cameras { get; set; } = [];

            public static AppSettings CreateDefault()
            {
                var settings = new AppSettings();

                for (var index = 0; index < CameraTotal; index++)
                {
                    settings.Cameras.Add(CameraSettings.CreateDefault(index));
                }

                return settings;
            }
        }

        private sealed class CameraSettings
        {
            public bool Enabled { get; set; } = true;

            public string Url { get; set; } = string.Empty;

            public string Login { get; set; } = "admin";

            public string Password { get; set; } = "password";

            public int StreamIndex { get; set; }

            public int TransportIndex { get; set; }

            public static CameraSettings CreateDefault(int index)
            {
                return new CameraSettings
                {
                    Url = $"rtsp://192.168.1.10{index + 1}/live"
                };
            }
        }
    }
}
