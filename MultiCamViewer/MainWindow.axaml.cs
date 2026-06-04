using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MultiCamViewer
{
    /// <summary>
    /// Defines the layout options for displaying multiple camera feeds in the application. 
    /// </summary>
    public enum CameraLayout
    {
        Grid,
        Horizontal,
        Vertical
    }

    public partial class MainWindow : Window
    {
        #region Member variables

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
        private readonly IReadOnlyList<Grid> _cameraVideoHosts;
        private readonly DispatcherTimer _controlsHideTimer;

        private readonly bool[] _cameraEnabled = [true, true, true, true];
        private readonly bool[] _cameraPlaying = [false, false, false, false];
        private readonly string[] _cameraPlaybackKeys = ["", "", "", ""];

        private readonly MediaPlayer?[] _mediaPlayers = new MediaPlayer?[CameraTotal];
        private LibVLC? _libVlc;

        private bool _vlcReady;
        private bool _vlcInitializing;
        private bool _playbackRequested;
        private int _cameraCount = 4;
        private CameraLayout _cameraLayout = CameraLayout.Grid;

        #endregion /Member variables

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.FullScreen;

            #region Init controls

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

            _cameraVideoHosts =
            [
                Camera1VideoHost,
                Camera2VideoHost,
                Camera3VideoHost,
                Camera4VideoHost
            ];

            #endregion /Init controls

            _controlsHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _controlsHideTimer.Tick += (_, _) => HideControlsOverlay();

            LoadSettings();
            ApplyCameraLayout();
        }

        #region Overrides

        /// <summary>
        /// Overrides the OnOpened method to perform asynchronous initialization tasks when the window is opened.
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnOpened(EventArgs e)
        {
            try
            {
                base.OnOpened(e);

                await Task.Delay(1500);
                await StartLivePlaybackAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during application startup");
                LayoutStatusText.Text = "Error initializing application";
            }
        }

        /// <summary>
        /// Overrides the OnClosed method to perform cleanup tasks when the window is closed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                StopAllCameraStreams();

                foreach (var mediaPlayer in _mediaPlayers)
                    mediaPlayer?.Dispose();

                _libVlc?.Dispose();
                base.OnClosed(e);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during application shutdown");
            }
        }

        #endregion /Overrides

        #region Settings auxiliary methods

        /// <summary>
        /// Constructs the file path for storing the application settings, ensuring it is located in a user-specific application data directory.
        /// </summary>
        /// <returns></returns>
        private string GetSettingsPath()
        {
            try
            {
                var configRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (string.IsNullOrWhiteSpace(configRoot))
                {
                    configRoot = AppContext.BaseDirectory;
                }
                return Path.Combine(configRoot, "MultiCamViewer", "settings.json");
            }
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// Attempts to load the application settings from a JSON file on disk. 
        /// If the file does not exist or an error occurs during loading, default settings are returned instead. 
        /// </summary>
        /// <returns></returns>
        private AppSettings LoadSettingsFromDisk()
        {
            try
            {
                var path = GetSettingsPath();

                if (!File.Exists(path))
                    return AppSettings.CreateDefault();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.CreateDefault();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to load settings from disk");
                return AppSettings.CreateDefault();
            }
        }

        /// <summary>
        /// Updates the camera URL preview text blocks to reflect the current values entered in the camera URL text boxes.
        /// </summary>
        private void UpdateCameraUrlPreviews()
        {
            for (var index = 0; index < _cameraUrlPreviewTexts.Count; index++)
            {
                _cameraUrlPreviewTexts[index].Text = _cameraUrlTextBoxes[index].Text ?? string.Empty;
            }
        }

        /// <summary>
        /// Loads the application settings from disk and applies them to the user interface and internal state.
        /// </summary>
        private void LoadSettings()
        {
            var settings = LoadSettingsFromDisk();
            ApplySettings(settings);
        }

        /// <summary>
        /// Applies the provided application settings to the user interface and internal state of the application.
        /// </summary>
        /// <param name="settings"></param>
        private void ApplySettings(AppSettings settings)
        {
            try
            {
                _cameraCount = Math.Clamp(settings.CameraCount, 0, CameraTotal);
                _cameraLayout = Enum.TryParse<CameraLayout>(settings.Layout, out var layout) ? layout : CameraLayout.Grid;

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
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Serializes the current application settings to a JSON file on disk, ensuring that the necessary directory structure exists before writing the file.
        /// </summary>
        private void SaveSettings()
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to save settings to disk");
                throw;
            }
        }

        /// <summary>
        /// Captures the current application settings from the user interface and internal state, constructing an AppSettings object 
        /// that represents the current configuration of the application.
        /// </summary>
        /// <returns></returns>
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

        #endregion /Settings auxiliary methods

        #region Camera methods

        /// <summary>
        /// Ensures that the camera stream for the specified index is playing if it is enabled and visible.
        /// </summary>
        /// <param name="index"></param>
        private void EnsureCameraPlaying(int index)
        {
            try
            {
                var mediaPlayer = _mediaPlayers[index];

                if (_libVlc is null || mediaPlayer is null)
                    return;

                var url = BuildCameraUrl(index);
                var playbackKey = $"{url}|{_cameraTransportComboBoxes[index].SelectedIndex}";

                if (_cameraPlaying[index] && _cameraPlaybackKeys[index] == playbackKey)
                    return;

                StopCameraStream(index);

                if (string.IsNullOrWhiteSpace(url))
                    return;

                using var media = new Media(_libVlc, url, FromType.FromLocation);

                if (_cameraTransportComboBoxes[index].SelectedIndex == 0)
                    media.AddOption(":rtsp-tcp");

                media.AddOption(":no-audio");
                mediaPlayer.Play(media);

                _cameraPlaying[index] = true;
                _cameraPlaybackKeys[index] = playbackKey;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Synchronizes the playback of camera streams based on the current visibility and enabled state of each camera panel.
        /// </summary>
        private void SynchronizeCameraPlayback()
        {
            try
            {
                if (!_playbackRequested || !_vlcReady)
                    return;

                for (var index = 0; index < CameraTotal; index++)
                {
                    if (_cameraPanels[index].IsVisible && _cameraEnabled[index])
                        EnsureCameraPlaying(index);

                    else
                        StopCameraStream(index);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Stops all camera streams
        /// </summary>
        private void StopAllCameraStreams()
        {
            try
            {
                for (var index = 0; index < CameraTotal; index++)
                    StopCameraStream(index);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Stops the camera stream for the specified index if it is currently playing, and resets the playback state for that camera.
        /// </summary>
        /// <param name="index"></param>
        private void StopCameraStream(int index)
        {
            try
            {
                var mediaPlayer = _mediaPlayers[index];

                if (mediaPlayer?.IsPlaying == true)
                    mediaPlayer.Stop();
                _cameraPlaying[index] = false;
                _cameraPlaybackKeys[index] = string.Empty;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Builds the camera URL for the specified index by combining the base URL entered in the camera URL text box with the login credentials if provided.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private string BuildCameraUrl(int index)
        {
            try
            {
                var url = _cameraUrlTextBoxes[index].Text ?? string.Empty;
                var login = _cameraLoginTextBoxes[index].Text ?? string.Empty;
                var password = _cameraPasswordTextBoxes[index].Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(login))
                    return url;

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo))
                    return url;

                var builder = new UriBuilder(uri)
                {
                    UserName = login,
                    Password = password
                };
                return builder.Uri.ToString();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Initiates the live playback of camera streams. If VLC is already initialized and ready, it synchronizes the camera playback immediately.
        /// </summary>
        /// <returns></returns>
        private async Task StartLivePlaybackAsync()
        {
            try
            {
                _playbackRequested = true;

                if (_vlcReady)
                {
                    SynchronizeCameraPlayback();
                    return;
                }
                await InitializeVlcPlaybackAsync();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Initializes the VLC media player library and sets up the media players for each camera feed.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeVlcPlaybackAsync()
        {
            if (_vlcReady || _vlcInitializing)
                return;

            _vlcInitializing = true;
            await Task.Delay(1000);

            LibVLC libVlc;

            try
            {
                libVlc = await Task.Run(() =>
                {
                    Core.Initialize();
                    return new LibVLC("--no-osd", "--no-video-title-show", "--quiet");
                });
            }
            catch
            {
                _vlcInitializing = false;
                LayoutStatusText.Text = "VLC initialization failed";
                return;
            }

            _libVlc = libVlc;

            for (var index = 0; index < CameraTotal; index++)
            {
                var mediaPlayer = new MediaPlayer(_libVlc)
                {
                    EnableMouseInput = false,
                    EnableKeyInput = false,
                    Mute = true
                };

                _mediaPlayers[index] = mediaPlayer;
                var videoView = new VideoView
                {
                    MediaPlayer = mediaPlayer,
                    Focusable = false
                };
                _cameraVideoHosts[index].Children.Clear();
                _cameraVideoHosts[index].Children.Add(videoView);
            }

            _vlcReady = true;
            _vlcInitializing = false;
            SynchronizeCameraPlayback();
        }

        /// <summary>
        ///Restarts the camera streams for all currently visible camera panels. 
        /// </summary>
        private void RestartVisibleCameraStreams()
        {
            try
            {
                for (var index = 0; index < CameraTotal; index++)
                {
                    if (_cameraPanels[index].IsVisible)
                        StopCameraStream(index);
                }
            }
            catch
            {
                throw;
            }
        }

        #endregion /Camera methods

        #region UI auxiliary methods

        /// <summary>
        /// Displays the controls overlay and hides the camera video hosts to ensure that the controls are prominently visible.
        /// </summary>
        private void ShowControlsOverlay()
        {
            try
            {
                SetVideoHostsVisible(false);
                ControlsOverlay.IsVisible = true;
                _controlsHideTimer.Stop();
                _controlsHideTimer.Start();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Hides the controls overlay and makes the camera video hosts visible again if the settings panel is not currently visible.
        /// </summary>
        private void HideControlsOverlay()
        {
            try
            {
                _controlsHideTimer.Stop();
                ControlsOverlay.IsVisible = false;

                if (!SettingsPanel.IsVisible)
                    SetVideoHostsVisible(true);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Sets the visibility of the camera video host controls based on the provided boolean value. 
        /// </summary>
        /// <param name="isVisible"></param>
        private void SetVideoHostsVisible(bool isVisible)
        {
            for (var index = 0; index < _cameraVideoHosts.Count; index++)
                _cameraVideoHosts[index].IsVisible = isVisible;
        }

        /// <summary>
        /// Reads the enabled state of each camera from the corresponding toggle buttons in the user interface, updates the internal state accordingly,
        /// </summary>
        private void ReadCameraEnabledFromUi()
        {
            for (var index = 0; index < _cameraEnabledButtons.Count; index++)
            {
                _cameraEnabled[index] = _cameraEnabledButtons[index].IsChecked == true;
                _cameraSettingsFields[index].IsEnabled = _cameraEnabled[index];
            }
        }

        /// <summary>
        /// Applies the current camera layout and count settings to the user interface, updating the visibility and arrangement of camera panels,
        /// </summary>
        private void ApplyCameraLayout()
        {
            try
            {
                CameraGrid.RowDefinitions.Clear();
                CameraGrid.ColumnDefinitions.Clear();

                var activePanels = new List<Border>();

                for (var index = 0; index < _cameraPanels.Count; index++)
                {
                    if (_cameraEnabled[index])
                        activePanels.Add(_cameraPanels[index]);
                }

                var activeCameraCount = activePanels.Count;

                if (_cameraCount > activeCameraCount)
                    _cameraCount = activeCameraCount;
                else if (_cameraCount == 0 && activeCameraCount > 0)
                    _cameraCount = 1;

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
                    CameraGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

                for (var column = 0; column < columns; column++)
                    CameraGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

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
                SynchronizeCameraPlayback();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the visibility and enabled state of the camera count selection buttons based on the number of active cameras.
        /// </summary>
        /// <param name="activeCameraCount"></param>
        private void UpdateCameraCountButtons(int activeCameraCount)
        {
            for (var index = 0; index < _countButtons.Count; index++)
            {
                var isAvailable = index < activeCameraCount;
                _countButtons[index].IsVisible = isAvailable;
                _countButtons[index].IsEnabled = isAvailable;
            }
        }

        /// <summary>
        /// Updates the checked state of a list of toggle buttons to reflect the currently selected index, 
        /// ensuring that only the button corresponding to the selected index is checked.
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="checkedIndex"></param>
        private void UpdateCheckedButton(IReadOnlyList<ToggleButton> buttons, int checkedIndex)
        {
            for (var index = 0; index < buttons.Count; index++)
                buttons[index].IsChecked = index == checkedIndex;
        }

        /// <summary>
        /// Updates the status text block to display the current number of active cameras, 
        /// the selected layout, and the arrangement of rows and columns in the camera grid.
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
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

        #endregion /UI auxiliary methods

        #region Event handlers

        /// <summary>
        /// Handles the PointerPressed event on the root view of the application. 
        /// When the user clicks or taps anywhere on the main window, this event is triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RootView_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                ShowControlsOverlay();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling pointer press event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error","Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the PointerPressed event on the settings panel to prevent the event from propagating to the root view,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Handles the Click event for the camera count selection buttons. When a user clicks one of the camera count buttons, this event is triggered,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CameraCountButton_Click(object? sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling camera count click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the Click event for the camera layout selection buttons. 
        /// When a user clicks one of the layout buttons, this event is triggered, and the application updates the camera layout accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LayoutButton_Click(object? sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling layout click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the Click event for the camera enabled toggle buttons. When a user toggles the enabled state of a camera, this event is triggered,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CameraEnabledButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                ShowControlsOverlay();
                ReadCameraEnabledFromUi();
                ApplyCameraLayout();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling camera enabled click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the Click event for the "Start Live" button. 
        /// When the user clicks this button, the application initiates the live playback of camera streams by calling the StartLivePlaybackAsync method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StartLiveButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                ShowControlsOverlay();
                _ = StartLivePlaybackAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling start live click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the Click event for the "Settings" button. When the user clicks this button, the application displays the settings panel by making it visible,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                SettingsPanel.IsVisible = true;
                SetVideoHostsVisible(false);
                ShowControlsOverlay();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling settings click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the Click event for the "Close Settings" button. When the user clicks this button, 
        /// the application hides the settings panel and updates the visibility of the camera video hosts accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CloseSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                SettingsPanel.IsVisible = false;
                SetVideoHostsVisible(!ControlsOverlay.IsVisible);
                ShowControlsOverlay();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling close settings click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        ///Handles the Click event for the "Save Settings" button. 
        ///When the user clicks this button, the application reads the current camera enabled states from the user interface,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                ReadCameraEnabledFromUi();
                UpdateCameraUrlPreviews();
                SaveSettings();
                RestartVisibleCameraStreams();
                ApplyCameraLayout();
                _ = StartLivePlaybackAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling save settings click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        /// <summary>
        /// Handles the Click event for the "Exit Application" button.
        /// When the user clicks this button, the application saves the current settings to disk and then closes the main window,
        /// effectively exiting the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExitApplicationButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling exit application click event");
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong", ButtonEnum.Ok);
                await box.ShowAsync();
            }
        }

        #endregion /Event handlers

    }
}
