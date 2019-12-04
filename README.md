# CPSC 319 2019W1 HSBC Project **DiScribe**

## Introduction
This is the code repository for a proof of concept project sponsored by HSBC Canada.

We aim to building a powerful bot that joins phone calls or web meetings, **record, transcribe, and send the meeting recordings texts** through email.

The bot could not only correctly transcribes the content of speech but also could recognizes voice differences, characterizes and labels "who spoke what" correctly and efficiently.

## Main Components
### Website Component
* [x]    Allows unregistered users to **register in the voice print** database with their email
* [ ]	 <span style="color:red">*(stretch goal)*</span> Able to save meeting informations into the Database
* [ ]    <span style="color:red">*(stretch goal)*</span> Allows users to **manage** meeting recordings and their corresponding transcription results
* [ ]    <span style="color:red">*(stretch goal)*</span> Allows users to update voice print samples, their email addresses (or add alternative ones)
* [ ]	 <span style="color:red">*(stretch goal)*</span> Warns users when audio input for voice registration isn't length sufficient

### Scheduling Component
* [x]    Able to retrieve meeting **attendees** email Lists
* [x]    Check if any attendees aren't **registered** in the **voice print database**
* [x]    Sends email to unregistered attendees with **link** to **voice print registration website**
* [x]    Bot automatically **Executes** the following processes when meeting time is near

### Call-in Component
* [x]   **Parses** meeting informations
* [x]   **Dials** into the meeting on time

### Recording Component
* [x]   Meeting finishes. **Saves** Recording into Cloud
* [x]   Downloads in correct Format

### Transcribing Component
* [x]   Starts the Transcribing process upon the recording **finishes downloading**
* [x]   **Transcribes** the meeting recording into text
* [x]   **Recognizes** the speakers and labelling them
* [x]   Outputs the formatted **text file**
* [ ]	 <span style="color:red">*(stretch goal)*</span> Speaker recognition with n > 10 meeting participants

### Emailing Component
* [x]    **Sends the Email** to meeting attendees upon transcription result is received

****Note that unchecked parts are incomplete***
