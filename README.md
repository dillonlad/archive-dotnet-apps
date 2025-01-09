# archive-dotnet-apps
Archived dotnet apps created and used for single purposes

## Add Access Key
This script generates an API key for a GateWay and saves it into a table called `access_keys`. Firstly, a random 32 character string is generated. It is then hashed before being saved into the database.

## Create Verification Key
This script allows a user to create a secret 'access key' or 'passcode' which could be used for a mobile app. The mobile app could have numerous passcodes that could be used to register a user for example. Each passcode could be specific to a client or company who have some need of the app for their customers.

## Deactivate Practice
This program allows the user to deactivate a 'practice' in a database by updating a column. It then deactivates all entities associated to that practice in the database.