# Newsblast
A [Discord](https://discordapp.com) bot that provides RSS updates.

Uses [.NET Core](https://github.com/dotnet/core) 2.0, [Entity Framework Core](https://github.com/aspnet/EntityFrameworkCore) 2.0, [Discord.Net](https://github.com/RogueException/Discord.Net), and [Html Agility Pack](https://github.com/zzzprojects/html-agility-pack).

## Configuration

The following settings must be configured before running Newsblast.

### Newsblast.Server

The configuration for Newsblast.Server is stored in **Newsblast.Server.json**. If the file does not exist, run Newsblast.Server and the file will be created as long as the file permissions are set correctly.

* **DiscordBotToken**: The bot token for your Discord application. It can be found in the [Discord Developer Portal](https://discordapp.com/developers/applications).
* **SqlServerConnectionString**: The connection string for the SQL Server database or Azure SQL Database used for storing data for the bot.

### Newsblast.Web

The configuration for Newsblast.Web can be stored in several different locations. Refer to the [ASP.NET Core 2.0 documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?tabs=basicconfiguration) for more information.

#### Connection Strings

* **SqlServerConnectionString**: The connection string for the SQL Server database or Azure SQL Database used for storing data for the bot.

#### Application Settings

* **DiscordClientId**: The client ID for your Discord application. It can be found in the [Discord Developer Portal](https://discordapp.com/developers/applications).
* **DiscordClientSecret**: The client secret for your Discord application. It can be found in the [Discord Developer Portal](https://discordapp.com/developers/applications).
* **DiscordBotToken**: The bot token for your Discord application. It can be found in the [Discord Developer Portal](https://discordapp.com/developers/applications).
* **AdministratorIds**: A comma-delimited list of the Discord user IDs of the administrators of your application.

### Using a Different Database Provider

If you do not want to use SQL Server or Azure SQL Database, the source code can be modified to use a different Entity Framework Core 2.0 database provider. Refer to the [Entity Framework Core 2.0 documentation](https://docs.microsoft.com/en-us/ef/core/providers/) for more information.

#### Newsblast.Server

The database connection is handled in **Program.cs** and **Models/Configuration.cs**.

#### Newsblast.Shared

The database connection is handled in **Data/NewsblastContext.cs**.

#### Newsblast.Web

The database connection is handled in **Startup.cs**.

## Logging

Newsblast.Server saves logging information to **Newsblast.Server.log**. You may wish to prune it every so often to prevent unneccessary storage space usage.