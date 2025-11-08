# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an Avalonia-based desktop application demonstrating the OwnAudioSharp audio library. It's a cross-platform audio player with real-time effects processing, built using the MVVM pattern with ReactiveUI.

## Architecture

### Core Components

- **OwnAudioSharp Integration**: The application depends on three local OwnAudioSharp projects referenced at `../../../Visualcode/OwnAudioSharp/`:
  - `Ownaudio.Core.csproj` - Core audio functionality
  - `Ownaudio.Windows.csproj` - Windows-specific audio engine
  - `Ownaudio.csproj` - Main library

- **Audio Engine Initialization**: The `OwnAudioEngine` is initialized in `Program.cs:19` and must be freed on exit (`Program.cs:28`). The engine handles low-level audio processing and FFmpeg integration.

- **SourceManager Pattern**: `MainWindowViewModel.cs:358` uses a singleton `SourceManager.Instance` that manages:
  - Output audio sources (files)
  - Input audio sources (microphone)
  - Custom sample processors for effects chains
  - Playback state and position tracking

### Effects Processing Architecture

The application uses a dual-processor architecture for audio effects:

1. **Output Effects** (`MainWindowViewModel.cs:436-521`):
   - Uses `FxProcessor` class (defined in `Processor/FxProcessor.cs`) as a chain-of-responsibility pattern
   - Default chain: 30-band Equalizer → Compressor → Enhancer → DynamicAmp
   - Applied to all output audio (mixed playback)

2. **Input Effects** (`MainWindowViewModel.cs:527-559`):
   - Separate `FxProcessor` for microphone input
   - Default chain: Reverb → Delay → Vocal Compressor
   - Each input source gets its own `CustomSampleProcessor`

The `FxProcessor` class processes audio by sequentially applying each `SampleProcessorBase` effect in the chain to the audio samples.

### UI/ViewModel Communication

- **Waveform Display**: MainWindow maintains a singleton instance (`MainWindow.Instance`) accessed by ViewModel for waveform visualization (`MainWindowViewModel.cs:617`)
- **Logging**: ViewModel implements `ILogger` interface, dispatching log messages to UI thread via `ObservableCollection<Log>` (`MainWindowViewModel.cs:382-403`)
- **Real-time Updates**: 80ms timer updates output level meters (`MainWindowViewModel.cs:118`)

## Common Development Commands

### Building
```bash
dotnet build
```

### Running
```bash
dotnet run
```

### Building for Release
```bash
dotnet build -c Release
```

## Key Technical Details

- **Target Framework**: .NET 8.0 (Windows only due to `OutputType>WinExe`)
- **Compiled Bindings**: Avalonia uses compiled bindings by default (`AvaloniaUseCompiledBindingsByDefault`)
- **Audio Formats**: Supports WAV, FLAC, MP3, AAC, AIFF, MP4, M4A, OGG, WMA, WebM via FFmpeg
- **Sample Processor Pattern**: All effects inherit from `SampleProcessorBase` and process `Span<float>` audio samples in-place
- **Engine Configuration**: Frame buffer size set at `MainWindowViewModel.cs:356` (1024 frames)

## Important Implementation Notes

- The SourceManager requires proper initialization before use - check `_player` for null throughout
- Input sources require FFmpeg to be initialized (`_isFFmpegInitialized` flag)
- All audio processing happens in-place on `Span<float>` buffers for performance
- UI updates must be dispatched to the UI thread via `Dispatcher.UIThread.InvokeAsync()`
- When saving audio, the file path must be set and `IsSaveFile` enabled before calling Play
- Seeking is disabled during seek operations (`_player.IsSeeking` check in `MainWindowViewModel.cs:796`)
