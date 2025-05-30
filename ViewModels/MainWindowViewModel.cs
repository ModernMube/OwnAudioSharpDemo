using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;

using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Ownaudio;
using Ownaudio.Sources;
using Ownaudio.Engines;
using Ownaudio.Common;

using OwnaAvalonia.Models;
using OwnaAvalonia.Views;
using OwnaAvalonia.Processor;
using Ownaudio.Fx;

namespace OwnaAvalonia.ViewModels
{
    /// <summary>
    /// Main view model for the audio player application window
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, ILogger
    {
        #region Private Fields

        private int _trackNumber = 0;
        private SourceManager? _player;
        private bool _isStopRequested = true;
        private bool _isFFmpegInitialized;
        private int _sourceOutputId = -1;
        private FxProcessor _Fxprocessor;
        private FxProcessor _inputFxprocessor;
        private DispatcherTimer _timer;

        #endregion

        #region Reactive Commands

        /// <summary>
        /// Command to add audio files to the player
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddFileCommand { get; }

        /// <summary>
        /// Command to remove the last added audio file
        /// </summary>
        public ReactiveCommand<Unit, Unit> RemoveFileCommand { get; }

        /// <summary>
        /// Command to reset the player to initial state
        /// </summary>
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }

        /// <summary>
        /// Command to add microphone input source
        /// </summary>
        public ReactiveCommand<Unit, Unit> InputCommand { get; }

        /// <summary>
        /// Command to toggle play/pause state
        /// </summary>
        public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }

        /// <summary>
        /// Command to stop playback
        /// </summary>
        public ReactiveCommand<Unit, Unit> StopCommand { get; }

        /// <summary>
        /// Command to select save file path
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveFilePathCommand { get; }

        #endregion

        #region Bindable Properties

        private float _pitch = 0.0f;
        /// <summary>
        /// Gets or sets the pitch adjustment value for all audio sources
        /// </summary>
        public float Pitch
        {
            get => _pitch;
            set
            {
                this.RaiseAndSetIfChanged(ref _pitch, value);
                for (int i = 0; i < _player?.Sources.Count; i++)
                {
                    _player.SetPitch(i, value);
                }
            }
        }

        private float _tempo = 0.0f;
        /// <summary>
        /// Gets or sets the tempo adjustment value for all audio sources
        /// </summary>
        public float Tempo
        {
            get => _tempo;
            set
            {
                this.RaiseAndSetIfChanged(ref _tempo, value);
                for (int i = 0; i < _player?.Sources.Count; i++)
                {
                    _player.SetTempo(i, value);
                }
            }
        }

        private float _volume = 100.0f;
        /// <summary>
        /// Gets or sets the volume level (0-100)
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                this.RaiseAndSetIfChanged(ref _volume, value);
                if (_player is not null)
                { 
                    _player.Volume = value / 100; 
                }
            }
        }

        private TimeSpan _duration;
        /// <summary>
        /// Gets or sets the total duration of the current audio
        /// </summary>
        public TimeSpan Duration 
        { 
            get => _duration; 
            set => this.RaiseAndSetIfChanged(ref _duration, value); 
        }

        private TimeSpan _position;
        /// <summary>
        /// Gets or sets the current playback position
        /// </summary>
        public TimeSpan Position 
        { 
            get => _position; 
            set => this.RaiseAndSetIfChanged(ref _position, value); 
        }

        private bool _isSaveFile;
        /// <summary>
        /// Gets or sets whether to save the output to a file
        /// </summary>
        public bool IsSaveFile 
        { 
            get => _isSaveFile; 
            set 
            { 
                this.RaiseAndSetIfChanged(ref _isSaveFile, value); 
                SaveFilePath = ""; 
            } 
        }

        private bool _isFxEnabled = false;
        /// <summary>
        /// Gets or sets whether audio effects are enabled
        /// </summary>
        public bool IsFxEnabled
        {
            get => _isFxEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _isFxEnabled, value);
                _Fxprocessor.IsEnabled = value;
            }
        }

        private string? _saveFilePath;
        /// <summary>
        /// Gets or sets the path where the audio file will be saved
        /// </summary>
        public string? SaveFilePath 
        { 
            get => _saveFilePath; 
            set => this.RaiseAndSetIfChanged(ref _saveFilePath, value); 
        }

        private string? _playPauseText = "Play";
        /// <summary>
        /// Gets or sets the text displayed on the play/pause button
        /// </summary>
        public string? PlayPauseText 
        { 
            get => _playPauseText; 
            set => this.RaiseAndSetIfChanged(ref _playPauseText, value); 
        }

        /// <summary>
        /// Gets the collection of loaded audio file names
        /// </summary>
        public ObservableCollection<string> FileNames { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the collection of log messages
        /// </summary>
        public ObservableCollection<Log> Logs { get; } = new ObservableCollection<Log>();

        private double _leftLevel;
        /// <summary>
        /// Gets or sets the left channel audio level (0-100)
        /// </summary>
        public double LeftLevel
        {
            get => _leftLevel; 
            set => this.RaiseAndSetIfChanged(ref _leftLevel, value);
        }

        private double _rightLevel;
        /// <summary>
        /// Gets or sets the right channel audio level (0-100)
        /// </summary>
        public double RightLevel
        {
            get => _rightLevel;
            set => this.RaiseAndSetIfChanged(ref _rightLevel, value);
        }

        #endregion

        #region File Picker Options

        /// <summary>
        /// File picker options for saving audio files
        /// </summary>
        private FilePickerSaveOptions options = new FilePickerSaveOptions
        {
            Title = "Save Your Audio File",
            SuggestedFileName = "OwnAudioSaveFile.wav",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Wave File")
                {
                    Patterns = new[] { "*.wav" }
                }
            }
        };

        /// <summary>
        /// Supported audio file types for opening
        /// </summary>
        private FilePickerFileType _audioFiles { get; } = new("Audio File")
        {
            Patterns = new[] { "*.wav", "*.flac", "*.mp3", "*.aac", "*.aiff", "*.mp4", "*.m4a", "*.ogg", "*.wma", "*.webm" },
            AppleUniformTypeIdentifiers = new[] { "public.audio" },
            MimeTypes = new[] { "audio/wav", "audio/flac", "audio/mpeg", "audio/aac", "audio/aiff", "audio/mp4", "audio/ogg", "audio/x-ms-wma", "audio/webm" }
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class
        /// </summary>
        public MainWindowViewModel()
        {
            // Initialize reactive commands
            AddFileCommand = ReactiveCommand.Create(addFileCommand);
            RemoveFileCommand = ReactiveCommand.Create(removeFileCommand);
            ResetCommand = ReactiveCommand.Create(resetCommand);
            InputCommand = ReactiveCommand.Create(inputCommand);
            PlayPauseCommand = ReactiveCommand.Create(playPauseCommand);
            StopCommand = ReactiveCommand.Create(stopCommand);
            SaveFilePathCommand = ReactiveCommand.Create(saveFilePathCommand);

            // Initialize effects processors
            _Fxprocessor = new FxProcessor() { IsEnabled = IsFxEnabled };
            _inputFxprocessor = new FxProcessor() { IsEnabled = true };

            // Initialize audio engine
            AudioEngineInitialize();
            
            // Start level monitoring timer
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 80), DispatcherPriority.Normal, new EventHandler(_outputLevel));
            _timer.Start();
        }

        #endregion

        #region Audio Engine Initialization

        /// <summary>
        /// Initializes the audio engine with default settings for input and output
        /// </summary>
        private void AudioEngineInitialize()
        {
            AudioEngineOutputOptions _audioEngineOptions = new AudioEngineOutputOptions
            (
                device: OwnAudio.DefaultOutputDevice,
                channels: OwnAudioEngine.EngineChannels.Stereo,
                sampleRate: OwnAudio.DefaultOutputDevice.DefaultSampleRate,
                latency: OwnAudio.DefaultOutputDevice.DefaultHighOutputLatency
            );

            AudioEngineInputOptions _audioInputOptions = new AudioEngineInputOptions
            (
                device: OwnAudio.DefaultInputDevice,
                channels: OwnAudioEngine.EngineChannels.Mono,
                sampleRate: OwnAudio.DefaultInputDevice.DefaultSampleRate,
                latency: OwnAudio.DefaultInputDevice.DefaultLowInputLatency
            );

            SourceManager.OutputEngineOptions = _audioEngineOptions;
            SourceManager.InputEngineOptions = _audioInputOptions;
            SourceManager.EngineFramesPerBuffer = 512;

            _player = SourceManager.Instance;

            _player.CustomSampleProcessor = _Fxprocessor;
            add_FXprocessor();

            _player.Logger = this;

            _player.StateChanged += OnStateChanged;
            _player.PositionChanged += OnPositionChanged;

            _isFFmpegInitialized = OwnAudio.IsFFmpegInitialized;

            if (!_isFFmpegInitialized)
            {
                LogError($"Decoder not initialized!");
                LogError($"Wrong file path: {OwnAudio.LibraryPath}");
                LogWarning("The decoder will be miniaudio.");
            }
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs an informational message to the application's log collection
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogInfo(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() => Logs.Add(new Log(message, Log.LogType.Info)));
        }

        /// <summary>
        /// Logs a warning message to the application's log collection
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public void LogWarning(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() => Logs.Add(new Log(message, Log.LogType.Warning)));
        }

        /// <summary>
        /// Logs an error message to the application's log collection
        /// </summary>
        /// <param name="message">The error message to log</param>
        public void LogError(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() => Logs.Add(new Log(message, Log.LogType.Error)));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Seeks to a specific position in the currently playing audio
        /// </summary>
        /// <param name="ms">The position in milliseconds to seek to</param>
        public void Seek(double ms)
        {
            if (_player is not null && _player.IsLoaded)
            {
                _player.Seek(TimeSpan.FromMilliseconds(ms));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the output level meters
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void _outputLevel(object? sender, EventArgs e)
        {
            if(_player is not null)
            {
                LeftLevel = (double)_player.OutputLevels.left * 100;
                RightLevel = (double)_player.OutputLevels.right * 100;
            }
        }

        /// <summary>
        /// Adds various audio effects to the output processor chain
        /// </summary>
        private void add_FXprocessor()
        {
            // Create and configure equalizer with 10 bands
            // Emphasizes highs, removes unnecessary lows, and cleans up midrange
            Equalizer _equalizer = new Equalizer((float)SourceManager.OutputEngineOptions.SampleRate);

            _equalizer.SetBandGain(band: 0, frequency: 50, q: 0.7f, gainDB: 1.2f);    // 50 Hz Sub-bass - Slight emphasis on deep bass
            _equalizer.SetBandGain(band: 1, frequency: 60, q: 0.8f, gainDB: -1.0f);   // 60 Hz Low bass - Slight cut for cleaner sound
            _equalizer.SetBandGain(band: 2, frequency: 120, q: 1.0f, gainDB: 0.8f);   // 120 Hz Upper bass - Small emphasis for "punch"
            _equalizer.SetBandGain(band: 3, frequency: 250, q: 1.2f, gainDB: -2.0f);  // 250 Hz Low mids - Slight cut to avoid "muddy" sound
            _equalizer.SetBandGain(band: 4, frequency: 500, q: 1.4f, gainDB: -1.5f);  // 500 Hz Middle - Small cut for clearer vocals
            _equalizer.SetBandGain(band: 5, frequency: 2000, q: 1.0f, gainDB: -0.5f); // 2 kHz Upper mids - Slight emphasis for vocal presence
            _equalizer.SetBandGain(band: 6, frequency: 4000, q: 1.2f, gainDB: 0.6f);  // 4 kHz Presence - Emphasis for details
            _equalizer.SetBandGain(band: 7, frequency: 6000, q: 1.0f, gainDB: 0.3f);  // 6 kHz High mids - Adding airiness
            _equalizer.SetBandGain(band: 8, frequency: 10000, q: 0.8f, gainDB: 0.8f); // 10 kHz Highs - Shimmer
            _equalizer.SetBandGain(band: 9, frequency: 16000, q: 0.7f, gainDB: 0.8f); // 16 kHz Air band - Extra brightness

            // Create mastering compressor
            Compressor _compressor = new Compressor
            (
                threshold: 0.5f,    // -6 dB
                ratio: 4.0f,        // 4:1 compression ratio
                attackTime: 100f,   // 100 ms
                releaseTime: 200f,  // 200 ms
                makeupGain: 1.0f,   // 0 dB
                sampleRate: SourceManager.OutputEngineOptions.SampleRate
            );

            // Create mastering enhancer
            Enhancer _enhancer = new Enhancer
            (
                mix: 0.2f,          // 20% of the original signal is mixed back
                cutFreq: 4000.0f,   // High-pass cutoff 4000 Hz
                gain: 2.5f,         // Pre-saturation amplification 2.5x
                sampleRate: SourceManager.OutputEngineOptions.SampleRate
            );

            // Create dynamic amplifier for consistent volume levels
            DynamicAmp _dynamicAmp = new DynamicAmp(
                targetLevel: -6.0f,           // Target Level -6 dB
                attackTimeSeconds: 0.2f,      // Slower attack for more transparent sound
                releaseTimeSeconds: 0.8f,     // Longer release to avoid pumping
                noiseThreshold: 0.0005f,      // Low noise threshold for handling quiet areas
                maxGainValue: 0.85f,          // Maximum gain to avoid excessive noise
                sampleRateHz: SourceManager.OutputEngineOptions.SampleRate,
                rmsWindowSeconds: 0.5f        // Longer window for more stable RMS calculation
            );

            // Add effects to the processor chain
            _Fxprocessor.AddFx(_equalizer);
            _Fxprocessor.AddFx(_enhancer);
            _Fxprocessor.AddFx(_compressor);
            _Fxprocessor.AddFx(_dynamicAmp);
        }

        /// <summary>
        /// Adds effects to the input processor chain for microphone input
        /// </summary>
        private void add_inputFxprocessor()
        {
            // Create reverb effect for spatial enhancement
            Reverb _reverb = new Reverb
            (
                size: 0.45f,        // Medium space, long reverb tail
                damp: 0.45f,        // Moderate high frequency damping
                wet: 0.25f,         // 25% effect - not too much reverb
                dry: 0.75f,         // 75% dry signal - vocal intelligibility is maintained
                stereoWidth: 0.8f,  // Good stereo space, but not too wide
                sampleRate: SourceManager.OutputEngineOptions.SampleRate
            );

            // Create delay effect for depth
            Delay _delay = new Delay
            (
                time: 310,      // Delay time 310 ms
                repeat: 0.4f,   // Rate of delayed signal feedback to the input 40%
                mix: 0.15f,     // Delayed signal ratio in the mix 15%
                sampleRate: SourceManager.OutputEngineOptions.SampleRate
            );

            // Create vocal compressor for consistent levels
            Compressor _vocalCompressor = new Compressor
            (
                threshold: 0.25f,   // -12 dB - adjusted to human voice average dynamic range
                ratio: 3.0f,        // 3:1 - natural, musical compression
                attackTime: 10f,    // 10 ms - fast enough to catch transients
                releaseTime: 100f,  // 100 ms - follows natural vocal decay
                makeupGain: 2.0f    // +6 dB - compensates for compression
            );

            // Add effects to the input processor chain
            _inputFxprocessor.AddFx(_reverb);
            _inputFxprocessor.AddFx(_delay);
            _inputFxprocessor.AddFx(_vocalCompressor);
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// Handles the file selection process to add audio tracks to the player
        /// </summary>
        private async void addFileCommand()
        {
            if (MainWindow.Instance != null && _isStopRequested)
            {
                IReadOnlyList<IStorageFile> result = await MainWindow.Instance.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Select audio file",
                    AllowMultiple = true,
                    FileTypeFilter = new FilePickerFileType[] { _audioFiles }
                });

                string? referenceFile;
                if (result.Count > 0)
                {
                    foreach (var file in result)
                    {
                        referenceFile = file.TryGetLocalPath();
                        if (referenceFile is not null)
                        {
                            if (_player is not null && !await _player.AddOutputSource(referenceFile))
                            {
                                return;
                            }

                            if(_player is not null)
                                MainWindow.Instance.waveformDisplay.SetAudioData(_player.Sources[_sourceOutputId + 1].GetFloatAudioData(TimeSpan.Zero));

                            FileNames.Add(String.Format("track{0}: {1}", (_trackNumber++).ToString(), referenceFile));
                            _sourceOutputId++;
                        }
                    }

                    if (_player is not null)
                    {
                        Duration = _player.Duration;
                        Position = TimeSpan.Zero;
                    }

                    // Reset pitch and tempo for all sources
                    for (int i = 0; i < _player?.Sources.Count; i++)
                    {
                        _player.SetTempo(i, 0);
                        _player.SetPitch(i, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the last added audio source from the player
        /// </summary>
        private void removeFileCommand()
        {
            if (FileNames.Count > 0 && _isStopRequested)
            {
                if (FileNames[FileNames.Count - 1].Contains("input source"))
                {
                    _player?.RemoveInputSource();
                }
                else
                {
                    _player?.RemoveOutputSource(_sourceOutputId);
                    _sourceOutputId--;
                }

                FileNames.RemoveAt(FileNames.Count - 1);
                _trackNumber--;
            }
        }

        /// <summary>
        /// Adds a microphone input source to the player
        /// </summary>
        private void inputCommand()
        {
            if (_isStopRequested && _isFFmpegInitialized)
            {
                _player?.AddInputSource(0.3f);
                FileNames.Add("Add new input source");
            }
        }

        /// <summary>
        /// Opens a file save dialog to select where to save the processed audio
        /// </summary>
        private async void saveFilePathCommand()
        {
            if (MainWindow.Instance != null)
            {
                var result = await MainWindow.Instance.StorageProvider.SaveFilePickerAsync(options);
                if (result != null)
                {
                    SaveFilePath = result.TryGetLocalPath();
                }
            }
        }

        /// <summary>
        /// Toggles between play and pause states for the audio player
        /// </summary>
        private void playPauseCommand()
        {
            if (_player is not null && !_player.IsLoaded)
                return;

            _isStopRequested = false;
            if (_player is not null)
            {
                _player.IsWriteData = IsSaveFile;

                // Handle input source if recording is enabled
                if (_player.IsRecorded)
                {
                    if (_player.AddInputSource(inputVolume: 1.0f).Result)
                    {
                        SourceManager.Instance.SourcesInput[0].CustomSampleProcessor = _inputFxprocessor;
                        add_inputFxprocessor();
                    }
                }
            }

            // Toggle play/pause state
            if (_player?.State is SourceState.Paused or SourceState.Idle)
            {
                if (IsSaveFile && SaveFilePath is not null)
                    _player.Play(SaveFilePath, 16);
                else
                    _player.Play();
            }
            else
            {
                _player?.Pause();
            }
        }

        /// <summary>
        /// Stops playback and recording of audio
        /// </summary>
        private void stopCommand()
        {
            _isStopRequested = true;
            _player?.Stop();
        }

        /// <summary>
        /// Resets the player state, clears all sources and logs
        /// </summary>
        private void resetCommand()
        {
            if (!_isStopRequested)
                _player?.Stop();

            if(_player is not null && _player.Reset())
            {
                FileNames.Clear();
                Logs.Clear();

                Pitch = 0;
                Tempo = 0;
                Volume = 100;
                _sourceOutputId = -1;

                if(MainWindow.Instance is not null)
                    MainWindow.Instance.waveformDisplay.SetAudioData(new float[] { });

                AudioEngineInitialize();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles UI updates when the player state changes between playing, paused, etc.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void OnStateChanged(object? sender, EventArgs e)
        {
            if (_player is not null)
                PlayPauseText = _player.State is SourceState.Playing or SourceState.Buffering ? "Pause" : "Play";
        }

        /// <summary>
        /// Handles position updates from the audio player to keep the UI in sync
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void OnPositionChanged(object? sender, EventArgs e)
        {
            if (_player is not null && _player.IsSeeking)
            {
                return;
            }

            if (_player is not null && ((_player.Position - Position).TotalSeconds > 1 || Position > _player.Position))
            {
                Position = _player.Position;
                if(MainWindow.Instance is not null)
                    Dispatcher.UIThread.InvokeAsync(() => MainWindow.Instance.waveformDisplay.PlaybackPosition = Position.TotalSeconds / Duration.TotalSeconds);
            }

            if (!_isStopRequested && Position == TimeSpan.Zero)
                _isStopRequested = true;
        }

        #endregion
    }
}