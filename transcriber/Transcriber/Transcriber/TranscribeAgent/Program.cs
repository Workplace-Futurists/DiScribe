using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;

namespace FuturistTranscriber.TranscribeAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Creating transcript...");
            Console.WriteLine("Converting audio...");

            FileInfo test = new FileInfo("C:\\Users\\OCB\\Documents\\UBC\\4th Year\\CPSC 319\\" +
                "Project Local\\cs319-2019w1-hsbc\\transcriber\\Test\\FakeMeeting.wav");

            ConvertFile(test);
             

        }
        
        private static void ConvertFile(FileInfo originalFile, int sampleRate = 16000, int bitRate = 16, int channels = 1)
        {
            string outFilePath = originalFile.FullName + "_converted.wav";                    //Full absolute path of output file. 

            /*Convert the file using NAudio library */
            using (var inputReader = new AudioFileReader(originalFile.FullName))
            {
                // convert our stereo ISampleProvider (inputReader) to mono
                var mono = new StereoToMonoSampleProvider(inputReader);
                mono.LeftVolume = 0.5f;
                mono.RightVolume = 0.5f;                                                       //Equal volume for left and right channels from stereo source

                var outFormat = new WaveFormat(sampleRate, channels);                          //Specify sample rate and # channels.
                var resampler = new MediaFoundationResampler(mono.ToWaveProvider(), outFormat);
                resampler.ResamplerQuality = 60;                                               //Use highest quality. Range is 1-60.

                WaveFileWriter.CreateWaveFile(outFilePath, resampler);                        //Write the resampled mono wave file.

            }
                        
        }




    }

    
}
