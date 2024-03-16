using Serilog;
using WebApiClient.interfaces;

namespace WebApiClient;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var configuration = builder.Build();
        var watermarkSettings = configuration.GetSection("Watermark").Get<WatermarkSettings>();
        services.AddSingleton(watermarkSettings);
        var resizeSettings = configuration.GetSection("Resize").Get<ResizeSettings>();
        services.AddSingleton(resizeSettings);
        // Add framework services.
        services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.UseDirectoryBrowser();

        // Create log folder if it doesn't exist
        var logFolder = Path.Combine(env.ContentRootPath, "logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }

        // Configure Serilog for logging to file
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logFolder, $"log-{DateTime.Now:yyyyMMdd}.txt"))
            .CreateLogger();


        // Add Serilog to the logger factory
        loggerFactory.AddSerilog();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}