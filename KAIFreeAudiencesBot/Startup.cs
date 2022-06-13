using Telegram.Bot;
using KAIFreeAudiencesBot.Models;
using KAIFreeAudiencesBot.Services;
using KAIFreeAudiencesBot.Services.Database;

namespace KAIFreeAudiencesBot;

public class Startup
{
    public IConfiguration Configuration { get; }
    private BotConfiguration BotConfiguration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        BotConfiguration = Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();

        using var client = new SchDbContext();
        client.Database.EnsureCreated();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var httpClient = new HttpClient();
        //httpClient.Timeout = new TimeSpan(0, 5, 0);
        services.AddHttpClient("kfab_webhook").AddTypedClient<ITelegramBotClient>(client =>
            new TelegramBotClient(BotConfiguration.BotApiKey, httpClient));
        services.AddEntityFrameworkSqlite().AddDbContext<SchDbContext>();
        services.AddHostedService<ConfigureWebhook>();
        services.AddScoped<HandleUpdateService>();
        services.AddScoped<ScheduleParser>();
        services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            var token = BotConfiguration.BotApiKey;
            endpoints.MapControllerRoute(
                name: "tgbot",
                pattern: $"bot/{token}",
                new {controller = "Webhook", action = "Post"});
        });
    }
}
