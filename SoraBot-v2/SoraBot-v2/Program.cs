﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using SoraBot_v2.Services;

namespace SoraBot_v2
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        #region Private Fields

        private DiscordSocketClient _client;
        //private CommandHandler _commands;
        //private SoraContext _soraContext;
        private InteractiveService _interactive;
        private AutoReconnectService _autoReconnectService;
        
        //// Disabled by Catherine Renelle - Memory Leak Fix
        ////private string _connectionString;
        #endregion

        public async Task MainAsync(string[] args)
        {
            Console.WriteLine(args.Join(", "));
            int shardId;
            if (!int.TryParse(args[0], out shardId))
            {
                throw new Exception("INVALID SHARD ARGUMENT");
            }

            //Setup config
            ConfigService.InitializeLoader();
            ConfigService.LoadConfig();

            if (!int.TryParse(ConfigService.GetConfigData("shardCount"), out Utility.TOTAL_SHARDS))
            {
                throw new Exception("INVALID SHARD COUNT");
            }

            //setup discord client
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 0,
                TotalShards = Utility.TOTAL_SHARDS,
                ShardId = shardId
            });

            _client.Log += Log;



            //setup DB

            /****
             * Disabled by Catherine Renelle - Memory leak fix
            if (!ConfigService.GetConfig().TryGetValue("connectionString", out _connectionString))
            {
                throw new IOException
                {
                    Source = "COULDNT FIND CONNECTION STRING FOR DB!"
                };
            }****/

            Utility.SORA_VERSION = ConfigService.GetConfigData("version");


            //_soraContext = new SoraContext(_connectionString);
            //await _soraContext.Database.EnsureCreatedAsync();

            //Setup Services
            ProfileImageGeneration.Initialize();
            _interactive = new InteractiveService(_client);
            //Create dummy commandHandler for dependency Injection
            //_commands = new CommandHandler();
            //Instantiate the dependency map and add our services and client to it
            var serviceProvider = ConfigureServices();

            //setup command handler
            await serviceProvider.GetRequiredService<CommandHandler>().InitializeAsync(serviceProvider);
            //_commands.ConfigureCommandHandler(serviceProvider);
            //await _commands.InstallAsync();

            //SETUP other dependency injection services
            serviceProvider.GetRequiredService<ReminderService>().Initialize();
            await serviceProvider.GetRequiredService<WeebService>().InitializeAsync();
            serviceProvider.GetRequiredService<StarboardService>().Initialize(); 
            serviceProvider.GetRequiredService<RatelimitingService>().SetTimer();


            //Set up an event handler to execute some state-reliant startup tasks
            _client.Ready += async () =>
            {
                SentryService.Install(_client);
            };
            string token = "";
            ConfigService.GetConfig().TryGetValue("token2", out token);

            //Connect to Discord
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // initialize Autoreconnect Feature
            _autoReconnectService = new AutoReconnectService(_client, LogPretty);
            //build webserver and inject service
            try
            {
                var host = new WebHostBuilder()
                    .UseKestrel() // MVC webserver is called Kestrel when self hosting
                    .UseUrls("http://localhost:" + (8087+shardId)) // Bind to localhost:port to allow http:// calls. TODO ADD WEBPORT
                    .UseContentRoot(Directory.GetCurrentDirectory() + @"/web/") // Required to be set and exist. Create web folder in the folder the bot runs from. Folder can be empty.
                    .UseWebRoot(Directory.GetCurrentDirectory() + @"/web/") // Same as above.
                    .UseStartup<Startup>() // Use Startup class in Startup.cs
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(_client); // Injected Discord client
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowLocal", builder => builder.WithOrigins("localhost")); // Enable CORS to only allow calls from localhost
                        });
                        services.AddMvc().AddJsonOptions( options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore ); // Fixes JSON Recursion issues in API response.
                    })
                    .Build(); // Actually creates the webhost

                await host.RunAsync(); // Run in tandem to client
                Console.WriteLine("WEB API STARTED ON PORT: 8087");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await SentryService.SendMessage(e.ToString());
            }

            //INITIALIZE CACHE
            CacheService.Initialize();

            //Hang indefinitely
            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_client);
            services.AddSingleton<CommandService>();
            services.AddSingleton<ClanService>();

            //// Disabled by Catherine Renelle - Memory leak fix
            ////services.AddDbContext<SoraContext>(options => options.UseMySql(_connectionString),ServiceLifetime.Transient);//, ServiceLifetime.Transient

            services.AddSingleton<CommandHandler>();
            services.AddSingleton(_interactive);
            services.AddSingleton<InteractionsService>();
            services.AddSingleton<AfkService>();
            services.AddSingleton<DynamicPrefixService>();
            services.AddSingleton<RatelimitingService>();
            services.AddSingleton<MusicShareService>();
            services.AddSingleton<SelfAssignableRolesService>();
            services.AddSingleton<WeatherService>();
            services.AddSingleton<AnnouncementService>();
            services.AddSingleton<MarriageService>();
            services.AddSingleton<StarboardService>();
            services.AddSingleton<GiphyService>();
            services.AddSingleton<WeebService>();
            services.AddSingleton<GuildLevelRoleService>();
            services.AddSingleton<ModService>();
            services.AddSingleton<ProfileService>();
            services.AddSingleton<ReminderService>();
            services.AddSingleton<GuildCountUpdaterService>();
            services.AddSingleton<UbService>();
            services.AddSingleton<ImdbService>();
            services.AddSingleton<ExpService>();
            services.AddSingleton<TagService>();
            services.AddSingleton<AnimeSearchService>();


            return new DefaultServiceProviderFactory().CreateServiceProvider(services);
        }

        // Example of a logging handler. This can be re-used by addons
    // that ask for a Func<LogMessage, Task>.
    private static Task LogPretty(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }
        Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
        Console.ResetColor();
        
        // If you get an error saying 'CompletedTask' doesn't exist,
        // your project is targeting .NET 4.5.2 or lower. You'll need
        // to adjust your project's target framework to 4.6 or higher
        // (instructions for this are easily Googled).
        // If you *need* to run on .NET 4.5 for compat/other reasons,
        // the alternative is to 'return Task.Delay(0);' instead.
        return Task.CompletedTask;
    }

        private Task Log(LogMessage m)
        {
            switch (m.Severity)
            {
                case LogSeverity.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LogSeverity.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                case LogSeverity.Critical: Console.ForegroundColor = ConsoleColor.DarkRed; break;
                case LogSeverity.Verbose: Console.ForegroundColor = ConsoleColor.White; break;
            }

            Console.WriteLine(m.ToString());
            if (m.Exception != null)
                Console.WriteLine(m.Exception);
            Console.ForegroundColor = ConsoleColor.Gray;

            return Task.CompletedTask;
        }

    }
}