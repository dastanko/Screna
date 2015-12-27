# Screna.Audio
This namespace hosts the Audio related features of Screna.  
Audio Capture technology has been adapted from [NAudio](https://github.com/NAudio/NAudio) by Mark Heath licensed under [Microsoft Public License](LICENSE.md)

> Take care that you use the same WaveFormat throughout a single Capture.  
Not following the same would lead to unexpected results with audio.

* ## IAudioProvider  
Implementing classes provide Audio.
Recording and Loopback providers are built-in.
Others can be added by Implementing this interface.
This interface is similar to `IWaveIn` in `NAudio`.
  * WasapiCapture  
  `Currently only Shared mode with MixFormat is supported.`  
  Capture audio from Microphone using Wasapi.

  * WasapiLoopbackCapture  
  Capture soundcard output using Wasapi.

  * WaveIn  
  Capture audio from Microphone using WaveIn Api.

* ## WaveFormat
Implementation of `WAVEFORMATEX`:  
Contains the format information that precedes wave audio.  
`Difference from NAudio: Serialize() method does not write its 'TotalSize' so as to be used for streaming. This although is done in WaveFileWriter`  
  * WaveFormatExtensible  
  An Extension of the WaveFormat structure

  * Mp3WaveFormat  
  WaveFormat for Mp3 encoded files

* ## IAudioFileWriter
Encodes Audio into an audio file.
  * WaveFileWriter  
  Writes the provided data to a Wave file.

  * EncodedAudioFileWriter  
  Encodes the data using an IAudioEncoder and writes it to a file.

* ## IAudioEncoder
Encodes audio and provides the encoded data.
  * Mp3EncoderLame  
  Encodes 16-bit PCM audio to Mp3 using LAME.
