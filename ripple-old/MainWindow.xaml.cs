using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;

namespace ripple
{
    public partial class MainWindow : System.Windows.Window
    {
        // Manager and session
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;

        // Timer to manually track song progress
        private readonly DispatcherTimer _songTimer;

        public MainWindow()
        {
            InitializeComponent();
            Topmost = true;

            _songTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            _songTimer.Tick += (_, __) => UpdateTimelinePosition();

            // Custom window
            async void InitializeMediaSession()
            {
                try
                {
                    // Session manager
                    _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                    if (_sessionManager == null)
                        throw new Exception("Could not initialize session manager. MainWindow.xaml.cs:MainWindow()");

                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

                    // Get current session
                    UpdateCurrentSession();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message + " < MainWindow()");
                }
            }
            InitializeMediaSession();
        }

        // Window Controls
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BeginAnimation(LeftProperty, null);
            BeginAnimation(TopProperty, null);
            Cursor = System.Windows.Input.Cursors.None;
            DragMove();
            CheckDock();
            Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void CheckDock()
        {
            // Get current screen
            var screen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
            var area = screen.WorkingArea;

            // Convert from forms to wpf units
            var dpi = VisualTreeHelper.GetDpi(this);
            var bounds = new Rect(
                area.Left / dpi.DpiScaleX,
                area.Top / dpi.DpiScaleY,
                area.Width / dpi.DpiScaleX,
                area.Height / dpi.DpiScaleY
            );

            double thresholdX = 350;
            double thresholdY = 250;
            double padding = 10;

            double bottomRightX = bounds.Right - Width;
            double bottomRightY = bounds.Bottom - Height;
            double topRightX = bounds.Right - Width;
            double topRightY = bounds.Top;

            double bottomLeftX = bounds.Left;
            double bottomLeftY = bounds.Bottom - Height;
            double topLeftX = bounds.Left;
            double topLeftY = bounds.Top;

            bool closeToRight = Math.Abs((Left + Width) - bounds.Right) <= thresholdX;
            bool closeToBottom = Math.Abs((Top + Height) - bounds.Bottom) <= thresholdY;
            bool closeToLeft = Math.Abs(Left - bounds.Left) <= thresholdX;
            bool closeToTop = Math.Abs(Top - bounds.Top) <= thresholdY;

            double? targetX = null;
            double? targetY = null;

            if (closeToBottom && closeToRight)
            {
                targetX = bottomRightX - padding;
                targetY = bottomRightY - padding;
            }
            else if (closeToTop && closeToRight)
            {
                targetX = topRightX - padding;
                targetY = topRightY + padding;
            }
            else if (closeToBottom && closeToLeft)
            {
                targetX = bottomLeftX + padding;
                targetY = bottomLeftY - padding;
            }
            else if (closeToTop && closeToLeft)
            {
                targetX = topLeftX + padding;
                targetY = topLeftY + padding;
            }

            if (targetX != null && targetY != null)
            {
                SmoothMoveWindow(targetX.Value, targetY.Value);
            }
        }

        private void SmoothMoveWindow(double targetX, double targetY)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds(600);
            QuadraticEase easing = new() { EasingMode = EasingMode.EaseOut };

            DoubleAnimation leftMove = new()
            {
                From = Left,
                To = targetX,
                Duration = duration,
                EasingFunction = easing
            };

            DoubleAnimation topMove = new()
            {
                From = Top,
                To = targetY,
                Duration = duration,
                EasingFunction = easing
            };

            BeginAnimation(LeftProperty, leftMove);
            BeginAnimation(TopProperty, topMove);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Tiny_Click(object sender, RoutedEventArgs e)
        {
            // Window
            Height = 100;
            Width = 300;

            // Main grid
            MainGrid.RowDefinitions.Clear();
            RowDefinition row1 = new() { Height = new GridLength(20) };
            RowDefinition row2 = new() { Height = new GridLength(30) };
            RowDefinition row3 = new() { Height = new GridLength(20) };
            RowDefinition row4 = new() { Height = new GridLength(30) };
            MainGrid.RowDefinitions.Add(row1);
            MainGrid.RowDefinitions.Add(row2);
            MainGrid.RowDefinitions.Add(row3);
            MainGrid.RowDefinitions.Add(row4);

            // Title bar
            SettingsMenu.Margin = new Thickness(0, 0, 0, 0);
            SettingsStack.Width = Width;
            WindowTitle.FontSize = 11;
            WindowTitle.Margin = new Thickness(5, 0, 0, 0);
            WindowControlsStack.Margin = new Thickness(0, 0, 5, 0);

            // Metadata
            SongTitleBox.FontSize = 12;
            SongArtistBox.FontSize = 11;

            // Timeline
            TimelineGrid.ColumnDefinitions.Clear();
            ColumnDefinition col1 = new() { Width = new GridLength(50) };
            ColumnDefinition col2 = new() { Width = new GridLength(200) };
            ColumnDefinition col3 = new() { Width = new GridLength(50) };
            TimelineGrid.ColumnDefinitions.Add(col1);
            TimelineGrid.ColumnDefinitions.Add(col2);
            TimelineGrid.ColumnDefinitions.Add(col3);
            TimelineBar.Width = 200;

            CheckDock();
        }

        private void Small_Click(object sender, RoutedEventArgs e)
        {
            // Window
            Height = 150;
            Width = 350;

            // Main grid
            MainGrid.RowDefinitions.Clear();
            RowDefinition row1 = new() { Height = new GridLength(30) };
            RowDefinition row2 = new() { Height = new GridLength(50) };
            RowDefinition row3 = new() { Height = new GridLength(30) };
            RowDefinition row4 = new() { Height = new GridLength(40) };
            MainGrid.RowDefinitions.Add(row1);
            MainGrid.RowDefinitions.Add(row2);
            MainGrid.RowDefinitions.Add(row3);
            MainGrid.RowDefinitions.Add(row4);

            // Title bar
            SettingsMenu.Margin = new Thickness(10, 0, 10, 0);
            SettingsStack.Width = Width;
            WindowTitle.FontSize = 12;
            WindowTitle.Margin = new Thickness(0, 0, 0, 0);
            WindowControlsStack.Margin = new Thickness(0, 0, 10, 0);

            // Metadata
            SongTitleBox.FontSize = 14;
            SongArtistBox.FontSize = 12;

            // TimelineBar
            TimelineGrid.ColumnDefinitions.Clear();
            ColumnDefinition col1 = new() { Width = new GridLength(50) };
            ColumnDefinition col2 = new() { Width = new GridLength(250) };
            ColumnDefinition col3 = new() { Width = new GridLength(50) };
            TimelineGrid.ColumnDefinitions.Add(col1);
            TimelineGrid.ColumnDefinitions.Add(col2);
            TimelineGrid.ColumnDefinitions.Add(col3);
            TimelineBar.Width = 250;

            CheckDock();
        }

        // Playback Events
        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, object args)
        {
            try
            {
                UpdateCurrentSession();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + " < OnCurrentSessionChanged()");
            }
        }

        private void UpdateCurrentSession()
        {
            if (_sessionManager == null)
                throw new Exception("Session manager is null. MainWindow.xaml.cs:UpdateCurrentSession()");

            var session = _sessionManager.GetCurrentSession()
                ?? throw new Exception("Could not get new session. MainWindow.xaml.cs:UpdateCurrentSession()");

            _currentSession = session;
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

            UpdateMediaProps();
            UpdatePlaybackInfo();
            UpdateTimeline();

            UpdateVisibility();
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            try 
            {
                UpdateMediaProps();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + " < OnMediaPropertiesChanged()");
            }
        }

        private void UpdateMediaProps()
        {
            async Task AsyncWorker()
            {
                if (_currentSession == null)
                    throw new Exception("Current session is null. MainWindow.xaml.cs:UpdateMediaProps()");

                var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync()
                    ?? throw new Exception("Could not get media properties. MainWindow.xaml.cs:UpdateMediaProps()");

                var thumbVis = Visibility.Visible;
                var thumbnail = mediaProperties.Thumbnail;
                if (thumbnail != null)
                {
                    var stream = await thumbnail.OpenReadAsync();

                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream.AsStream();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();

                    Dispatcher.Invoke(() => { MediaThumbnail.Source = image; });
                }
                else thumbVis = Visibility.Hidden;

                await Dispatcher.InvokeAsync(() =>
                {
                    MediaThumbnail.Visibility = thumbVis;
                    SongTitleBox.Text = mediaProperties.Title;
                    SongArtistBox.Text = mediaProperties.Artist;
                });
            }
            _ = AsyncWorker();
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            try
            {
                UpdatePlaybackInfo();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void UpdatePlaybackInfo()
        {
            if (_currentSession == null)
                throw new Exception("Current session is null. MainWindow.xaml.cs:UpdatePlaybackInfo()");

            // Playback info and timeline
            var playbackInfo = _currentSession.GetPlaybackInfo()
                ?? throw new Exception("Could not get playback info. MainWindow.xaml.cs:UpdatePlaybackInfo()");

            _songTimer.IsEnabled = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var icon = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                ? FontAwesome.WPF.FontAwesomeIcon.Pause
                : FontAwesome.WPF.FontAwesomeIcon.Play;

            Dispatcher.Invoke(() =>
            {
                TogglePlayButtonIcon.Icon = icon;
            });
        }

        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            try
            {
                UpdateTimeline();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void UpdateTimeline()
        {
            Dispatcher.InvokeAsync(() =>
            {
                UpdateTimelineBounds();
                UpdateTimelinePosition();
            }, DispatcherPriority.Normal);
        }

        private void UpdateTimelineBounds()
        {
            if (_currentSession == null)
                throw new Exception("Current session is null. MainWindow.xaml.cs:UpdateTimelineBounds()");

            var timeline = _currentSession.GetTimelineProperties();
            TimelineBar.Minimum = timeline.StartTime.TotalSeconds;
            TimelineBar.Maximum = timeline.EndTime.TotalSeconds;
        }

        private void UpdateTimelinePosition()
        {
            if (_currentSession == null)
                throw new Exception("Current session is null. MainWindow.xaml.cs:UpdateTimelinePosition()");

            var timeline = _currentSession.GetTimelineProperties();
            double rate = _currentSession.GetPlaybackInfo().PlaybackRate ?? 1.00;
            double elapsed = (DateTimeOffset.Now - timeline.LastUpdatedTime).TotalSeconds * rate;
            double pos = timeline.Position.TotalSeconds + elapsed;
            pos = Math.Clamp(pos, TimelineBar.Minimum, TimelineBar.Maximum);

            // Update UI elements
            TimelineBar.Value = pos;

            SongStartMin.Text = ((int)pos / 60).ToString("D2");
            SongStartSec.Text = ((int)pos % 60).ToString("D2");

            SongEndMin.Text = ((int)timeline.EndTime.TotalSeconds / 60).ToString("D2");
            SongEndSec.Text = ((int)timeline.EndTime.TotalSeconds % 60).ToString("D2");
        }

        // Playback Controls
        private void UpdateVisibility()
        {
            if (_currentSession == null)
                throw new Exception("Could not get current session. MainWindow.xaml.cs:UpdateVisibility()"); ;

            var playbackInfo = _currentSession.GetPlaybackInfo()
                ?? throw new Exception("Could not get playback info. MainWindow.xaml.cs:UpdateVisibility()");

            // Get visibilities
            Visibility skipPrevVis = playbackInfo.Controls.IsPreviousEnabled
                ? Visibility.Visible
                : Visibility.Hidden;
            Visibility playPauseVis = playbackInfo.Controls.IsPlayPauseToggleEnabled
                ? Visibility.Visible
                : Visibility.Hidden;
            Visibility skipNextVis = playbackInfo.Controls.IsNextEnabled
                ? Visibility.Visible
                : Visibility.Hidden;

            // Update
            Dispatcher.Invoke(() =>
            {
                SkipPreviousButton.Visibility = skipPrevVis;
                TogglePlayPauseButton.Visibility = playPauseVis;
                SkipNextButton.Visibility = skipNextVis;
            });
        }
        
        private void SkipPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            SkipPreviousButton.IsEnabled = false;

            async Task AsyncWorker()
            {
                try
                {
                    await _currentSession?.TrySkipPreviousAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            SkipPreviousButton.IsEnabled = true;
        }

        private void SkipNextButton_Click(object sender, RoutedEventArgs e)
        {
            SkipNextButton.IsEnabled = false;

            async Task AsyncWorker()
            {
                try
                {
                    await _currentSession?.TrySkipNextAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            SkipNextButton.IsEnabled = true;
        }

        private void TogglePlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPauseButton.IsEnabled = false;

            async Task AsyncWorker()
            {
                try
                {
                    await _currentSession?.TryTogglePlayPauseAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            TogglePlayPauseButton.IsEnabled = true;
        }
    }
}
