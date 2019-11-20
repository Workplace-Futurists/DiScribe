# CPSC 319 2019W1 HSBC Project **DiScribe**

## Introduction
This is the code repository for a proof of concept project sponsored by HSBC Canada.

We aim to building a powerful bot that joins phone calls or web meetings, **record, transcribe, and send the meeting recordings texts** through email.

The bot could not only correctly transcribes the content of speech but also could recognizes voice differences, characterizes and labels "who spoke what" correctly and efficiently.

## Main Components
### Website Component
- <span style="color:orange">*(incomplete)*</span> Allows unregistered users to **register in the voice print** database with their email
- <span style="color:orange">*(incomplete)* </span><span style="color:red">*(stretch goal)*</span> Allows users to **manage** meeting recordings and their corresponding transcription results
- <span style="color:orange">*(incomplete)*</span> <span style="color:red">*(stretch goal)*</span> Allows users to update voice print samples, their email addresses or add alternative ones

### Scheduling Component
- <span style="color:orange">*(incomplete)*</span> **Listens** for emails containing scheduled meeting informations
- <span style="color:orange">*(incomplete)*</span> Saves meeting informations into **database**
- <span style="color:orange">*(incomplete)*</span> Able to retrieve meeting **attendees** email Lists
- <span style="color:orange">*(incomplete)*</span> Check if any attendees aren't **registered** in the **voice print database**
- <span style="color:orange">*(incomplete)*</span> Sends email to unregistered attendees with **link** to **voice print registration website**
- <span style="color:orange">*(incomplete)*</span> Bot automatically **Executes** the following processes when meeting time is near

### Call-in Component
- **Parses** meeting informations
- **Dials** into the meeting on time

### Recording Component
- Meeting finishes. **Saves** Recording into Cloud
- Downloads in correct Format

### Transcribing Component
- <span style="color:orange">*(incomplete)*</span> Starts the Transcribing process upon the recording **finishes downloading**
- **Transcribes** the meeting recording into text
- **Recognizes** the speakers and labelling them
- Outputs the formatted **text file** `minutes.txt`

### Emailing Component
- <span style="color:orange">*(incomplete)*</span> **Sends the Email** to meeting hosts upon `minutes.txt` is received
