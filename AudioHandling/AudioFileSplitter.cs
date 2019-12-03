using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;





namespace DiScribe.AudioHandling
{
    /// <summary>
    /// Provides meeting audio file splitting. An audio file is split into <see cref="AudioSegment"></see>
    /// instances.
    /// Splitting is performed by determining when speakers change. Only speakers with a known
    /// voice profile will be identified.
    /// <para>Uses Speaker Recognition API in <see cref="Microsoft.CognitiveServices.Speech"/> for speaker recognition.</para>
    /// <para>Note that audio file must be a WAV file with the following characteristics: PCM/WAV mono with 16kHz sampling rate and 16 bits per sample. </para>
    /// </summary>
    public class AudioFileSplitter
    {
        /// <summary>
        /// Create an AudioFileSplitter instance which uses a List of voiceprints for speaker recognition to
        /// divide an audio file. Allows the divided audio segment data to be accessed via streams.
        /// </summary>
        /// instances used for speaker recognition
        /// <param name="audioFile"><see cref="FileInfo"/> instance with absolute path to audio file. File must be a WAV file
        /// with mono audio, 16kHz sampling rate, and 16 bits per sample.</param>
        public AudioFileSplitter(FileInfo audioFile)
        {
            AudioFile = audioFile;
            ProcessWavFile(audioFile);                                          //Convert wav file to correct format and read data into buffer AudioData.
            MainStream = new MemoryStream(AudioData);                           //Set up the main stream with AudioData as backing buffer.
        }

        /*Required WAV file format attributes */
        public const int REQ_SAMPLE_RATE = 16000;
        public const int REQ_BITS_PER_SAMPLE = 16;
        public const int REQ_CHANNELS = 1;


        /// <summary>
        /// Info for access to audio file which must be in correct format for Azure Cognitive Services Speech API.
        /// </summary>
        public FileInfo AudioFile { get; set; }

        /// <summary>
        /// WAV audio data without header. References the buffer backing MainStream.
        /// </summary>
        public byte[] AudioData { get; set; }

        public MemoryStream MainStream { get; private set; }

        /// <summary>
        /// Creates an AudioSegment from this instance using the specified
        /// start and end offsets. Note that if end offset > audio data length
        /// for this instance, a segment up to the end will be created.
        /// </summary>
        /// <returns>SortedList of <see cref="AudioSegment"/> instances</returns>
        public AudioSegment SplitAudio(ulong startOffset, ulong endOffset)
        {
            return CreateAudioSegment(startOffset, endOffset);
        }

        /// <summary>
        /// Obtain a buffer containing data for the specified start
        /// and end offsets in milliseconds.
        /// No RIFF header is included in data.
        /// </summary>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        /// <returns></returns>
        public byte[] SplitAudioGetBuf(ulong startOffset, ulong endOffset)
        {
            long BitRate = (long)REQ_SAMPLE_RATE * (long)REQ_BITS_PER_SAMPLE;
            long BytesPerSecond = BitRate / 8L;

            /*Calc positions in stream in bytes, given start and end offsets in milliseconds */
            ulong lowerIndex = startOffset * (ulong)BytesPerSecond / 1000UL;
            ulong upperIndex = endOffset * (ulong)BytesPerSecond / 1000UL;

            ulong segmentLength = upperIndex - lowerIndex;

            System.Span<byte> bufSpan = new byte[segmentLength];

            MainStream.Seek((long)lowerIndex, SeekOrigin.Begin);                           //Seek to position in stream at start of segment
            MainStream.Read(bufSpan);                                                      //Read bytes into buf (number of bytes read is segmentLength)

            return bufSpan.ToArray();
        }

        /// <summary>
        /// Get a stream which provides access to data in this audio file from startOffset to endOffset.
        /// No RIFF header is included in data.
        /// </summary>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        /// <returns></returns>
        public MemoryStream SplitAudioGetStream(ulong startOffset, ulong endOffset)
        {
            return new MemoryStream(SplitAudioGetBuf(startOffset, endOffset));
        }

        /// <summary>
        /// Get the entire audio managed by this instance as an AudioSegment
        /// </summary>
        /// <returns>The audio segment containing all audio data and a stream to access that data.</returns>
        public AudioSegment GetEntireAudio()
        {
            byte[] dataCopy = (byte[])AudioData.Clone();                   //Make copy of audio data

            /*Calc audio length in milliseconds */

            long bitRate = REQ_SAMPLE_RATE * REQ_BITS_PER_SAMPLE;
            long bytesPerSecond = bitRate / 8L;
            long audioLengthMS = AudioData.Length * 1000L / bytesPerSecond;

            return new AudioSegment(dataCopy, 0, audioLengthMS);
        }

        /// <summary>
        /// Creates buffer with file data. File header is removed.
        /// </summary>
        /// <param name="inFile"></param>
        /// <returns>Byte[] containing raw audio data, without header.</returns>
        private void ReadWavFileRemoveHeader(FileInfo inFile)
        {
            byte[] outData;
            using (var inputReader = new WaveFileReader(inFile.FullName))
            {
                outData = new byte[inputReader.Length];                      //Buffer size is size of data section in wav file.
                inputReader.Read(outData, 0, (int)(inputReader.Length));     //Read entire data section of file into buffer.
            }

            AudioData = outData;
        }

        /// <summary>
        /// Creates an AudioSegment containing the specified stream in a <see cref="PullAudioInputStream"/>
        /// wrapper. The stream has the specified int offset.
        /// </summary>
        /// <param name="startOffset">Offset in bytes where this audio segment starts</param>
        /// <param name="endOffset">Offset in bytes where this audio segment ends</param>
        /// <returns></returns>
        public AudioSegment CreateAudioSegment(ulong startOffset, ulong endOffset)
        {
            byte[] buf = SplitAudioGetBuf(startOffset, endOffset);

            return new AudioSegment(buf, (long)startOffset, (long)endOffset);
        }

        /// <summary>
        /// Splits audio data to obtain a stream which gives access data between
        /// startOffset and endOffset (inclusive).
        /// </summary>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        /// <returns></returns>
        public MemoryStream WriteWavToStream(ulong startOffset, ulong endOffset)
        {
            byte[] buf = SplitAudioGetBuf(startOffset, endOffset);

            byte[] wavBuf = WriteWavToBuf(buf);                  //Write data to the wave data buffer

            MemoryStream outputStream = new MemoryStream(wavBuf, 0, wavBuf.Length, true, true);
            outputStream.Position = 0;                          //Set stream position to 0;

            return outputStream;
        }

        /// <summary>
        /// Writes WAV data INCLUDING a RIFF header to a stream
        /// </summary>
        /// <returns></returns>
        public static byte[] WriteWavToBuf(byte[] dataBuf, int sampleRate = REQ_SAMPLE_RATE, int channels = REQ_CHANNELS, int bitPerSample = REQ_BITS_PER_SAMPLE)
        {
            byte[] output = null;
            WaveFormat format = new WaveFormat(sampleRate, bitPerSample, channels);

            MemoryStream stream = new MemoryStream();
            using (WaveFileWriter writer = new WaveFileWriter(stream, format))
            {
                writer.Write(dataBuf, 0, dataBuf.Length);

                output = stream.GetBuffer();
            }
            return output;
        }


        /// <summary>
        /// Resample source into 16 bit WAV mono output with the target sampling rate.
        /// Output stream includes modified RIFF header.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="targetSampleRate"></param>
        /// <param name="targetChannels"></param>
        /// <returns></returns>
        public static MemoryStream Resample(MemoryStream sourceStream, int targetSampleRate)
        {
                /*Read from the wav file's contents using stream */
                using (var inputReader = new WaveFileReader(sourceStream))
                {
                    int sourceChannels = inputReader.WaveFormat.Channels;

                    WdlResamplingSampleProvider resampler;

                    /*Stereo source. Must convert to mono with StereoToMonoSampleProvider */
                    if (sourceChannels == 2)
                    {
                        var monoSampleProvider = new StereoToMonoSampleProvider(inputReader.ToSampleProvider());
                        resampler = new WdlResamplingSampleProvider(monoSampleProvider, targetSampleRate);
                    }
                    else
                    {
                        resampler = new WdlResamplingSampleProvider(inputReader.ToSampleProvider(), targetSampleRate);
                    }

                    MemoryStream outStream = new MemoryStream();

                    /*Write origin data to stream as 16 bit PCM */
                    WaveFileWriter.WriteWavFileToStream(outStream, resampler.ToWaveProvider16());


                return outStream;

                }

        }



        


        /// <summary>
        /// Converts data in Wav file into the specified format and reads data section of file (removes header) into AudioData buffer.
        /// </summary>
        /// <param name="originalFile" ></param>
        /// <param name="sampleRate"></param>
        private void ProcessWavFile(FileInfo originalFile)
        {
            byte[] temp = File.ReadAllBytes(AudioFile.FullName);

            using (MemoryStream stream = new MemoryStream(temp))
            {

                int channels;

                /*Read from the wav file's contents using stream */
                using (var inputReader = new WaveFileReader(stream))
                {
                    channels = inputReader.WaveFormat.Channels;

                    WdlResamplingSampleProvider resampler;

                    /*Stereo source. Must convert to mono with StereoToMonoSampleProvider */
                    if (channels == 2)
                    {
                        var monoSampleProvider = new StereoToMonoSampleProvider(inputReader.ToSampleProvider());
                        resampler = new WdlResamplingSampleProvider(monoSampleProvider, REQ_SAMPLE_RATE);
                    }
                    else
                    {
                        resampler = new WdlResamplingSampleProvider(inputReader.ToSampleProvider(), REQ_SAMPLE_RATE);
                    }


                    /*Write converted audio to overwrite the original wav file */
                    WaveFileWriter.CreateWaveFile16(AudioFile.FullName, resampler);

                }

                using (WaveFileReader reader = new WaveFileReader(AudioFile.FullName))
                {
                    AudioData = new byte[reader.Length];
                    reader.Read(AudioData, 0, (int)reader.Length);
                }


            }
        }
    }
}
