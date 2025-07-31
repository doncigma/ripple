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
        // Media manager
        private readonly RippleMediaManager _mediaManager;

        private class Config
        {
            // Dimensions
            public double windowHeight { get; set; } = 100;
            public double windowWidth { get; set; } = 300;

            // Sizes
            public double smallFont { get; set; } = 11;
            public double bigFont { get; set; } = 12;
            public double margin { get; set; } = 5;
            public double timestampSize { get; set; } = 50;

            // Scales
            public double thinBarScale { get; set; } = 0.2;
            public double thickBarScale { get; set; } = 0.3;
            public double timelineScale { get; set; } = 2.0 / 3.0;
        }

        private readonly Dictionary<string, Config> _configs = new()
        {
            ["Small"] = new Config
            {
                windowHeight = 100,
                windowWidth = 300,
                smallFont = 11,
                bigFont = 12,
                margin = 5,
                timestampSize = 50,
                thinBarScale = 0.2,
                thickBarScale = 0.3,
                timelineScale = 2.0 / 3.0
            },
            ["Medium"] = new Config
            {
                windowHeight = 125,
                windowWidth = 325,
                smallFont = 12,
                bigFont = 13,
                margin = 7,
                timestampSize = 50,
                thinBarScale = 0.2,
                thickBarScale = 0.3,
                timelineScale = 2.0 / 3.0
            },
            ["Large"] = new Config
            {
                windowHeight = 150,
                windowWidth = 350,
                smallFont = 12,
                bigFont = 14,
                margin = 10,
                timestampSize = 50,
                thinBarScale = 0.2,
                thickBarScale = 0.3,
                timelineScale = 2.0 / 3.0
            }
        };

        public MainWindow()
        {
            InitializeComponent();
            Topmost = true;

            // Initialize media manager
            _mediaManager = new RippleMediaManager();
            _mediaManager.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _mediaManager.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _mediaManager.TimelineBoundsChanged += OnTimelineBoundsChanged;
            _mediaManager.TimelinePositionChanged += OnTimelinePositionChanged;

            async Task InitializeManager()
            {
                try
                {
                    await _mediaManager.InitializeAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message + " < InitializeManager()");
                }
                finally
                {
                    CheckDock();
                }
            }
            _ = InitializeManager();
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

        private void Small_Click(object sender, RoutedEventArgs e)
        {
            ApplyWindowSizing(_configs["Small"]);
            CheckDock();
        }

        private void Medium_Click(object sender, RoutedEventArgs e)
        {
            ApplyWindowSizing(_configs["Medium"]);
            CheckDock();
        }

        private void Large_Click(object sender, RoutedEventArgs e)
        {
            ApplyWindowSizing(_configs["Large"]);
            CheckDock();
        }

        private void ApplyWindowSizing(Config config)
        {
            var thinBar = config.windowHeight * config.thinBarScale;
            var thickBar = config.windowHeight * config.thickBarScale;
            var timelineBar = config.windowWidth * config.timelineScale;
            var margin = config.margin;
            var timestamp = config.timestampSize;

            Dispatcher.Invoke(() =>
            {
                Height = config.windowHeight;
                Width = config.windowWidth;

                MainGrid.RowDefinitions.Clear();
                MainGrid.RowDefinitions.Add(new() { Height = new GridLength(thinBar) });
                MainGrid.RowDefinitions.Add(new() { Height = new GridLength(thickBar) });
                MainGrid.RowDefinitions.Add(new() { Height = new GridLength(thinBar) });
                MainGrid.RowDefinitions.Add(new() { Height = new GridLength(thickBar) });

                // Title bar
                SettingsMenu.Margin = new Thickness(margin, 0, 0, 0);
                SettingsStack.Width = Width;
                WindowTitle.FontSize = config.smallFont;
                WindowTitle.Margin = new Thickness(margin, 0, 0, 0);
                WindowControlsStack.Margin = new Thickness(0, 0, margin, 0);

                // Metadata
                SongTitleBox.FontSize = config.bigFont;
                SongArtistBox.FontSize = config.smallFont;

                // Timeline
                TimelineGrid.ColumnDefinitions.Clear();
                TimelineGrid.ColumnDefinitions.Add(new() { Width = new GridLength(timestamp) });
                TimelineGrid.ColumnDefinitions.Add(new() { Width = new GridLength(timelineBar) });
                TimelineGrid.ColumnDefinitions.Add(new() { Width = new GridLength(timestamp) });
                TimelineBar.Width = timelineBar;
            });
        }

        // Playback Events
        private void OnMediaPropertiesChanged(object? sender, MediaPropertiesEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                SongTitleBox.Text = args.Title;
                SongArtistBox.Text = args.Artist;
                MediaThumbnail.Source = args.Thumbnail;
                MediaThumbnail.Visibility = args.Thumbnail != null 
                    ? Visibility.Visible
                    : Visibility.Hidden;
            });
        }

        private void OnPlaybackInfoChanged(object? sender, PlaybackInfoEventArgs args)
        {
            // Get icon and visibilities
            var icon = args.IsPlaying
                ? FontAwesome.WPF.FontAwesomeIcon.Pause
                : FontAwesome.WPF.FontAwesomeIcon.Play;

            Visibility skipPrevVis = args.Controls.IsPreviousEnabled
                ? Visibility.Visible
                : Visibility.Hidden;
            Visibility playPauseVis = args.Controls.IsPlayPauseToggleEnabled
                ? Visibility.Visible
                : Visibility.Hidden;
            Visibility skipNextVis = args.Controls.IsNextEnabled
                ? Visibility.Visible
                : Visibility.Hidden;

            // Update
            Dispatcher.Invoke(() =>
            {
                TogglePlayButtonIcon.Icon = icon;
                SkipPreviousButton.Visibility = skipPrevVis;
                TogglePlayPauseButton.Visibility = playPauseVis;
                SkipNextButton.Visibility = skipNextVis;
            });
        }

        private void OnTimelineBoundsChanged(object? sender, TimelineBoundsEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                TimelineBar.Minimum = args.StartTime;
                TimelineBar.Maximum = args.EndTime;
            });
        }

        private void OnTimelinePositionChanged(object? sender, TimelinePositionEventArgs args)
        {
            double pos = Math.Clamp(args.Position, args.StartTime, args.EndTime);
            //double pos = Math.Clamp(args.Position, TimelineBar.Minimum, TimelineBar.Maximum);

            string startMin = ((int)pos / 60).ToString("D2");
            string startSec = ((int)pos % 60).ToString("D2");

            string endMin = ((int)args.EndTime / 60).ToString("D2");
            string endSec = ((int)args.EndTime % 60).ToString("D2");

            Dispatcher.Invoke(() =>
            {
                TimelineBar.Value = pos;
                SongStartMin.Text = startMin;
                SongStartSec.Text = startSec;
                SongEndMin.Text = endMin;
                SongEndSec.Text = endSec;
            });
        }

        // Playback Controls
        private async void SkipPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            SkipPreviousButton.IsEnabled = false;
            
            try
            {
                await _mediaManager.SkipPreviousAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to skip previous: {ex.Message}");
            }
            finally
            {
                SkipPreviousButton.IsEnabled = true;
            }
        }

        private async void SkipNextButton_Click(object sender, RoutedEventArgs e)
        {
            SkipNextButton.IsEnabled = false;

            try
            {
                await _mediaManager.SkipNextAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to skip next: {ex.Message}");
            }
            finally
            {
                SkipNextButton.IsEnabled = true;
            }
        }

        private async void TogglePlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPauseButton.IsEnabled = false;

            try
            {
                await _mediaManager.TogglePlayPauseAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to toggle play/pause: {ex.Message}");
            }
            finally
            {
                TogglePlayPauseButton.IsEnabled = true;
            }
        }
    }
}
