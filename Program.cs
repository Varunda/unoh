using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using unoh.config;

namespace unoh {

    public class Program {

        static void Main(string[] args) {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            IServiceCollection services = builder.Services;
            builder.Configuration.AddJsonFile("secrets.json", optional: false);
            builder.Configuration.AddJsonFile("tourney.json", optional: false);
            builder.Logging.AddConsole(options => options.FormatterName = "OneLineLogger")
                .AddConsoleFormatter<OneLineLogger, OneLineFormatterOptions>(options => { });

            services.AddSingleton<Match>();
            services.AddSingleton<DiscordWrapper>();
            services.AddSingleton<MatchManager>();
            services.AddSingleton<MatchSteps>();
            services.AddHostedService<DiscordService>();

            services.Configure<DiscordOptions>(builder.Configuration.GetSection("Discord"));
            services.Configure<MatchConfig>(builder.Configuration.GetSection("Tourney"));

            using IHost host = builder.Build();
            host.Run();
        }

    }
}
