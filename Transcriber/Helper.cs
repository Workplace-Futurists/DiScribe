//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DiScribe.Transcriber
{
    internal static class Helper
    {
        public static string WrapText(string text, int lineLength)
        {
            if (text.Length < lineLength)
                return text;

            StringBuilder outcome = new StringBuilder();

            int i;
            int lastSplitEnd = 0;
            Boolean savedLastLine = false;

            for (i = lineLength; i < text.Length; i += lineLength)
            {
                Boolean foundSpace = false;
                int startingIndex = i;

                while (i < text.Length && !foundSpace)
                {
                    if (text[i] == ' ') //Find a space and split there
                    {
                        foundSpace = true;
                        outcome.AppendLine(text.Substring(startingIndex - lineLength, lineLength + (i - startingIndex)));
                    }
                    i++;
                }

                /*If we never found a space before reaching end of text, append line anyway,
                 * otherwise it will be lost.*/
                if (!foundSpace)
                {
                    outcome.AppendLine(text.Substring(startingIndex, text.Length - startingIndex));
                    savedLastLine = true;
                }
                lastSplitEnd = i;
            }
            /*Ensure that remaining characters are also appended */
            if (!savedLastLine && text.Length % lineLength != 0)
            {
                outcome.AppendLine(text.Substring(lastSplitEnd, text.Length - lastSplitEnd));
            }
            return outcome.ToString();
        }

       
    }    
}
