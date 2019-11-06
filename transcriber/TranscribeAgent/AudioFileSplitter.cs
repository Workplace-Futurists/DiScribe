using System.Collections.Generic;
using System.IO;
using transcriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System.Collections;
using Microsoft.Cognitive.SpeakerRecognition.Streaming.Result;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;
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
    class AudioFileSplitter
    {

        /// <summary>
        /// Create an AudioFileSplitter instance which uses a List of voiceprints for speaker recognition to
        /// divide an audio file. Allows the divided audio segment data to be accessed via streams.
        /// </summary>
        /// <param name="voiceprints"><see cref="List{Voiceprint}"/>List of <see cref="Voiceprint"/> instances used for speaker recognition</param>
        /// <param name="audioFile"><see cref="FileInfo"/> instance with absolute path to audio file. File must be a WAV file
        /// with mono audio, 16kHz sampling rate, and 16 bits per sample.</param>
        public AudioFileSplitter(List<Voiceprint> voiceprints, FileInfo audioFile)
        {
            Voiceprints = voiceprints;
            AudioFile = audioFile;
            ProcessWavFile(audioFile);                                          //Convert wav file to correct format and read data into buffer AudioData.
            MainStream = new MemoryStream(AudioData);                           //Set up the main stream with AudioData as backing buffer.
        }


        public const int SAMPLE_RATE = 16000;
        public const int BITS_PER_SAMPLE = 16;
        public const int CHANNELS = 1;

        /// <summary>
        /// List of Voiceprint instances used to identify users in the audio file.
        /// </summary>
        public List<Voiceprint> Voiceprints { get; set; }

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
        /// FOR DEMO: Will only return a sorted list with a single <see cref="AudioSegment"/>.
        ///  
        /// <para>Creates a SortedList of <see cref="AudioSegment"/> instances which are sorted according
        /// to their offset from the beginning of the audio file.
        /// The audio is segmented by identifying the speaker. Each time the speaker changes,
        /// the <see cref="AudioSegment"/> is added to the SortedList. </para>
        /// </summary>
        /// <returns>SortedList of <see cref="AudioSegment"/> instances</returns>
        public SortedList<AudioSegment, AudioSegment> SplitAudio()
        {
            var tempList = new SortedList<AudioSegment, AudioSegment>();

            /*TODO: Divide audio stream using speaker recognition
             * --------Logic------- 
             * 
             *   Get the matching User object for each of the Voiceprint objects in Voiceprints that were matched
             *   Get a set of start and end offsets (these are offsets from the beginning of the audio) for the time when each match occurred.
             *   Split MainStream into a List of AudioSegments using the offsets. 
             *   
             *   See GetUserFromResult() which
             *   uses the GUID in a RecognitionResult object to look up the User.
            */
                                            
            /*******For testing, just use fake RecognitionResultWrapper objects
             * which represent the results of the recognition process. ********/
            var outcomes = DoRecognition();                                    //RecognitionResultWrapper objects sorted by offset (keys are the offsets)

            foreach (var node in outcomes)
            {
                var segment = CreateAudioSegment(node.Value);
                tempList.Add(segment, segment);
            }
                       
            return tempList;
        }

        /// <summary>
        /// Identifies all speakers in AudioFile using the participant voiceprints.
        /// Create a set of RecognitionResultWrapper corresponding to each time the speaker changes.
        /// in the audio changes.
        /// For testing, just returns list of 5 fake RecognitionResultWrapper objects
        /// </summary>
        /// <returns><see cref="SortedList"/> List of RecognitionResultWrapper objects sorted by offset.</returns>
        private SortedList<int, RecognitionResultWrapper> DoRecognition()
        {
            RecognitionResult fakeResult = new RecognitionResult(new Identification(), new System.Guid(), 0);
            

            return new SortedList<int, RecognitionResultWrapper>()
            {
                {0, new RecognitionResultWrapper(0, 10, fakeResult)},
                {20, new RecognitionResultWrapper(15, 20, fakeResult)},
                {21, new RecognitionResultWrapper(21, 58, fakeResult)},
                {60, new RecognitionResultWrapper(60, 80, fakeResult) },
                {120, new RecognitionResultWrapper(120, 150, fakeResult) }

            };
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


        /// <summary>
        /// Creates an AudioSegment containing the specified stream in a <see cref="PullAudioInputStream"/> 
        /// wrapper. The stream has the specified int offset, and associated <see cref="Data.User"/> who
        /// is the person speaking.
        /// </summary>
        /// <param name="start">Offset in ms where this audio segment starts</param>
        /// <param name="end">Offset in ms where this audio segment ends</param>
        /// <param name="result">Outcome of the call to SpeakerRecognition API</param>
        /// <returns></returns>
        private AudioSegment CreateAudioSegment(RecognitionResultWrapper outcome)
        {
            const long BIT_RATE = SAMPLE_RATE * BITS_PER_SAMPLE;
            const long BYTES_PER_SECOND = BIT_RATE / 8;

            /*Calc positions in stream in bytes, given start and end offsets in ms */
            long lowerIndex = outcome.Start/1000 * BYTES_PER_SECOND;
            long upperIndex = outcome.End/1000 * BYTES_PER_SECOND;
            long segmentLength = upperIndex - lowerIndex;


            /*Setup the PullAudioInputStream for this AudioSegment 
             * by writing data for this segment into temp buffer and creating 
             * a MemoryStream to read from that buffer */
            byte[] buf = new byte[segmentLength];
            System.Span<byte> bufSpan = buf;
            MainStream.Seek(lowerIndex, SeekOrigin.Begin);                                 //Seek to position in stream at start of segment
            MainStream.Read(bufSpan);                                                      //Read bytes into buf (number of bytes read is segmentLength)
            MemoryStream stream = new MemoryStream(buf);

            AudioStreamFormat streamFormat = AudioStreamFormat.GetWaveFormatPCM(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS);   
            PullAudioInputStream audioStream = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(stream), streamFormat);

            User speaker = GetUserFromResult(outcome.Result);                  //Get the User associated with the GUID in the RecognitionResult

            return new AudioSegment(audioStream, outcome.Start, speaker);
        }



        /// <summary>
        /// Uses the GUID in the RecognitionResult to get a corresponding User object.
        /// Returns a TEST user object with random name currently.
        /// </summary>
        /// <param name="result"></param>
        private static User GetUserFromResult(RecognitionResult result)
        {
            return new User("USER_" + new System.Random().Next(), "TEST@EXAMPLE.COM", result.Value.IdentifiedProfileId);
        }



    }
}
