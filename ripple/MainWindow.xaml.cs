using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media;
using Windows.Media.Control;
using System.Reflection;
using System.Windows.Media.Animation;
using System.Text.Unicode;
using System.ComponentModel;
using System;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
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

        public MainWindow()
        {
            InitializeComponent();
            this.Topmost = true;

            // Custom window
            //SolidColorBrush brush = new SolidColorBrush()
            //brush.Color = new Color();

            async void InitializeMediaSession()
            {
                try
                {
                    _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                    if (_sessionManager == null) { throw new Exception("Could not initialize session manager"); }

                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

                    this._currentSession = this._sessionManager.GetCurrentSession();
                    this._currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    this._currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                    this._currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

                    // Media properties
                    var mediaProperties = await this._currentSession.TryGetMediaPropertiesAsync() 
                        ?? throw new Exception("Could not initialize media properties");
                    Dispatcher.Invoke(() =>
                    {
                        SongTitleBox.Text = mediaProperties.Title;
                        SongArtistBox.Text = mediaProperties.Artist;
                    });

                    // Playback info and timeline
                    var playbackInfo = this._currentSession.GetPlaybackInfo() 
                        ?? throw new Exception("Could not initialize playback info");
                    this._controls = playbackInfo.Controls;
                    this.UpdateVisibility();

                    var status = (int)playbackInfo.PlaybackStatus;
                    switch (status)
                    {
                        case 4: // play
                                //this._songTimer.Start();
                            Dispatcher.Invoke(() =>
                            {
                                TogglePlayButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                            });
                            break;
                        case 5: // paused
                            Dispatcher.Invoke(() =>
                            {
                                TogglePlayButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                            });
                            break;
                    }

                    var timeline = this._currentSession.GetTimelineProperties() 
                        ?? throw new Exception("Could not initialize session manager");
                    Dispatcher.Invoke(() =>
                    {
                        Seeker.Minimum = timeline.StartTime.TotalSeconds;
                        Seeker.Maximum = timeline.EndTime.TotalSeconds;
                        Seeker.Value = timeline.Position.TotalSeconds;

                        SongStartMin.Text = timeline.Position.Minutes < 10
                            ? timeline.Position.Minutes.ToString()
                            : timeline.Position.Minutes.ToString("D2");
                        SongStartSec.Text = timeline.Position.Seconds.ToString("D2");

                        SongEndMin.Text = timeline.EndTime.Minutes < 10
                            ? timeline.EndTime.Minutes.ToString()
                            : timeline.EndTime.Minutes.ToString("D2");
                        SongEndSec.Text = timeline.EndTime.Seconds.ToString("D2");
                    });

                    // Shuffle and Repeat states
                    this._shuffleState = this._shuffleEnabled;
                    this.RepeatState = playbackInfo.AutoRepeatMode ?? MediaPlaybackAutoRepeatMode.None;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                    this.UpdateVisibility();
                }
            }
            InitializeMediaSession();
        }
        
        private void UpdateVisibility()
        {
            var playbackInfo = this._currentSession?.GetPlaybackInfo();
            this._controls = playbackInfo?.Controls;

            if (this._controls != null)
            {
                this._shuffleEnabled = this._controls.IsShuffleEnabled;
                this._skipPrevEnabled = this._controls.IsPreviousEnabled;
                this._seekEnabled = this._controls.IsPlaybackPositionEnabled;
                this._playPauseEnabled = this._controls.IsPlayPauseToggleEnabled;
                this._skipNextEnabled = this._controls.IsNextEnabled;
                this._repeatEnabled = this._controls.IsRepeatEnabled;
            }
            else
            {
                this._shuffleEnabled = false;
                this._skipPrevEnabled = false;
                this._seekEnabled = false;
                this._skipNextEnabled = false;
                this._repeatEnabled = false;
            }

            Visibility shuffleVis = Visibility.Hidden;
            Visibility skipPrevVis = Visibility.Hidden;
            Visibility playPauseVis = Visibility.Hidden;
            Visibility seekerVis = Visibility.Hidden;
            Visibility skipNextVis = Visibility.Hidden;
            Visibility repeatVis = Visibility.Hidden;

            // Shuffle
            if (this._shuffleEnabled) { shuffleVis = Visibility.Visible; }
            else if (!this._shuffleEnabled) { shuffleVis = Visibility.Hidden; }

            // Skip prev
            if (this._skipPrevEnabled) { skipPrevVis = Visibility.Visible; }
            else if (!this._shuffleEnabled) { skipPrevVis = Visibility.Hidden; }
                
            // Play pause
            if (this._playPauseEnabled) { playPauseVis = Visibility.Visible; }
            else if (!this._playPauseEnabled) { playPauseVis = Visibility.Hidden; }

            // Seeker
            if (this._seekEnabled) { seekerVis = Visibility.Visible; }
            else if (!this._seekEnabled) { seekerVis = Visibility.Hidden; }

            // Skip next
            if (this._skipNextEnabled) { skipNextVis = Visibility.Visible; } 
            else if (!this._skipNextEnabled) { skipNextVis = Visibility.Hidden; }

            // Repeat
            if (this._repeatEnabled) { repeatVis = Visibility.Visible; }
            else if (!this._repeatEnabled) { repeatVis = Visibility.Hidden; }

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
            this.BeginAnimation(LeftProperty, null);
            this.BeginAnimation(TopProperty, null);
            this.Cursor = System.Windows.Input.Cursors.None;
            DragMove();
            this.CheckDock();
            this.Cursor = System.Windows.Input.Cursors.Arrow;
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

            double bottomRightX = bounds.Right - this.Width;
            double bottomRightY = bounds.Bottom - this.Height;
            double topRightX = bounds.Right - this.Width;
            double topRightY = bounds.Top;

            double bottomLeftX = bounds.Left;
            double bottomLeftY = bounds.Bottom - this.Height;
            double topLeftX = bounds.Left;
            double topLeftY = bounds.Top;

            bool closeToRight = Math.Abs((this.Left + this.Width) - bounds.Right) <= thresholdX;
            bool closeToBottom = Math.Abs((this.Top + this.Height) - bounds.Bottom) <= thresholdY;
            bool closeToLeft = Math.Abs(this.Left - bounds.Left) <= thresholdX;
            bool closeToTop = Math.Abs(this.Top - bounds.Top) <= thresholdY;

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
                From = this.Left,
                To = targetX,
                Duration = duration,
                EasingFunction = easing
            };

            DoubleAnimation topMove = new()
            {
                From = this.Top,
                To = targetY,
                Duration = duration,
                EasingFunction = easing
            };

            this.BeginAnimation(LeftProperty, leftMove);
            this.BeginAnimation(TopProperty, topMove);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private bool PingShuffle()
        {
            this._shuffleState = !this._shuffleState;
            return this._shuffleState;
        }

        private MediaPlaybackAutoRepeatMode? PingRepeat()
        {
            //MediaPlaybackAutoRepeatMode mode = (this._repeatState + 1);
            return ++this.RepeatState;
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, object args)
        {
            var newSession = sender.GetCurrentSession() 
                ?? throw new Exception("Could not get media session");
            this._currentSession = newSession;
            this._currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            this._currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            //this._currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            this.UpdateVisibility();
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
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            this.UpdateVisibility();
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            async Task AsyncWorker()
            {
                try
                {
                    var playbackInfo = sender.GetPlaybackInfo();
                    FontAwesome.WPF.FontAwesomeIcon icon = FontAwesome.WPF.FontAwesomeIcon.None;
                    int status = (int)playbackInfo.PlaybackStatus;

                    switch(status) // current status after event
                    {
                        case 4: // playing
                            icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                            break;
                        case 5: // paused
                            icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                            break;
                    }

                    var mediaProperties = await sender.TryGetMediaPropertiesAsync();
                    string title = "Nothing";
                    string artist = "Nobody";
                    if (mediaProperties != null)
                    {
                        title = mediaProperties.Title;
                        artist = mediaProperties.Artist;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        SongTitleBox.Text = title;
                        SongArtistBox.Text = artist;
                        TogglePlayButtonIcon.Icon = icon;
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            _ = AsyncWorker();

            this.UpdateVisibility();
        }

        // TODO: Seeker
        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            var timeline = this._currentSession?.GetTimelineProperties();
            if (timeline != null)
            {
                Dispatcher.Invoke(() =>
                {
                    Seeker.Minimum = timeline.StartTime.TotalSeconds;
                    Seeker.Maximum = timeline.EndTime.TotalSeconds;
                    Seeker.Value = timeline.Position.TotalSeconds;

                    SongStartMin.Text = timeline.Position.Minutes < 10
                        ? timeline.Position.Minutes.ToString()
                        : timeline.Position.Minutes.ToString("D2");
                    SongStartSec.Text = timeline.Position.Seconds.ToString("D2");

                    SongEndMin.Text = timeline.EndTime.Minutes < 10
                        ? timeline.EndTime.Minutes.ToString()
                        : timeline.EndTime.Minutes.ToString("D2");
                    SongEndSec.Text = timeline.EndTime.Seconds.ToString("D2");
                });
            }
        }

        // TODO: Seeker
        //private void Seeker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    async Task AsyncWorker()
        //    {
        //        try
        //        {
        //            await this._currentSession.TryChangePlaybackPositionAsync((long)e.NewValue);
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
                    bool newShuffleState = this.PingShuffle();
                    await this._currentSession?.TryChangeShuffleActiveAsync(newShuffleState);

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
                    await this._currentSession?.TrySkipPreviousAsync();
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
                    await this._currentSession?.TrySkipNextAsync();
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
                    await this._currentSession?.TryTogglePlayPauseAsync();
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
                    MediaPlaybackAutoRepeatMode? mode = this.PingRepeat();
                    if (mode.HasValue)
                    {
                        await this._currentSession?.TryChangeAutoRepeatModeAsync((MediaPlaybackAutoRepeatMode)mode);
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
            this.Height = 100;
            this.Width = 300;

            // Main grid
            this.MainGrid.RowDefinitions.Clear();
            RowDefinition row1 = new() { Height = new GridLength(20) };
            RowDefinition row2 = new() { Height = new GridLength(30) };
            RowDefinition row3 = new() { Height = new GridLength(20) };
            RowDefinition row4 = new() { Height = new GridLength(30) };
            this.MainGrid.RowDefinitions.Add(row1);
            this.MainGrid.RowDefinitions.Add(row2);
            this.MainGrid.RowDefinitions.Add(row3);
            this.MainGrid.RowDefinitions.Add(row4);

            // Title bar
            this.SettingsMenu.Margin = new Thickness(0,0,0,0);
            this.SettingsStack.Width = this.Width;
            this.WindowTitle.FontSize = 11;
            this.WindowTitle.Margin = new Thickness(5,0,0,0);
            this.WindowControlsStack.Margin = new Thickness(0,0,5,0);

            // Metadata
            this.SongTitleBox.FontSize = 12;
            this.SongArtistBox.FontSize = 11;

            // Seeker
            this.SeekerGrid.ColumnDefinitions.Clear();
            ColumnDefinition col1 = new() { Width = new GridLength(50) };
            ColumnDefinition col2 = new() { Width = new GridLength(200) };
            ColumnDefinition col3 = new() { Width = new GridLength(50) };
            this.SeekerGrid.ColumnDefinitions.Add(col1);
            this.SeekerGrid.ColumnDefinitions.Add(col2);
            this.SeekerGrid.ColumnDefinitions.Add(col3);
            this.Seeker.Width = 200;
        }

        private void Small_Click(object sender, RoutedEventArgs e)
        {
            // Window
            this.Height = 150;
            this.Width = 350;

            // Main grid
            this.MainGrid.RowDefinitions.Clear();
            RowDefinition row1 = new() { Height = new GridLength(30) };
            RowDefinition row2 = new() { Height = new GridLength(50) };
            RowDefinition row3 = new() { Height = new GridLength(30) };
            RowDefinition row4 = new() { Height = new GridLength(40) };
            this.MainGrid.RowDefinitions.Add(row1);
            this.MainGrid.RowDefinitions.Add(row2);
            this.MainGrid.RowDefinitions.Add(row3);
            this.MainGrid.RowDefinitions.Add(row4);

            // Title bar
            this.SettingsMenu.Margin = new Thickness(10,0,10,0);
            this.SettingsStack.Width = this.Width;
            this.WindowTitle.FontSize = 12;
            this.WindowTitle.Margin = new Thickness(0,0,0,0);
            this.WindowControlsStack.Margin = new Thickness(0, 0, 10, 0);

            // Metadata
            this.SongTitleBox.FontSize = 14;
            this.SongArtistBox.FontSize = 12;

            // Seeker
            this.SeekerGrid.ColumnDefinitions.Clear();
            ColumnDefinition col1 = new() { Width = new GridLength(50) };
            ColumnDefinition col2 = new() { Width = new GridLength(250) };
            ColumnDefinition col3 = new() { Width = new GridLength(50) };
            this.SeekerGrid.ColumnDefinitions.Add(col1);
            this.SeekerGrid.ColumnDefinitions.Add(col2);
            this.SeekerGrid.ColumnDefinitions.Add(col3);
            this.Seeker.Width = 250;
        }
    }
}