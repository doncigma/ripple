using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Windows.Media;
using Windows.Media.Control;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ripple
{
    public partial class MainWindow : System.Windows.Window
    {
        // Manager and session
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private GlobalSystemMediaTransportControlsSessionPlaybackControls? _controls;

        // Controls
        private bool _shuffleEnabled;
        private bool _skipPrevEnabled;
        private bool _playPauseEnabled;
        private bool _seekEnabled;
        private bool _skipNextEnabled;
        private bool _repeatEnabled;

        // Shuffle and Repeat states
        private bool _shuffleState;
        private MediaPlaybackAutoRepeatMode RepeatState { get; set; }

        // Timer to keep the timeline moving once per second
        private readonly DispatcherTimer _songTimer;

        public MainWindow()
        {
            InitializeComponent();
            Topmost = true;

            _songTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            _songTimer.Tick += (_, __) => UpdateTimelinePosition(); // Hook the timer that drives the progress bar

            // Custom window
            async void InitializeMediaSession()
            {
                try
                {
                    _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                    if (_sessionManager == null) { throw new Exception("Could not initialize session manager. MainWindow.xaml.cs:MainWindow()"); }

                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

                    _currentSession = _sessionManager.GetCurrentSession();

                    if (_currentSession != null)
                    {
                        _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                        _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                        _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

                        // Playback info and timeline
                        var playbackInfo = _currentSession.GetPlaybackInfo()
                            ?? throw new Exception("Could not intialize playback info. MainWindow.xaml.cs:MainWindow()");

                        _songTimer.IsEnabled = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                        _controls = playbackInfo.Controls;

                        // Media properties
                        var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync()
                            ?? throw new Exception("Could not intialize media properties. MainWindow.xaml.cs:MainWindow()");

                        FontAwesome.WPF.FontAwesomeIcon icon = FontAwesome.WPF.FontAwesomeIcon.None;
                        if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                            icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                        else icon = FontAwesome.WPF.FontAwesomeIcon.Play;

                        Dispatcher.Invoke(() =>
                        {
                            SongTitleBox.Text = mediaProperties.Title;
                            SongArtistBox.Text = mediaProperties.Artist;
                            TogglePlayButtonIcon.Icon = icon;
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            InitializeMediaSession();

            UpdateVisibility();
            UpdateTimeline();
        }

        private void UpdateVisibility()
        {
            var playbackInfo = _currentSession?.GetPlaybackInfo();
            _controls = playbackInfo?.Controls;

            if (_controls != null)
            {
                _shuffleEnabled = _controls.IsShuffleEnabled;
                _skipPrevEnabled = _controls.IsPreviousEnabled;
                _seekEnabled = _controls.IsPlaybackPositionEnabled;
                _playPauseEnabled = _controls.IsPlayPauseToggleEnabled;
                _skipNextEnabled = _controls.IsNextEnabled;
                _repeatEnabled = _controls.IsRepeatEnabled;
            }
            else
            {
                _shuffleEnabled = false;
                _skipPrevEnabled = false;
                _seekEnabled = false;
                _skipNextEnabled = false;
                _repeatEnabled = false;
            }

            Visibility shuffleVis = Visibility.Hidden;
            Visibility skipPrevVis = Visibility.Hidden;
            Visibility playPauseVis = Visibility.Hidden;
            Visibility seekerVis = Visibility.Hidden;
            Visibility skipNextVis = Visibility.Hidden;
            Visibility repeatVis = Visibility.Hidden;

            // Shuffle
            if (_shuffleEnabled) { shuffleVis = Visibility.Visible; }
            else if (!_shuffleEnabled) { shuffleVis = Visibility.Hidden; }

            // Skip prev
            if (_skipPrevEnabled) { skipPrevVis = Visibility.Visible; }
            else if (!_shuffleEnabled) { skipPrevVis = Visibility.Hidden; }

            // Play pause
            if (_playPauseEnabled) { playPauseVis = Visibility.Visible; }
            else if (!_playPauseEnabled) { playPauseVis = Visibility.Hidden; }

            // Seeker
            if (_seekEnabled) { seekerVis = Visibility.Visible; }
            else if (!_seekEnabled) { seekerVis = Visibility.Hidden; }

            // Skip next
            if (_skipNextEnabled) { skipNextVis = Visibility.Visible; }
            else if (!_skipNextEnabled) { skipNextVis = Visibility.Hidden; }

            // Repeat
            if (_repeatEnabled) { repeatVis = Visibility.Visible; }
            else if (!_repeatEnabled) { repeatVis = Visibility.Hidden; }

            Dispatcher.Invoke(() =>
            {
                ShuffleButton.Visibility = shuffleVis;
                SkipPreviousButton.Visibility = skipPrevVis;
                TogglePlayPauseButton.Visibility = playPauseVis;
                Seeker.Visibility = seekerVis;
                SkipNextButton.Visibility = skipNextVis;
                RepeatButton.Visibility = repeatVis;
            });
        }

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
            // For multiple monitor setup (not working): 
            //System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            //var bounds = screen.Bounds;

            Rect bounds = SystemParameters.WorkArea;
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

        private bool PingShuffle()
        {
            _shuffleState = !_shuffleState;
            return _shuffleState;
        }

        private MediaPlaybackAutoRepeatMode? PingRepeat()
        {
            //MediaPlaybackAutoRepeatMode mode = (_repeatState + 1);
            return ++RepeatState;
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, object args)
        {
            // Detach events from previous session
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }

            var newSession = sender.GetCurrentSession()
                ?? throw new Exception("Could not get media session");
            _currentSession = newSession;
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            UpdateTimelinePosition();

            UpdateVisibility();
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            async Task AsyncWorker()
            {
                try
                {
                    var mediaProperties = await sender.TryGetMediaPropertiesAsync();
                    if (mediaProperties != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SongTitleBox.Text = mediaProperties.Title;
                            SongArtistBox.Text = mediaProperties.Artist;
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            UpdateVisibility();
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            async Task AsyncWorker()
            {
                try
                {
                    // Playback info and timeline
                    var playbackInfo = sender.GetPlaybackInfo()
                        ?? throw new Exception("Could not get playback info. MainWindow.xaml.cs:OnPlaybackInfoChanged()");

                    _songTimer.IsEnabled = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                    _controls = playbackInfo.Controls;

                    // Media properties
                    var mediaProperties = sender.TryGetMediaPropertiesAsync().GetResults()
                        ?? throw new Exception("Could not get media properties. MainWindow.xaml.cs:OnPlaybackInfoChanged()");

                    FontAwesome.WPF.FontAwesomeIcon icon = FontAwesome.WPF.FontAwesomeIcon.None;
                    if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                        icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                    else icon = FontAwesome.WPF.FontAwesomeIcon.Play;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        SongTitleBox.Text = mediaProperties.Title;
                        SongArtistBox.Text = mediaProperties.Artist;
                        TogglePlayButtonIcon.Icon = icon;
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            UpdateTimeline();
        }

        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            UpdateTimeline();
        }

        private void UpdateTimeline()
        {
            Dispatcher.InvokeAsync(() =>
            {
                UpdateTimelineBounds();
                UpdateTimelinePosition();
            }, DispatcherPriority.Background);
        }

        private void UpdateTimelineBounds()
        {
            if (_currentSession == null) return;

            var timeline = _currentSession.GetTimelineProperties();
            Seeker.Minimum = timeline.StartTime.TotalSeconds;
            Seeker.Maximum = timeline.EndTime.TotalSeconds;
        }

        private void UpdateTimelinePosition()
        {
            if (_currentSession == null) return;

            var timeline = _currentSession.GetTimelineProperties();
            double rate = _currentSession.GetPlaybackInfo().PlaybackRate ?? 1.00;
            double elapsed = (DateTimeOffset.Now - timeline.LastUpdatedTime).TotalSeconds * rate;
            double pos = timeline.Position.TotalSeconds + elapsed;
            pos = Math.Clamp(pos, Seeker.Minimum, Seeker.Maximum);

            // Update UI elements
            Seeker.Value = pos;

            SongStartMin.Text = ((int)pos / 60).ToString("D2");
            SongStartSec.Text = ((int)pos % 60).ToString("D2");

            SongEndMin.Text = ((int)timeline.EndTime.TotalSeconds / 60).ToString("D2");
            SongEndSec.Text = ((int)timeline.EndTime.TotalSeconds % 60).ToString("D2");
        }

        // TODO: Seeker
        //private void Seeker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    async Task AsyncWorker()
        //    {
        //        try
        //        {
        //            await _currentSession.TryChangePlaybackPositionAsync((long)e.NewValue);
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Windows.MessageBox.Show(ex.Message);
        //        }
        //    }
        //    _ = AsyncWorker();
        //}

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ShuffleButton.IsEnabled = false;

            async Task AsyncWorker()
            {
                try
                {
                    bool newShuffleState = PingShuffle();
                    await _currentSession?.TryChangeShuffleActiveAsync(newShuffleState);

                    SolidColorBrush brush;

                    switch (newShuffleState)
                    {
                        case true:
                            brush = new SolidColorBrush(Colors.LightGray);
                            break;
                        case false:
                            brush = new SolidColorBrush(Colors.Transparent);
                            break;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        ShuffleButton.Background = brush;
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            ShuffleButton.IsEnabled = true;
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

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            RepeatButton.IsEnabled = false;

            async Task AsyncWorker()
            {
                try
                {
                    MediaPlaybackAutoRepeatMode? mode = PingRepeat();
                    if (mode.HasValue)
                    {
                        await _currentSession?.TryChangeAutoRepeatModeAsync((MediaPlaybackAutoRepeatMode)mode);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            RepeatButton.IsEnabled = true;
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

            // Seeker
            SeekerGrid.ColumnDefinitions.Clear();
            ColumnDefinition col1 = new() { Width = new GridLength(50) };
            ColumnDefinition col2 = new() { Width = new GridLength(200) };
            ColumnDefinition col3 = new() { Width = new GridLength(50) };
            SeekerGrid.ColumnDefinitions.Add(col1);
            SeekerGrid.ColumnDefinitions.Add(col2);
            SeekerGrid.ColumnDefinitions.Add(col3);
            Seeker.Width = 200;
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

            // Seeker
            SeekerGrid.ColumnDefinitions.Clear();
            ColumnDefinition col1 = new() { Width = new GridLength(50) };
            ColumnDefinition col2 = new() { Width = new GridLength(250) };
            ColumnDefinition col3 = new() { Width = new GridLength(50) };
            SeekerGrid.ColumnDefinitions.Add(col1);
            SeekerGrid.ColumnDefinitions.Add(col2);
            SeekerGrid.ColumnDefinitions.Add(col3);
            Seeker.Width = 250;
        }
    }
}
