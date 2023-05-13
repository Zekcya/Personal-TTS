using NAudio.Wave;
using System.IO;

// This class is a custom implementation of the IWaveProvider interface, which is used to provide
// audio data to be played by an NAudio playback device.
public class MemoryStreamWaveProvider : IWaveProvider
{
    // A MemoryStream that contains the audio data to be played
    private readonly MemoryStream _memoryStream;

    // The format of the audio data contained in the MemoryStream
    private readonly WaveFormat _waveFormat;

    // Constructor that initializes the MemoryStream and WaveFormat
    public MemoryStreamWaveProvider(MemoryStream memoryStream, WaveFormat waveFormat)
    {
        _memoryStream = memoryStream;
        _waveFormat = waveFormat;
    }

    // Property that returns the WaveFormat of the audio data
    public WaveFormat WaveFormat => _waveFormat;

    // Method that reads audio data from the MemoryStream into a buffer
    // This method is called by the playback device when it needs more data to play
    public int Read(byte[] buffer, int offset, int count)
    {
        // The buffer is the area in memory where the audio data will be written
        // The offset is the position in the buffer where writing should start
        // The count is the maximum number of bytes to write

        // The Read method of the MemoryStream is used to read data from the stream and write it to the buffer
        // It returns the actual number of bytes read, which can be less than the count if the end of the stream is reached
        return _memoryStream.Read(buffer, offset, count);
    }
}
