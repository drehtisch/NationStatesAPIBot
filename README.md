# NationStatesAPIBot
A Bot for performing API Actions on NationStates and to provide other features via DiscordAPI. It can be controlled via discord.

I wrote this bot for the discord server of the [region](https://www.nationstates.net/region=the_free_nations_region "The Free Nations Region") where i am currently member of. 

It can send automatically recruitment telegrams via the NationStates API.
It will check if the recipient would receive the telegram before actually sending it, if not it will be skipped. Which increases the efficency because no telegrams are wasted.

It is intended to provide:
  - some statistics from NationStates API to discord users
  - verification of nation ownership via the NationStates API (To-Do)
  - backup functionality of discord channel chat logs. (To-Do)
  - basic moderation functionality for authorized users (To-Do)
  
It will be extended as needed.

It can probably be used for general purpose as well.

Feel free to contribute!

# Configuration - v2.1+

The order of the lines is irrelevant. Write them into a file named "keys.config" in your execution directory.  

Required:

`clientKey=<your nation states clientKey>`  
`telegramId=<your nation states recruitment telegramId>`  
`secretKey=<your nation states telegram secretKey>`  
`contact=<your nation states nation or an email address or something like that>`  
`dbConnection=<your mysql database connection string>`  
`botLoginToken=<your discord bot login token>`  
`botAdminUser=<discord user id how is main admin on this bot>`  
`regionName=<name of the region you are recruiting for>`
  
Optional:  
`logLevel=<logLevel 0-5 0 = Critical - 5 = Debug>`
See Discord.Net.Core LogSeverity for details

Be sure to have at least dbConnection configured when you run `dotnet ef database update`.  
You need to have a copy of "keys.config" in the directory where you execute `dotnet ef database update` or `dotnet ef migrations add <name>`

# Roadmap

## Version 2.6

- Recruitable Nations (/rn) returns a list of nations who would receive recruitment telegrams out of pending list


## Version 3

- Add refounded nations to pending  
- Help Command
- Huge Refactoring to Configuration, Logging, Testability, API call systematics, using cache first approach with help of dumps, etc.  
- Get Nations that were endorsed by a nation (endorsed) (on cache only)
- Add List of active nations for a possibly (/arn) Active recruitable nations (in question)

## Later

- CustomStats about nations and regions
- Backing Up Logs of Channels using Discord Chat Exporter
- Verify Nation ownership using Nation States Verification API: https://www.nationstates.net/pages/api.html#verification and automatic role asignment, if verified, after specified time.
- Activity Points for verified nations on NationStates activity and discord activity
- BasicStats about the recruitment process (send, skipped, failed, pending count) for total and since the recruitment process was last started
- Stats for manual recruitment results  
- Basic Moderator Features (Kick, Ban, Mute, Delete Messages, etc.)
- Games (Werewolf)  
- Polls  
