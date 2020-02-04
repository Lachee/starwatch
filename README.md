
# starwatch
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
It provides stability to the ilovebacons server, account management, access controll and a lot more! It works silently in the background and you may not know it, but is the reason why ilovebacons has had great continous uptimes.
To prove its worth, it has gone through not 1, but 2 seperate server wipes by bacon and still has all its records and data!

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

## Installation
#### Requirements
* Dotnet Core 2.0
* MySQL or something similar
* Python (for metadata)

1. Import SQL into the server: `Resources/starwatch.sql`
How you import is up to you. I personally prefer to drag'n'drop into phpMyAdmin, but you can easily do it via the command line too.

2. Setup Database User.
Create a user for the starwatch application. Its NOT recommended to use the root account. 

3. `dotnet build` 
This will build Starwatch and restore the dependencies. 

4. Import previous configuration: 
`dotnet run --project Starwatch -import <path to starbound_server.conf>`
This will run the project (and build too technically) with the `-import` flag. This will run starwatch and import previous starbound_server configuration.
**Note**
It will fail the first run as it will generate the configuration. Edit the `Starwatch.json` and update the `SQL` settings after the first run to match what you setup in steps 1 and 2. For more information about the other configurations and what each item means, **check the configuration section**.

5. Configure Starwatch
Edit your `Starwatch.json` to match your starbound configuration.  Check the configuration section for more details.

6. Run starwatch
`dotnet run --project Starwatch`
This will run starwatch and start the server (if all configured correctly).

### Configuration
#### Base
| Field | Description |
|-------|-------------|
| output | file for starwatch logs |
| SQL | See SQL configuration |
| python_parsers | path to the py folder |
| children | child configurations. |

#### SQL
| Field | Description |
|-------|-------------|
| Host | Host of the SQL database.|
| Database | Database Name |
| Username | Username to the DB |
| Password | Password to the DB |
| Prefix | The prefix all the tables have. |
| Passphrase | Encryption passphrase for sensitive user data and passwords. Make sure this is unique |
|ConnectionStringOverride | custom connection string for abnormal SQL setups. |

#### api
| Field | Description |
|-------|-------------|
| blocklist | IPs and Accounts that are blocked from the API |
| enable_rest | Enables the rest api |
| rest_minimum_auth | Minimum level to use the rest api |
| enable_world | Enables world access|
| enable_auth | Enables oAuth2 API |
| enable_log | Enables log access |
| enable_web | Enables live log serving |
| web_root | Root directory of the web pages to serve |
| secured | Uses SSL encryption. This is **required** for account management and oAuth2. |
| port | Port to serve the APIs on |
| cert_file | PFX Certificate for SSL. See `convert-cert.sh` to see how to convert letsencrypt certificates |
| cert_pass | Password for the cert_file |


## API
A mostly complete documentation for the REST and Gateway API can be found here on [Postman](https://documenter.getpostman.com/view/5336131/SWT8hzsk?version=latest).
The world api is not documented yet.
