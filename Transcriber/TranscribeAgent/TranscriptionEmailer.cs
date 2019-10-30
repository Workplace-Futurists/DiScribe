using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FuturistTranscriber.TranscribeAgent
{
    /// <summary>
    /// Provides emailing feature to send email with a transcription file as an attachment.
    /// Optionally allows custom e-mail body and subject to be specified.
    /// </summary>
    class TranscriptionEmailer
    {
        public TranscriptionEmailer(string organizerEmail, FileInfo transcript)
        {
            OrganizerEmail = organizerEmail;
        }

        public string OrganizerEmail { get; set; }

        public FileInfo Transcript { get; set; }
       
        /// <summary>
        /// Sends an e-mail containing the text file associated with Transcript property of this instance.
        /// Uses the e-mail address in OrganizerEmail property. Optionally, allows a custom e-mail body
        /// and subject to be specified.
        /// </summary>
        /// <param name="body" type="string"></param>
        /// <param name="subject" type="string"></param>
        /// <returns></returns>
        public Boolean SendEmail(string body = "", string subject = "")
        {
            return true;
        }
        

    }
}
