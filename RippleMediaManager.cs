using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Media.Control;

namespace ripple
{
    public class RippleMediaManager
    {
        // Manager and session
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;

        // Timer to manually track song progress
        private readonly DispatcherTimer _songTimer;

        // Events for UI updates
        public event EventHandler<MediaPropertiesEventArgs>? MediaPropertiesChanged;
        public event EventHandler<PlaybackInfoEventArgs>? PlaybackInfoChanged;
        public event EventHandler<TimelineBoundsEventArgs>? TimelineBoundsChanged;
        public event EventHandler<TimelinePositionEventArgs>? TimelinePositionChanged;

        public RippleMediaManager()
        {
            _songTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            _songTimer.Tick += (_, __) => UpdateTimelinePosition();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Session manager
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_sessionManager == null)
                    throw new Exception("Could not initialize session manager. RippleMediaManager.cs:InitializeAsync()");

                _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

                // Initialize current session
                UpdateCurrentSession();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + " < InitializeAsync()");
            }
        }

        public GlobalSystemMediaTransportControlsSession? GetCurrentSession()
        {
            return _currentSession;
        }

        // Helper methods
        private void UpdateCurrentSession()
        {
            if (_sessionManager == null)
                throw new Exception("Session manager is null. RippleMediaManager.cs:UpdateCurrentSession()");

            var session = _sessionManager.GetCurrentSession()
                ?? throw new Exception("Could not get new session. RippleMediaManager.cs:UpdateCurrentSession()");

            _currentSession = session;
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

            UpdateMediaProps();
            UpdatePlaybackInfo();
            UpdateTimeline();
        }
        
        private void UpdateMediaProps()
        {
            async Task AsyncWorker()
            {
                if (_currentSession == null)
                    throw new Exception("Current session is null. RippleMediaManager.cs:UpdateMediaProps()");

                var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync()
                    ?? throw new Exception("Could not get media properties. RippleMediaManager.cs:UpdateMediaProps()");

                BitmapImage? thumbnailImage = null;
                if (mediaProperties.Thumbnail != null)
                {
                    var stream = await mediaProperties.Thumbnail.OpenReadAsync();

                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream.AsStream();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();

                    thumbnailImage = image;
                }

                MediaPropertiesChanged?.Invoke(this, new MediaPropertiesEventArgs
                {
                    Title = mediaProperties.Title,
                    Artist = mediaProperties.Artist,
                    Thumbnail = thumbnailImage
                });
            }
            _ = AsyncWorker();
        }
        
        private void UpdatePlaybackInfo()
        {
            if (_currentSession == null)
                throw new Exception("Current session is null. RippleMediaManager.cs:UpdatePlaybackInfo()");

            var playbackInfo = _currentSession.GetPlaybackInfo()
                ?? throw new Exception("Could not get playback info. RippleMediaManager.cs:UpdatePlaybackInfo()");

            bool isPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            _songTimer.IsEnabled = isPlaying;

            PlaybackInfoChanged?.Invoke(this, new PlaybackInfoEventArgs
            {
                IsPlaying = isPlaying,
                Controls = playbackInfo.Controls
            });
        }

        private void UpdateTimeline()
        {
            UpdateTimelineBounds();
            UpdateTimelinePosition();
        }
        
        private void UpdateTimelineBounds()
        {
            if (_currentSession == null)
                throw new Exception("Current session is null. RippleMediaManager.cs:UpdateTimelineBounds()");

            var timeline = _currentSession.GetTimelineProperties();

            TimelineBoundsChanged?.Invoke(this, new TimelineBoundsEventArgs
            {
                StartTime = timeline.StartTime.TotalSeconds,
                EndTime = timeline.EndTime.TotalSeconds
            });
        }
        
        private void UpdateTimelinePosition()
        {
            if (_currentSession == null)
                throw new Exception("Current session is null. RippleMediaManager.cs:UpdateTimelinePosition()");

            var timeline = _currentSession.GetTimelineProperties();
            double rate = _currentSession.GetPlaybackInfo().PlaybackRate ?? 1.00;
            double elapsed = (DateTimeOffset.Now - timeline.LastUpdatedTime).TotalSeconds * rate;
            double pos = timeline.Position.TotalSeconds + elapsed;

            TimelinePositionChanged?.Invoke(this, new TimelinePositionEventArgs
            {
                Position = pos,
                StartTime = timeline.StartTime.TotalSeconds,
                EndTime = timeline.EndTime.TotalSeconds
            });
        }
        
        // Event handlers
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

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            try
            {
                UpdatePlaybackInfo();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + " < OnPlaybackInfoChanged()");
            }
        }

        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            try
            {
                UpdateTimeline();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + " < OnTimelinePropertiesChanged()");
            }
        }

        // Playback Controls
        public async Task SkipPreviousAsync()
        {
            if (_currentSession != null)
                await _currentSession.TrySkipPreviousAsync();
        }

        public async Task SkipNextAsync()
        {
            if (_currentSession != null)
                await _currentSession.TrySkipNextAsync();
        }

        public async Task TogglePlayPauseAsync()
        {
            if (_currentSession != null)
                await _currentSession.TryTogglePlayPauseAsync();
        }
    }

    // Event argument classes
    public class MediaPropertiesEventArgs : EventArgs
    {
        public string Title { get; set; } = "Title";
        public string Artist { get; set; } = " Artist";
        public BitmapImage? Thumbnail { get; set; }
    }

    public class PlaybackInfoEventArgs : EventArgs
    {
        public bool IsPlaying { get; set; }
        public GlobalSystemMediaTransportControlsSessionPlaybackControls Controls { get; set; } = null!;
    }

    public class TimelineBoundsEventArgs : EventArgs
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }

    public class TimelinePositionEventArgs : EventArgs
    {
        public double Position { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }
}
