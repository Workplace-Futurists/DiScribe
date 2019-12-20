# **DiScribe**

## Introduction
Hi, we are all **UBC** students who took CPSC 319 *Software Engineering Project* in 2019 Winter Term 1. This is the code repository for a proof of concept project sponsored by HSBC Canada.

We built a powerful bot that joins phone calls or web meetings, **record, transcribe, and send the meeting transcription texts** to your email.

The bot recognizes your content of speech and who you are correctly, with time stamps and name of the person.

**And YES.** This whole process runs **silently and autonomously**, handling multiple meetings concurrently with no issue.

**Feel free to read more about our project in our complete [Wiki](https://github.com/Workplace-Futurists/DiScribe/wiki).*

### Possible Result
```
[Happy Tree]	  0:0:0	  We are the workplace futurists. We are happy to see you.

[Excited Duck]	  0:0:8	  Me too. What a great day.

[Amazed Cow]	  0:0:11  So am I. Good bye.
```

### Platforms we support
- Webex Meeting
- MS Graph or Outlook event scheduling
- Our designated website
For meeting scheduling.

## Our Website link
https://discribe-cs319.azurewebsites.net

## Main Components
### Website Component
* [x]    Allows unregistered users to **register in the voice print** database with their email
* [x]	 <span style="color:red">*(stretch goal)*</span> Able to save meeting informations into the Database
* [x]    <span style="color:red">*(stretch goal)*</span> Allows users to **manage** meeting recordings and their corresponding transcription results
* [ ]    <span style="color:red">*(stretch goal)*</span> Allows users to update voice print samples, their email addresses (or add alternative ones)
* [x]	 <span style="color:red">*(stretch goal)*</span> Warns users when audio input for voice registration isn't length sufficient
                                                      (We implemented with a count-down and a display of the length of audio recorded)

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

***unchecked boxes indicates incompleteness***

*Note that: since we ran out of Azure Credits, you probably won't get to see the bot run in real time.*
