using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FuturistTranscriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FuturistTranscriber.TranscribeAgent
{
    /// <summary>
    /// Provides meeting audio file splitting. An audio file is split into <see cref="TranscribeAgent.AudioSegment"></see>
    /// instances.
    /// Splitting is performed by determining when speakers change. Only speakers with a known
    /// voice profile <see cref="Data.Voiceprint"></see> will be identified.    
    /// <para>Uses Speaker Recognition API in <see cref="Microsoft.CognitiveServices.Speech"/> for speaker recognition.</para>
    /// </summary>
    class AudioFileSplitter
    {
        public AudioFileSplitter(List<Voiceprint> voiceprints, FileInfo audioFile)
        {
            Voiceprints = voiceprints;
            ConvertWavFile(audioFile);              //Convert audio file into correct format and set AudioFile for access to this file.
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

            /*TODO: Divide audio file using recognition here.
              Get offset from beginning of file (start of meeting).
              Also determine who the speaker is and get a matching User object.

              FOR DEMO: Entire audio file is included, offset is 0, and user is a test user.
            */

            AudioStreamFormat streamFormat = AudioStreamFormat.GetWaveFormatPCM(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS);   //Set up audio stream.
            PushAudioInputStream audioStream = AudioInputStream.CreatePushStream(streamFormat);
            int offset = 0;
            User participant = new User("Some person", "someone@example.com");

            //Read raw audio into from file into buffer and write to audioStream. Note file header is removed.
            byte[] audioData = ReadWavFileRemoveHeader(AudioFile);
            audioStream.Write(audioData);                                                                                                                                    

            AudioSegment segment = new AudioSegment(audioStream, offset, participant);
            tempList.Add(segment, segment);                   

            return tempList;
        }


        
        /// <summary>
        /// Create a set of AudioSegments corresponding to each time the speaker
        /// in the audio changes.
        /// </summary>
        /// <returns><see cref="SortedList"/>SortedList of AudioSegements sorted by offset.</returns>
        private SortedList<AudioSegment, AudioSegment> IdentifySpeakers()
        {
            return new SortedList<AudioSegment, AudioSegment>();
        }


        /// <summary>
        /// Creates buffer with file data. File header is removed.
        /// </summary>
        /// <param name="inFile"></param>
        /// <returns>Byte[] containing raw audio data, without header.</returns>
        private static byte[] ReadWavFileRemoveHeader(FileInfo inFile)
        {
            byte[] outData;
            using (var inputReader = new WaveFileReader(inFile.FullName))
            {
                outData = new byte[inputReader.Length];                      //Buffer size is size of data section in wav file.
                inputReader.Read(outData, 0, (int)(inputReader.Length));     //Read entire data section of file into buffer. 
            }

            return outData;
        }


        /// <summary>
        /// Converts WAV file to a new WAV file with the required sample rate, bit rate, and # channels, 
        /// and then and sets AudioFile property to point to the converted file.
        /// </summary>
        /// <param name="originalFile" ></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitRate"></param>
        /// <param name="channels"></param>
        private void ConvertWavFile(FileInfo originalFile, int sampleRate = SAMPLE_RATE, int bitPerSample = BITS_PER_SAMPLE, int channels = CHANNELS)
        {
            string outFilePath = originalFile.FullName + "_converted.wav";                    //Full absolute path of output file. 
            
            /*Convert the file using NAudio library */
            using (var inputReader = new AudioFileReader(originalFile.FullName))
            {
                var mono = new StereoToMonoSampleProvider(inputReader);                        //convert our stereo ISampleProvider (inputReader) to mono
                mono.LeftVolume = 0.5f;
                mono.RightVolume = 0.5f;                                                       //Equal volume for left and right channels from stereo source

                var outFormat = new WaveFormat(sampleRate, channels);                          
                var resampler = new MediaFoundationResampler(mono.ToWaveProvider(), outFormat);
                resampler.ResamplerQuality = 60;                                               //Use highest quality. Range is 1-60.

                WaveFileWriter.CreateWaveFile(outFilePath, resampler);                        //Write the resampled mono wave file.
             }

            AudioFile = new FileInfo(outFilePath);                                           //Set AudioFile property to provide access to output file.
        }
    }
}
