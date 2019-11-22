# Scheduler
## Introduction
This component does the following

- **Check** if theres new emails in the inbox every now and then
- **Parses** the meeting informations and stores them in database
- **Retrieves** the meeting attendees email lists
- **Check** if all the attendees have registered their voice prints in the database
- **Emails** the unregistered attendees with registration link
- **Schedules** tasks or threads (*the process is written in [Main](../Main)*) when there are upcoming meetings within 30 minutes
