# OwnAudioSharp Avalonia Demo

This project is a sample application demonstrating the capabilities of the [OwnAudioSharp](https://github.com/ModernMube/OwnAudioSharp) audio library within an Avalonia MVVM application, utilizing ReactiveUI. The `MainWindowViewModel.cs` contains the core logic for audio processing, playback, effects application, and user interface control.

---

## Features

The main features handled by `MainWindowViewModel` include:

* **Audio Playback:**
    * Loading multiple audio files (supports formats like WAV, FLAC, MP3, AAC, etc. via FFmpeg).
    * Play, Pause, and Stop functionality.
    * Seeking to specific time positions within the audio.
    * Display of total audio duration and current playback position.
* **Pitch and Tempo Control:**
    * Real-time adjustment of pitch and tempo for all loaded audio sources.
* **Volume Control:**
    * Master volume adjustment for the player.
* **Real-time Audio Effects (Output):**
    * Utilizes an `FxProcessor` for applying a chain of custom effects to the output audio.
    * Effects can be dynamically enabled or disabled by the user.
    * Default output effects chain includes:
        * **Equalizer:** A 10-band equalizer with pre-configured bands for general sound shaping.
        * **Enhancer:** An audio enhancer to add brightness and clarity.
        * **Compressor:** A mastering compressor to control dynamics.
        * **DynamicAmp:** A dynamic amplifier to normalize volume levels.
* **Microphone Input & Effects (Input):**
    * Ability to add a microphone input source to the audio mix.
    * A separate `FxProcessor` is used for applying effects to the microphone input.
    * Default input effects chain includes:
        * **Reverb:** Adds reverberation to the input signal.
        * **Delay:** Applies a delay effect.
        * **Compressor:** A vocal compressor optimized for voice.
* **Saving Audio to File:**
    * The processed audio output (including all applied effects) can be saved to a `.wav` file.
* **File Management:**
    * Add audio files to the playback queue using a file picker.
    * Remove the last added audio source (either an output file or the input source).
* **User Interface & Interaction:**
    * The Play/Pause button text dynamically updates to reflect the current player state ("Play" or "Pause").
    * `ObservableCollection` is used for dynamically updating lists of loaded file names and log messages in the UI.
    * `ReactiveCommand` (from ReactiveUI) is used for handling user actions like adding files, playing, stopping, etc.
    * Real-time visualization of left and right output audio levels.
    * Interaction with a waveform display (`MainWindow.Instance.waveformDisplay`) to show audio data and playback position.
* **Logging:**
    * Implements an `ILogger` interface (`LogInfo`, `LogWarning`, `LogError`) to display messages (information, warnings, errors) in an observable collection within the UI. This helps in monitoring the application's status and diagnosing issues.
* **FFmpeg Dependency:**
    * The ViewModel checks if FFmpeg is initialized, as `OwnAudioSharp` relies on it for decoding a wide range of audio formats. Logs an error if FFmpeg is not found or not initialized correctly.

---

## Core Components & Libraries

* **Avalonia:** Used for creating the cross-platform user interface.
* **ReactiveUI:** An MVVM framework that helps in creating elegant, testable, and maintainable code by using a reactive paradigm. Leveraged for commands and property change notifications.
* **OwnAudioSharp:** The core audio library providing functionalities for audio playback, recording, processing, and effects.

---

## How to Use / Key Functionalities Guide

1.  **Adding Audio Files:** Click the "Add File" button to open a file dialog. You can select one or more audio files (`.wav`, `.mp3`, `.flac`, etc.). The file names will appear in the list, and the waveform will be displayed if available.
2.  **Playback Control:**
    * Use the "Play"/"Pause" button to start or pause playback.
    * Use the "Stop" button to halt playback completely.
    * Click on the waveform display (if implemented in the View) to seek to a specific position (the `Seek` method in ViewModel supports this).
3.  **Adjusting Audio Properties:**
    * Modify the "Pitch" slider to change the audio pitch.
    * Modify the "Tempo" slider to change the audio playback speed without affecting pitch.
    * Adjust the "Volume" slider to control the master output volume.
4.  **Using Output Effects:**
    * Check the "Enable FX" checkbox to toggle the entire chain of output audio effects (Equalizer, Enhancer, Compressor, DynamicAmp).
5.  **Using Microphone Input:**
    * Click the "Input" button to add your default microphone as an audio source. Effects like Reverb, Delay, and a Vocal Compressor will be applied to the microphone input automatically.
6.  **Saving Processed Audio:**
    * Check the "Save File" checkbox.
    * Click the "Set Save Path" button to choose a location and name for your output `.wav` file.
    * When you press "Play" with "Save File" enabled and a path set, the output audio (including all effects and mixed sources) will be written to the specified file.
7.  **Removing Sources:**
    * Click the "Remove File" button to remove the most recently added audio source (file or microphone input).
8.  **Resetting:**
    * Click the "Reset" button to stop playback, clear all loaded tracks, reset effects, clear logs, and re-initialize the audio engine to its default state.
9.  **Monitoring Audio Levels:**
    * The "Left Level" and "Right Level" progress bars (or similar UI elements) will show the current output volume for each channel in real-time.
10. **Viewing Logs:**
    * The "Logs" section will display informational messages, warnings, or errors from the audio engine or application logic. This is useful for understanding what the application is doing and for troubleshooting.

---

## Prerequisites

* **.NET SDK:** To build and run the Avalonia application.

---
