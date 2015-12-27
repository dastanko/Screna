Capturing Avi Video from Entire Screen:
```csharp
// Initialize an IImageProvider
IImageProvider provider = new WindowProvider(); // Capture the entire Desktop

// Initialize AviWriter
IVideoFileWriter VideoWriter = new AviWriter("output.avi",
                                              provider,
                                              AviCodec.MotionJpeg);

// Create and Start the Recorder
IRecorder Recorder = new Recorder(VideoWriter, provider);
Recorder.Start();

// Do Anything you wish ...

Recorder.Stop();
```
------------------------------------------------------------

Capturing only Audio from Default Microphone into .wav file

```csharp
// Make sure the same WaveFormat is used with WaveIn and WaveFileWriter
var wf = new WaveFormat(44100, 16, 1);

IAudioProvider audio = new WaveIn() { WaveFormat = wf };

IAudioFileWriter writer = new WaveFileWriter("output.wav", wf);

IRecorder Recorder = new AudioRecorder(audio, writer);

// You know how to use the Recorder
```
---------------------------------------------

Include the Mouse Cursor Overlay in the Video
```csharp
// Initialize an IImageProvider along with Cursor Overlay.
IImageProvider provider = new WindowProvider(Overlays: new MouseCursor());

// Initialize AviWriter
IVideoFileWriter VideoWriter = new AviWriter("output.avi",
                                              provider,
                                              AviCodec.MotionJpeg);

// Create and Start the Recorder
IRecorder Recorder = new Recorder(VideoWriter, provider);

// You know how to use the Recorder
```
----------------------------------------------

Capturing Screen to Gif
```csharp
// Initialize an IImageProvider
IImageProvider provider = new WindowProvider(); // Capture the entire Desktop

// Initialize AviWriter
IVideoFileWriter VideoWriter = new GifWriter("output.gif");

// Create and Start the Recorder
IRecorder Recorder = new Recorder(VideoWriter, provider);
```
