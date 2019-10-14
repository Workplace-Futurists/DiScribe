using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FuturistTranscriber.Data;

namespace FuturistTranscriber.TranscribeAgent
{
    /// <summary>
    /// Provides meeting audio file splitting. An audio file is split into <see cref="TranscribeAgent.AudioSegment"></see>
    /// instances.
    /// Splitting is performed by determining when speakers change. Only speakers with a known
    /// voice profile <see cref="Data.Voiceprint"></see> will be identified.    
    /// </summary>
    class AudioFileSplitter
    {
        public AudioFileSplitter(List<Voiceprint> voiceprints, FileInfo audioFile)
        {
            Voiceprints = voiceprints;
            AudioFile = audioFile;
        }

        public List<Voiceprint> Voiceprints { get; set; }

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
        public SortedList<int, AudioSegment> SplitAudio()
        {
            var tempList = new SortedList<int, AudioSegment>();
            tempList.Add



            return new SortedList<int, AudioSegment>();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private SortedList<AudioSegment, AudioSegment> IdentifySpeakers()
        {
            return new SortedList<AudioSegment, AudioSegment>();
        }
    }
}
