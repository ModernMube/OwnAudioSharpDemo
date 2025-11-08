# OwnAudio API Reference

## Tartalomjegyzék

1. [Bevezetés](#bevezetés)
2. [Fő komponensek](#fő-komponensek)
3. [ISource Interface](#isource-interface)
4. [Source osztály](#source-osztály)
5. [SourceInput osztály](#sourceinput-osztály)
6. [SourceSound osztály](#sourcesound-osztály)
7. [SourceSpark osztály](#sourcespark-osztály)
8. [SourceManager osztály](#sourcemanager-osztály)
9. [SourceStateEnum](#sourcestate-enum)
10. [Példakódok](#példakódok)

---

## Bevezetés

Az OwnAudio egy platformfüggetlen audio keretrendszer, amely több audió forrás kezelését, keverését és lejátszását támogatja. Az API egyszerű interfészt biztosít audio fájlok betöltéséhez, valós idejű audio feldolgozáshoz és többcsatornás keveréshez.

---

## Fő komponensek

- **ISource**: Alapvető interface minden audio forráshoz
- **Source**: Fájl alapú audio lejátszás
- **SourceInput**: Audio bemenet (mikrofon) kezelése
- **SourceSound**: Valós idejű sample-alapú audio
- **SourceSpark**: Gyors betöltésű hangeffektek
- **SourceManager**: Singleton osztály a források kezeléséhez és keveréséhez

---

## ISource Interface

Az összes audio forrás alapinterfész.

### Properties

```csharp
TimeSpan Duration { get; }           // Total audio duration
TimeSpan Position { get; }           // Current playback position
SourceState State { get; }           // Current state (Idle/Playing/Paused/Buffering)
bool IsSeeking { get; set; }         // Is seeking in progress
float Volume { get; set; }           // Volume (0.0 - 1.0)
double Pitch { get; set; }           // Pitch adjustment in semitones
double Tempo { get; set; }           // Tempo adjustment as percentage
string? Name { get; set; }           // Source identifier name
(float, float)? OutputLevels { get; } // Stereo output levels (L, R)
```

### Events

```csharp
event EventHandler StateChanged;     // Triggered when state changes
event EventHandler PositionChanged;  // Triggered when position changes
```

### Methods

```csharp
void Seek(TimeSpan position);
void ChangeState(SourceState state);
byte[] GetByteAudioData(TimeSpan position);
float[] GetFloatAudioData(TimeSpan position);
```

---

## Source osztály

Fájl alapú audio forrás teljes funkcionalitással.

### Constructor

```csharp
public Source()
```

### Methods

#### LoadAsync
```csharp
public Task<bool> LoadAsync(string url)
public Task<bool> LoadAsync(Stream stream)
```

Betölt egy audio fájlt URL-ről vagy streamből.

**Példa:**
```csharp
var source = new Source();
await source.LoadAsync("music.mp3");
```

#### Play, Pause, Stop
```csharp
protected void Play()
protected void Pause()
protected void Stop()
```

Playback control methods (internal use, typically called via ChangeState).

#### Seek
```csharp
public void Seek(TimeSpan position)
```

Adott pozícióra ugrik az audio streamben.

**Példa:**
```csharp
source.Seek(TimeSpan.FromSeconds(30)); // Jump to 30s
```

### Properties

```csharp
public bool IsLoaded { get; }
public string? CurrentUrl { get; }
public IAudioDecoder? CurrentDecoder { get; set; }
```

### Példa: Teljes használat

```csharp
var source = new Source
{
    Name = "MainTrack",
    Volume = 0.8f,
    Logger = myLogger
};

await source.LoadAsync("song.mp3");

source.StateChanged += (s, e) => 
{
    Console.WriteLine($"State: {source.State}");
};

source.ChangeState(SourceState.Playing);
await Task.Delay(5000);
source.Seek(TimeSpan.FromSeconds(10));
```

---

## SourceInput osztály

Audio bemenet (mikrofon) kezelése.

### Constructor

```csharp
public SourceInput(AudioConfig inOptions)
```

### Methods

```csharp
public void ReceivesData(out float[] recData, IAudioEngine? Engine)
```

Audio adatok fogadása a bemenetről.

### Példa

```csharp
var inputConfig = new AudioConfig 
{ 
    SampleRate = 44100, 
    Channels = AudioChannels.Stereo 
};

var inputSource = new SourceInput(inputConfig)
{
    Name = "Microphone",
    Volume = 0.7f
};

// Typically managed by SourceManager
```

---

## SourceSound osztály

Valós idejű sample-alapú audio forrás külső adatok beküldésére.

### Constructor

```csharp
public SourceSound(int inputDataChannels = 1)
```

### Methods

#### SubmitSamples
```csharp
public void SubmitSamples(float[] samples)
```

Audio sample-ök beküldése valós időben.

**Példa:**
```csharp
var realtimeSource = new SourceSound(2) // Stereo
{
    Name = "Synthesizer",
    Volume = 1.0f
};

// Generate and submit audio samples
float[] samples = GenerateAudioSamples(512);
realtimeSource.SubmitSamples(samples);
```

### Példa: Audio generálás

```csharp
var soundSource = new SourceSound(2);
soundSource.ChangeState(SourceState.Playing);

// Generate 440Hz sine wave
int sampleRate = 44100;
int frames = 512;
float[] buffer = new float[frames * 2]; // Stereo

for (int i = 0; i < frames; i++)
{
    float sample = (float)Math.Sin(2 * Math.PI * 440 * i / sampleRate);
    buffer[i * 2] = sample;     // Left
    buffer[i * 2 + 1] = sample; // Right
}

soundSource.SubmitSamples(buffer);
```

---

## SourceSpark osztály

Gyors betöltésű hangeffektek (teljes memóriában).

### Constructor

```csharp
public SourceSpark()
public SourceSpark(string filePath, bool looping = false)
```

### Methods

```csharp
public Task<bool> LoadAsync(string url)
public void Play()
public void Stop()
public void Pause()
public void Resume()
```

### Properties

```csharp
public bool IsLooping { get; set; }
public bool IsPlaying { get; }
public bool HasFinished { get; }
```

### Példa

```csharp
// Simple sound effect
var sparkSource = new SourceSpark("beep.wav", looping: false)
{
    Volume = 0.5f
};

sparkSource.Play();

// Or with looping
var loopingEffect = new SourceSpark("background.wav", looping: true);
loopingEffect.Play();
```

---

## SourceManager osztály

Singleton osztály több audio forrás kezeléséhez és keveréséhez.

### Instance

```csharp
public static SourceManager Instance { get; }
```

### Engine Configuration

```csharp
public static AudioConfig OutputEngineOptions { get; set; }
public static AudioConfig InputEngineOptions { get; set; }
public static int EngineFramesPerBuffer { get; set; }
```

### Collections

```csharp
public List<ISource> Sources { get; }          // Output sources
public List<ISource> SourcesInput { get; }     // Input sources
public List<SourceSpark> SourcesSpark { get; } // Spark sources
```

### Methods

#### AddOutputSource
```csharp
public Task<bool> AddOutputSource(string url, string? name = "Output")
```

Output forrás hozzáadása (max 10).

**Példa:**
```csharp
var manager = SourceManager.Instance;
await manager.AddOutputSource("track1.mp3", "Track1");
await manager.AddOutputSource("track2.mp3", "Track2");
```

#### AddInputSource
```csharp
public Task<bool> AddInputSource(float inputVolume = 0f, string? name = "Input")
```

Input forrás hozzáadása (max 1).

#### AddRealTimeSource
```csharp
public SourceSound AddRealTimeSource(float initialVolume = 1.0f, int dataChannels = 2, string? name = "Realtime")
```

Valós idejű forrás hozzáadása.

#### AddSparkSource
```csharp
public SourceSpark AddSparkSource(string filePath, bool looping = false, float volume = 1.0f)
```

Spark forrás hozzáadása.

#### Play, Pause, Stop
```csharp
public void Play()
public void Play(string fileName, short bitPerSamples)
public void Pause()
public void Stop()
```

**Példa felvétellel:**
```csharp
// Play and record output to file
manager.Play("output.wav", 16); // 16-bit audio
```

#### Seek
```csharp
public void Seek(TimeSpan position)
```

Minden forrás szinkronizált pozicionálása.

#### RemoveOutputSource
```csharp
public Task<bool> RemoveOutputSource(int SourceID)
```

#### Reset
```csharp
public bool Reset(bool resetGlobalSettings = false)
public bool ResetSourceManager()
public bool ResetAll()
```

Teljes rendszer visszaállítása.

### Properties

```csharp
public bool IsLoaded { get; }
public bool IsRecorded { get; }
public TimeSpan Duration { get; }
public TimeSpan Position { get; }
public SourceState State { get; }
public float Volume { get; set; }
public (float left, float right) OutputLevels { get; }
public (float left, float right) InputLevels { get; }
```

### Indexer

```csharp
public ISource this[string name] { get; }
```

**Példa:**
```csharp
var track = manager["Track1"];
track.Volume = 0.5f;
```

---

## SourceState Enum

```csharp
public enum SourceState
{
    Idle,       // Not playing
    Playing,    // Currently playing
    Buffering,  // Buffering data
    Paused,     // Paused
    Recording   // Recording audio
}
```

---

## Példakódok

### 1. Egyszerű lejátszás

```csharp
var manager = SourceManager.Instance;
SourceManager.OutputEngineOptions = new AudioConfig
{
    SampleRate = 44100,
    Channels = AudioChannels.Stereo
};

await manager.AddOutputSource("music.mp3", "Music");
manager.Play();

await Task.Delay(10000);
manager.Stop();
```

### 2. Többcsatornás keverés

```csharp
var manager = SourceManager.Instance;

// Add multiple tracks
await manager.AddOutputSource("vocals.mp3", "Vocals");
await manager.AddOutputSource("drums.mp3", "Drums");
await manager.AddOutputSource("bass.mp3", "Bass");

// Adjust individual volumes
manager["Vocals"].Volume = 1.0f;
manager["Drums"].Volume = 0.8f;
manager["Bass"].Volume = 0.6f;

// Start playback
manager.Play();
```

### 3. Audio felvétel mikrofon bemenettel

```csharp
var manager = SourceManager.Instance;

// Add input source
await manager.AddInputSource(1.0f, "Microphone");

// Add backing track
await manager.AddOutputSource("backing.mp3", "Backing");

// Record mixed output
manager.Play("recording.wav", 16);

await Task.Delay(30000); // Record 30 seconds
manager.Stop();
```

### 4. Valós idejű audio generálás

```csharp
var manager = SourceManager.Instance;
var soundSource = manager.AddRealTimeSource(1.0f, 2, "Generator");

manager.Play();

// Generate audio in loop
while (true)
{
    float[] samples = GenerateSineWave(440, 512);
    soundSource.SubmitSamples(samples);
    await Task.Delay(10);
}

float[] GenerateSineWave(float frequency, int frames)
{
    float[] buffer = new float[frames * 2];
    for (int i = 0; i < frames; i++)
    {
        float sample = (float)Math.Sin(2 * Math.PI * frequency * i / 44100);
        buffer[i * 2] = sample;
        buffer[i * 2 + 1] = sample;
    }
    return buffer;
}
```

### 5. Hangeffektek kezelése

```csharp
var manager = SourceManager.Instance;

// Add main track
await manager.AddOutputSource("game_music.mp3", "Music");

// Add sound effects
var jumpSound = manager.AddSparkSource("jump.wav", false, 0.7f);
var coinSound = manager.AddSparkSource("coin.wav", false, 0.8f);

manager.Play();

// Play effects on demand
manager.PlaySparkSource(jumpSound);
await Task.Delay(500);
manager.PlaySparkSource(coinSound);
```

### 6. Pitch és Tempo beállítás

```csharp
var source = new Source();
await source.LoadAsync("song.mp3");

// Pitch adjustment (-6 to +6 semitones)
source.Pitch = 2.0; // Up 2 semitones

// Tempo adjustment (-20% to +20%)
source.Tempo = 10.0; // 10% faster

source.ChangeState(SourceState.Playing);
```

### 7. Audio adatok lekérése

```csharp
var source = new Source();
await source.LoadAsync("audio.wav");

// Get as float array
float[] audioData = source.GetFloatAudioData(TimeSpan.Zero);

// Process audio data
for (int i = 0; i < audioData.Length; i++)
{
    audioData[i] *= 0.5f; // Reduce volume by half
}
```

### 8. Custom sample processor

```csharp
public class MyProcessor : SampleProcessorBase
{
    public override void Process(Span<float> samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Math.Clamp(samples[i] * 1.2f, -1.0f, 1.0f);
        }
    }

    public override void Reset() { }
}

var source = new Source();
source.CustomSampleProcessor = new MyProcessor { IsEnabled = true };
await source.LoadAsync("music.mp3");
```

### 9. Események kezelése

```csharp
var manager = SourceManager.Instance;

manager.StateChanged += (s, e) =>
{
    Console.WriteLine($"Manager state: {manager.State}");
};

manager.PositionChanged += (s, e) =>
{
    Console.WriteLine($"Position: {manager.Position}");
};

await manager.AddOutputSource("track.mp3");
manager.Play();
```

### 10. Teljes alkalmazás példa

```csharp
class AudioPlayer
{
    private SourceManager manager;
    
    public async Task Initialize()
    {
        manager = SourceManager.Instance;
        
        SourceManager.OutputEngineOptions = new AudioConfig
        {
            SampleRate = 48000,
            Channels = AudioChannels.Stereo
        };
        
        SourceManager.EngineFramesPerBuffer = 512;
        
        manager.StateChanged += OnStateChanged;
        manager.Volume = 0.8f;
    }
    
    public async Task LoadPlaylist(string[] files)
    {
        foreach (var file in files)
        {
            await manager.AddOutputSource(file, Path.GetFileName(file));
        }
    }
    
    public void Play() => manager.Play();
    public void Pause() => manager.Pause();
    public void Stop() => manager.Stop();
    
    public void Seek(double seconds)
    {
        manager.Seek(TimeSpan.FromSeconds(seconds));
    }
    
    private void OnStateChanged(object? sender, EventArgs e)
    {
        Console.WriteLine($"State changed to: {manager.State}");
        
        if (manager.State == SourceState.Idle)
        {
            Console.WriteLine("Playback finished");
        }
    }
}

// Usage
var player = new AudioPlayer();
await player.Initialize();
await player.LoadPlaylist(new[] { "song1.mp3", "song2.mp3" });
player.Play();
```

---

## Függelék: Best Practices

1. **Mindig használj using vagy Dispose-t**: Minden ISource implementáció IDisposable
2. **Thread-safe műveletek**: Az API belül kezeli a szálkezelést
3. **Buffer méretek**: EngineFramesPerBuffer optimálása a késleltetés és teljesítmény egyensúlyához
4. **Volume szintek**: 0.0 - 1.0 tartomány, automatikus ellenőrzéssel
5. **Reset használata**: Új session előtt mindig hívd meg a Reset()-et