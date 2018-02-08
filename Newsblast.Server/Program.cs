using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newsblast.Server.Models;
using Newsblast.Shared.Data;

namespace Newsblast.Server
{
    class Program
    {
        const string ConfigurationFileName = "Newsblast.Server.json";

        static int MaxParallelUpdates = 5;
        static int MinMinutesBetweenFeedUpdates = 20;

        static NewsblastContext Context;
        static DiscordManager Discord;
        static SourceManager Sources;
        static SubscriptionManager Subscriptions;

        static async Task Main(string[] args)
        {
            var startupSucceeded = true;

            var ser = new DataContractJsonSerializer(typeof(Configuration));

            var codeBase = Assembly.GetExecutingAssembly().CodeBase;

            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase);

            var configFile = new Uri(Path.Combine(dir, ConfigurationFileName)).LocalPath;

            FileStream stream;

            if (!File.Exists(configFile))
            {
                try
                {
                    stream = File.Create(configFile);

                    ser.WriteObject(stream, new Configuration());

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{ConfigurationFileName} needs to be updated before the server can start.");
                    Console.ResetColor();

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{ConfigurationFileName} could not be created: {ex.Message}");
                    Console.ResetColor();
                }

                Environment.Exit(-1);
            }

            var connectionString = "";
            var token = "";

            try
            {
                stream = File.OpenRead(configFile);
                var config = (Configuration)ser.ReadObject(stream);

                if (config != null)
                {
                    connectionString = config.SqlServerConnectionString;
                    token = config.DiscordBotToken;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{ConfigurationFileName} could not be read: {ex.Message}");
                Console.ResetColor();

                Environment.Exit(-1);
            }

            Console.WriteLine($"{DateTime.Now.ToString()} - Newsblast.Server starting up...");
            Console.WriteLine();

            if (startupSucceeded)
            {
                Console.Write($"{DateTime.Now.ToString()} - Connecting to SQL Server...".PadRight(60));

                if (connectionString == null || connectionString.Length <= 0)
                {
                    startupSucceeded = false;

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Fail]");
                    Console.WriteLine($"SqlServerConnectionString is missing from {ConfigurationFileName}.");
                    Console.ResetColor();
                }
                else
                {
                    try
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<NewsblastContext>()
                            .UseSqlServer(connectionString);

                        Context = new NewsblastContext(optionsBuilder.Options);
                        await Context.Database.MigrateAsync();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[Success]");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        startupSucceeded = false;

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Fail]");
                        Console.WriteLine(ex.Message);
                        Console.ResetColor();
                    }
                }
            }

            if (startupSucceeded)
            {
                Console.Write($"{DateTime.Now.ToString()} - Connecting to Discord...".PadRight(60));

                if (token == null || token.Length <= 0)
                {
                    startupSucceeded = false;

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Fail]");
                    Console.WriteLine($"DiscordBotToken is missing from {ConfigurationFileName}.");
                    Console.ResetColor();
                }
                else
                {
                    try
                    {
                        Discord = new DiscordManager(Context, token);
                        await Discord.ConnectAsync();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[Success]");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        startupSucceeded = false;

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Fail]");
                        Console.WriteLine(ex.Message);
                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine();

            if (!startupSucceeded)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString()} - Startup has failed.");
                Console.WriteLine();
                Console.ResetColor();

                Environment.Exit(-1);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{DateTime.Now.ToString()} - Startup was successful.");
            Console.WriteLine();
            Console.ResetColor();

            Sources = new SourceManager(Context);
            Subscriptions = new SubscriptionManager(Context, Discord);

            while (true)
            {
                Console.WriteLine($"{DateTime.Now.ToString()} - Starting updates... at least {MinMinutesBetweenFeedUpdates.ToString()} {(MinMinutesBetweenFeedUpdates != 1 ? "minutes" : "minute")} must elapse before next update cycle.");
                await Task.WhenAll(UpdateAsync(), Task.Delay(new TimeSpan(0, MinMinutesBetweenFeedUpdates, 0)));
            }
        }

        static async Task UpdateAsync()
        {
            await Sources.UpdateAsync(MaxParallelUpdates);
            await Subscriptions.UpdateAsync(MaxParallelUpdates);
        }
    }
}
