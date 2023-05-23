using System.Diagnostics;
using FragLabs.Audio.Codecs;


OpusEncoder _encoder = OpusEncoder.Create(16000, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
// _encoder.Bitrate = 65536;
// _encoder.Bitrate = 32768;
_encoder.Bitrate = 16384;
// _encoder.Bitrate = 8192;
OpusDecoder _decoder = OpusDecoder.Create(16000, 1);

// https://stackoverflow.com/questions/44053762/incorrect-argument-using-opus-net-when-encoding
// 1 Answer
// Sorted by:

// Highest score (default)

// 3


// Your sampling rate can be one of [8, 12, 16, 24, 48] kHz, so 8kHz is ok.

// Value of _segmentFrames depends on your sampling rate and size of opus frame (in milliseconds). Possible sizes are [2.5, 5, 10, 20, 40, 60] ms, default is 20ms. So here is how 960 calculated:

// SampleRate / 1000 * FrameSize = 48000 / 1000 * 20 = 960

// If you want your rate to be 8kHz, _segmentFrames should be 8000 / 1000 * 20 = 160.

// _encoder.Bitrate can be any of 6-510 kbs.

// More info: https://mf4.xiph.org/jenkins/view/opus/job/opus/ws/doc/html/group__opus__encoder.html

// Share
// Improve this answer
// Follow


int _segmentFrames = (16000 / 1000) * 40;
int _bytesPerSegment = _encoder.FrameByteCount(_segmentFrames);

byte[] _notEncodedBuffer = new byte[0];
ulong _bytesSent = 0;


var destEncodedDecodedFile = "destEncodedDecodedFile.raw";
if(File.Exists(destEncodedDecodedFile))
    File.Delete(destEncodedDecodedFile);
void EncodeDecodeData(byte[] Buffer, int BytesRecorded)
{
    byte[] soundBuffer = new byte[BytesRecorded + _notEncodedBuffer.Length];
    for (int i = 0; i < _notEncodedBuffer.Length; i++)
        soundBuffer[i] = _notEncodedBuffer[i];
    for (int i = 0; i < BytesRecorded; i++)
        soundBuffer[i + _notEncodedBuffer.Length] = Buffer[i];

    int byteCap = _bytesPerSegment;
    int segmentCount = (int)Math.Floor((decimal)soundBuffer.Length / byteCap);
    int segmentsEnd = segmentCount * byteCap;
    int notEncodedCount = soundBuffer.Length - segmentsEnd;
    _notEncodedBuffer = new byte[notEncodedCount];
    for (int i = 0; i < notEncodedCount; i++)
    {
        _notEncodedBuffer[i] = soundBuffer[segmentsEnd + i];
    }

    for (int i = 0; i < segmentCount; i++)
    {
        byte[] segment = new byte[byteCap];
        for (int j = 0; j < segment.Length; j++)
            segment[j] = soundBuffer[(i*byteCap) + j];
        int len;
        byte[] buff = _encoder.Encode(segment, segment.Length, out len);
        _bytesSent += (ulong)len;
        var dec_buff = _decoder.Decode(buff, len, out len);
        // Console.WriteLine($"Decoded Length is: {len}");


        using (var fileStream = new FileStream(destEncodedDecodedFile, FileMode.Append, FileAccess.Write, FileShare.None))
        using (var bw = new BinaryWriter(fileStream))
        {
           bw.Write(dec_buff, 0, len);
        }
    }
    // Console.WriteLine($"TotalEncodedLength: {_bytesSent}");
}

// // 40 ms of silence at 16 KHz (1 channel).
// byte[] inputPCMBytes = new byte[40 * 16 * 2];
// for(int i = 0; i < 1000; i++)
//     EncodeDecodeData(inputPCMBytes, inputPCMBytes.Length);


int bytesRead = 0;
var destFile = "dest.raw";
if(File.Exists(destFile))
    File.Delete(destFile);

int bufferLength = 0;
var sourceFile = "source.raw";

Stopwatch stopWatch = new Stopwatch();
stopWatch.Start();
using (var inFileSteam = new FileStream(sourceFile, FileMode.Open))
{
    int Length60Ms = 16*60*2;
    int Length40Ms = 16*40*2;
    bufferLength = bufferLength == 0 ? Length60Ms : (bufferLength == Length60Ms ? Length40Ms : Length60Ms);
    bufferLength = Length40Ms;



    byte[] buffer = new byte[bufferLength];
    while ((bytesRead = inFileSteam.Read(buffer, 0, buffer.Length)) > 0)
    {
#if true
        // Console.WriteLine($"Pcm buffer.Length is: {buffer.Length}");
        EncodeDecodeData(buffer, buffer.Length);
#else

        using (var fileStream = new FileStream(destFile, FileMode.Append, FileAccess.Write, FileShare.None))
        using (var bw = new BinaryWriter(fileStream))
        {
           bw.Write(buffer, 0, buffer.Length);
        }
#endif
    }
}
Console.WriteLine("RunTime " + stopWatch.Elapsed.Milliseconds);
stopWatch.Stop();
Console.WriteLine($"TotalEncodedLength: {_bytesSent}");
Console.WriteLine($"{sourceFile}: {new System.IO.FileInfo(sourceFile).Length} bytes");
if(File.Exists(destEncodedDecodedFile))
    Console.WriteLine($"{destEncodedDecodedFile}: {new System.IO.FileInfo(destEncodedDecodedFile).Length} bytes");

