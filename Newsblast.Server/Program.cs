using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Newsblast.Server.Models;
using Newsblast.Shared;
using Newsblast.Shared.Data;

namespace Newsblast.Server
{
    class Program
    {
        const string ConfigurationFileName = "Newsblast.Server.json";
        const string LogFileName = "Newsblast.Server.log";
        const int MaxParallelUpdates = 5;

        static async Task Main(string[] args)
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;

            var dir = Path.GetDirectoryName(new Uri(codeBase).LocalPath);

            var logFile = Path.Combine(dir, LogFileName);

            var serilog = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logFile)
                .CreateLogger();

            var logger = new LoggerFactory()
                .AddSerilog(serilog, dispose: true)
                .CreateLogger<Program>();

            var startupSucceeded = true;

            var ser = new DataContractJsonSerializer(typeof(Configuration));

            var configFile = Path.Combine(dir, ConfigurationFileName);

            FileStream stream;

            if (!File.Exists(configFile))
            {
                try
                {
                    using (stream = File.Create(configFile))
                    {
                        ser.WriteObject(stream, new Configuration());

                        logger.LogCritical($"{ConfigurationFileName} needs to be updated before the server can start.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"{ConfigurationFileName} could not be created.");
                }

                Environment.Exit(-1);
            }

            var connectionString = "";
            var token = "";

            DbContextOptions<NewsblastContext> contextOptions = null;

            try
            {
                using (stream = File.OpenRead(configFile))
                {
                    var config = (Configuration)ser.ReadObject(stream);

                    if (config != null)
                    {
                        connectionString = config.SqlServerConnectionString;
                        token = config.DiscordBotToken;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"{ConfigurationFileName} could not be read.");

                Environment.Exit(-1);
            }

            logger.LogInformation("Newsblast.Server starting up.");

            if (startupSucceeded)
            {
                logger.LogInformation("Migrating database.");

                if (connectionString == null || connectionString.Length <= 0)
                {
                    startupSucceeded = false;

                    logger.LogCritical($"SqlServerConnectionString is missing from {ConfigurationFileName}.");
                }
                else
                {
                    try
                    {
                        contextOptions = new DbContextOptionsBuilder<NewsblastContext>()
                            .UseSqlServer(connectionString)
                            .Options;

                        using (var context = new NewsblastContext(contextOptions))
                        {
                            await context.Database.MigrateAsync();
                        }

                        logger.LogInformation("Successfully migrated database.");
                    }
                    catch (Exception ex)
                    {
                        startupSucceeded = false;

                        logger.LogCritical(ex, "Failed to migrate database.");
                    }
                }
            }

            DiscordManager discord = null;

            if (startupSucceeded)
            {
                logger.LogInformation("Connecting to Discord.");

                if (token == null || token.Length <= 0)
                {
                    startupSucceeded = false;

                    logger.LogCritical($"DiscordBotToken is missing from {ConfigurationFileName}.");
                }
                else
                {
                    try
                    {
                        discord = new DiscordManager(logger, contextOptions, token);
                        await discord.ConnectAsync();

                        logger.LogInformation("Successfully connected to Discord.");
                    }
                    catch (Exception ex)
                    {
                        startupSucceeded = false;

                        logger.LogCritical(ex, "Failed to connect to Discord.");
                    }
                }
            }

            Console.WriteLine();

            if (!startupSucceeded)
            {
                logger.LogCritical("Startup has failed.");

                Environment.Exit(-1);
            }

            logger.LogInformation("Startup was successful.");

            var sources = new SourceManager(logger, contextOptions);
            var subscriptions = new SubscriptionManager(logger, contextOptions, discord);

            while (true)
            {
                logger.LogInformation($"Starting updates. " +
                    $"At least {Constants.TimeBetweenUpdatesInMinutes.ToString()} {(Constants.TimeBetweenUpdatesInMinutes != 1 ? "minutes" : "minute")} must elapse before next update cycle. ");

                await Task.WhenAll(Task.Run(async () =>
                {
                    await sources.UpdateAsync(MaxParallelUpdates);
                    await subscriptions.UpdateAsync(MaxParallelUpdates);
                }),
                Task.Delay(TimeSpan.FromMinutes(Constants.TimeBetweenUpdatesInMinutes)));
            }
        }
    }
}
