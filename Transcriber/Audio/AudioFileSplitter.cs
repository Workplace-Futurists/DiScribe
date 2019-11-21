using System.Collections.Generic;
using System.IO;
using DatabaseController.Data;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System.Collections;
using NAudio.Wave.SampleProviders;

namespace Transcriber.TranscribeAgent
{
    /// <summary>
    /// Provides meeting audio file splitting. An audio file is split into <see cref="TranscribeAgent.AudioSegment"></see>
    /// instances.
    /// Splitting is performed by determining when speakers change. Only speakers with a known
    /// voice profile <see cref="Data.Voiceprint"></see> will be identified.    
    /// <para>Uses Speaker Recognition API in <see cref="Microsoft.CognitiveServices.Speech"/> for speaker recognition.</para>
    /// <para>Note that audio file must be a WAV file with the following characteristics: PCM/WAV mono with 16kHz sampling rate and 16 bits per sample. </para>
    /// </summary>
    public class AudioFileSplitter
    {

        /// <summary>
        /// Create an AudioFileSplitter instance which uses a List of voiceprints for speaker recognition to
        /// divide an audio file. Allows the divided audio segment data to be accessed via streams.
        /// </summary>
        /// <param name="voiceprints"><see cref="List{Voiceprint}"/>List of <see cref="Voiceprint"/> instances used for speaker recognition</param>
        /// <param name="audioFile"><see cref="FileInfo"/> instance with absolute path to audio file. File must be a WAV file
        /// with mono audio, 16kHz sampling rate, and 16 bits per sample.</param>
        public AudioFileSplitter(FileInfo audioFile)
        {
            AudioFile = audioFile;
            ProcessWavFile(audioFile);                                          //Convert wav file to correct format and read data into buffer AudioData.
            MainStream = new MemoryStream(AudioData);                           //Set up the main stream with AudioData as backing buffer.
        }

        /*Default WAV file format attributes */
        public const int SAMPLE_RATE = 16000;
        public const int BITS_PER_SAMPLE = 16;
        public const int CHANNELS = 1;


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
        /// <throws>Exception if end offSet <= startOffset,
        /// or startOffset > audio length.</throws>
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
            const long BIT_RATE = SAMPLE_RATE * BITS_PER_SAMPLE;
            const long BYTES_PER_SECOND = BIT_RATE / 8L;

            /*Calc positions in stream in bytes, given start and end offsets in milliseconds */
            ulong lowerIndex = startOffset / 1000UL * BYTES_PER_SECOND;
            ulong upperIndex = endOffset / 1000UL * BYTES_PER_SECOND;

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
            const long BIT_RATE = SAMPLE_RATE * BITS_PER_SAMPLE;
            const long BYTES_PER_SECOND = BIT_RATE / 8L;
            long audioLengthMS = AudioData.Length / BYTES_PER_SECOND * 1000L;

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
        /// <param name="start">Offset in bytes where this audio segment starts</param>
        /// <param name="end">Offset in bytes where this audio segment ends</param>
        /// <param name="result">Outcome of the call to SpeakerRecognition API</param>
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

            MemoryStream outputStream = new MemoryStream(wavBuf);
            outputStream.Position = 0;                          //Set stream position to 0;

            return outputStream;
        }



        /// <summary>
        /// Writes WAV data INCLUDING a RIFF header to a stream
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static byte[] WriteWavToBuf(byte[] dataBuf, int sampleRate = SAMPLE_RATE, int channels = CHANNELS, int bitPerSample = BITS_PER_SAMPLE)
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
        /// Converts data in Wav file into the specified format and reads data section of file (removes header) into AudioData buffer.
        /// </summary>
        /// <param name="originalFile" ></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitRate"></param>
        /// <param name="channels"></param>
        private void ProcessWavFile(FileInfo originalFile, int sampleRate = SAMPLE_RATE)
        {
            /*Convert the file using NAudio library */
            using (var inputReader = new WaveFileReader(originalFile.FullName))
            {
                WdlResamplingSampleProvider resampler;

                /*Stereo source. Must convert to mono with StereoToMonoSampleProvider */
                if (inputReader.WaveFormat.Channels == 2)
                {
                    var monoSampleProvider = new StereoToMonoSampleProvider(inputReader.ToSampleProvider());
                    resampler = new WdlResamplingSampleProvider(monoSampleProvider, sampleRate);
                }

                else
                {
                    resampler = new WdlResamplingSampleProvider(inputReader.ToSampleProvider(), sampleRate);
                }

                var wav16provider = resampler.ToWaveProvider16();
                AudioData = new byte[inputReader.Length];
                wav16provider.Read(AudioData, 0, (int)(inputReader.Length));        //Read transformed WAV data into buffer WavData (header is removed).

            }

        }







    }
}
