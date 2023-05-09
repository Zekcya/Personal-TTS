using NAudio.Wave;
using System.IO;

public class MemoryStreamWaveProvider : IWaveProvider
{
    private readonly MemoryStream _memoryStream;
    private readonly WaveFormat _waveFormat;

    public MemoryStreamWaveProvider(MemoryStream memoryStream, WaveFormat waveFormat)
    {
        _memoryStream = memoryStream;
        _waveFormat = waveFormat;
    }

    public WaveFormat WaveFormat => _waveFormat;

    public int Read(byte[] buffer, int offset, int count)
    {
        return _memoryStream.Read(buffer, offset, count);
    }
}
