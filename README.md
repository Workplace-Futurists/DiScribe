# CPSC 319 2019W1 HSBC Project **DiScribe**

## Introduction
This is the code repository for a proof of concept project sponsored by HSBC Canada.

We aim to building a powerful bot that joins phone calls or web meetings, **record, transcribe, and send the meeting recordings texts** through email.

The bot could not only correctly transcribes the content of speech but also could recognizes voice differences, characterizes and labels "who spoke what" correctly and efficiently.

## Main Processes
### Call-in Process
- **Receives** proposed meeting schedules
- **Parses** meeting informations
- **Dials** into the meeting on time

### Recording Process
- **Ask for Identification** upon meeting attendee dials in *(incomplete)*
- **Record and saves** voice samples for **speaker recognition** use *(incomplete)*
- Meeting finishes. **Saves** Recording into Cloud
- Downloads in correct Format

### Transcribing Process
- Starts the Transcribing process upon recording received *(incomplete)*
- **Transcribes** the meeting recording into text
- **Recognizes** the speakers and labelling them
- Outputs the formatted **text file** `minutes.txt`

### Emailing Process
- **Sends the Email** to meeting hosts upon `minutes.txt` is received *(incomplete)*
