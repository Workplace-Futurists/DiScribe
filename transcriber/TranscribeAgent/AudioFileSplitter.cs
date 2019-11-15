using System.Collections.Generic;
using System.IO;
using transcriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System.Collections;
using NAudio.Wave.SampleProviders;

namespace transcriber.TranscribeAgent
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
        public byte[] AudioData {get; set;}


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
        /// Provides a memory stream to access audio data which starts at
        /// startOffset and ends at endOffset in milliseconds
        /// </summary>
        public MemoryStream SplitAudioGetMemStream(ulong startOffset, ulong endOffset)
        {
            byte[] buf = SplitAudioGetBuf(startOffset, endOffset);
            System.Span<byte> bufSpan = buf;
            MainStream.Seek((long)startOffset, SeekOrigin.Begin);                           //Seek to position in stream at start of segment
            MainStream.Read(bufSpan);                                                      //Read bytes into buf (number of bytes read is segmentLength)
            return new MemoryStream(buf);

        }

        /// <summary>
        /// Obtain a buffer containing data for the specified start
        /// and end offsets in milliseconds.
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

            ulong segmentLength = endOffset - startOffset;


            /*Setup the PullAudioInputStream for this AudioSegment 
             * by writing data for this segment into temp buffer and creating 
             * a MemoryStream to read from that buffer */
            return new byte[segmentLength];

        }

        /// <summary>
        /// Get the entire audio managed by this instance as an AudioSegment
        /// </summary>
        /// <returns>The audio segment containing all audio data and a stream to access that data.</returns>
        public AudioSegment GetEntireAudio()
        {
            AudioStreamFormat streamFormat = AudioStreamFormat.GetWaveFormatPCM(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS);
            PullAudioInputStream audioStream = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(MainStream), streamFormat);

            const long BIT_RATE = SAMPLE_RATE * BITS_PER_SAMPLE;
            const long BYTES_PER_SECOND = BIT_RATE / 8L;
            long audioLengthMS = AudioData.Length * BYTES_PER_SECOND * 1000L;

            return new AudioSegment(audioStream, 0, audioLengthMS);
            
        }


        /// <summary>
        /// Identifies all speakers in AudioFile using the participant voiceprints.
        /// Create a set of RecognitionResultWrapper corresponding to each time the speaker changes.
        /// in the audio changes.
        /// For testing, just returns list of 5 fake RecognitionResultWrapper objects
        /// </summary>
        /// <returns><see cref="SortedList"/> List of RecognitionResultWrapper objects sorted by offset.</returns>
        //private SortedList<int, RecognitionResultWrapper> DoRecognition()
        //{
        //   RecognitionResult fakeResult = new RecognitionResult(new Identification(), new System.Guid(), 0);


        //   return new SortedList<int, RecognitionResultWrapper>()
        // {
        //   {0, new RecognitionResultWrapper(0, 10000, fakeResult)},
        // {20, new RecognitionResultWrapper(15000, 20000, fakeResult)},
        //{21, new RecognitionResultWrapper(21000, 58000, fakeResult)},
        //{60//, new RecognitionResultWrapper(60000, 80000, fakeResult) },
        //{120, new RecognitionResultWrapper(120000, 150000, fakeResult) }

        //};
        //}

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
            MemoryStream stream = SplitAudioGetMemStream(startOffset, endOffset);

            AudioStreamFormat streamFormat = AudioStreamFormat.GetWaveFormatPCM(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS);   
            PullAudioInputStream audioStream = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(stream), streamFormat);
            
            return new AudioSegment(audioStream, (long)startOffset, (long)endOffset);
        }


        /// <summary>
        /// Writes WAV data with header to a stream
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static byte[] writeWavToBuf(byte[] dataBuf, int sampleRate = SAMPLE_RATE, int channels = CHANNELS, int bitPerSample = BITS_PER_SAMPLE)
        {
           
            WaveFormat format = new WaveFormat(sampleRate, bitPerSample, channels);

            MemoryStream stream = new MemoryStream();
            WaveFileWriter writer = new WaveFileWriter(stream, format);
            
            writer.Write(dataBuf, 0, dataBuf.Length);

            writer.Dispose();
            return stream.ToArray();


        }





        /// <summary>
        /// Converts data in Wav file into the specified format and reads data section of file (removes header) into AudioData buffer.
        /// </summary>
        /// <param name="originalFile" ></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitRate"></param>
        /// <param name="channels"></param>
        private void ProcessWavFile(FileInfo originalFile, int sampleRate = SAMPLE_RATE, int channels = CHANNELS, int bitPerSample = BITS_PER_SAMPLE)
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
