# unoh

unoh is a .NET 8 Discord bot used to flip matches between 2 teams in threads, using Discord components

created for usage in PIL:X (2024), it allowed for multiple flips to occur at the same time, allows for different formats at the same time, and formats are coded using json

## setup

1. install .NET 8
2. make a Discord application (https://discord.com/developers)
3. copy secrets.template.json and tourney.template.json and modify them to your needs
4. run the bot

## usage

the bot comes with 2 commands:

- `/flip <team> <format>` (autocompletes): start a new flip process
- `/teams`: list all teams and their captains

any changes to the config json files (`secrets.json` and `tourney.json`) require the app to be restarted
