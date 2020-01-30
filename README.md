# starwatch
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
It provides stability to the ilovebacons server, account management, access controll and a lot more! It works silently in the background and you may not know it, but is the reason why ilovebacons has had great continous uptimes.
To prove its worth, it has gone through not 1, but 2 seperate server wipes by bacon and still has all its records and data!

Just check out the **REST API** on [Postman](https://documenter.getpostman.com/view/5336131/SWT8hzsk?version=latest)! No seriously... I put a lot of effort in documenting it all and holy shit what ilovebacon actually uses is only like 25% of its capabilities.

## Features
* Realtime Logs
* Monitoring for Crashes and automatic Reboots
* Automatic Bans for some targeted attacks
* VPN Detection
* Database! All the account data is stored on a database
* Full Player Tracking - Track who is online and where abouts in the world they are in REAL TIME.
* SSL Protected - Protect the admin page with actual encryption.
* Chat logs - Go back through chats at your own discresion.
* World Meta Data - Able to fetch world metadata such as its names and players online
* MUCH MUCH MORE!

### MAJOR API FEATURES
* Fully fledge REST API - Send and Receive JSON objects to effect the state of your server, from account management to player bans.
* Fully fledge Gateway API - Receive LIVE EVENTS from the server, from logs to player connects
* World File API - Download & Upload worlds via a web api! You can use this to enable builders to freely edit worlds!

TL;DR 
Without this, iLoveBacons would've been dead a long time ago

###  Servers Running Starwatch
_feel free to create PR to add your server_
* [iLoveBacons](https://ilovebacons.com)

## Installation
#### Requirements
* Dotnet Core 2.0
* MySQL or something similar
* Python (for metadata)

#### Running
1. Use dotnet run --project Starwatch
It should do everything you need. It will generate a initial configuration that you should edit.


### API
A mostly complete documentation for the REST and Gateway API can be found here on [Postman](https://documenter.getpostman.com/view/5336131/SWT8hzsk?version=latest).
The world api is not documented yet.
