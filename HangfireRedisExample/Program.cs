using Hangfire;
using HangfireRedisExample.Extensions;

void ConfigureServices(WebHostBuilderContext webHostBuilderContext, IServiceCollection services)
{
    services.AddHangfire(webHostBuilderContext.Configuration);

}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.WebHost.ConfigureServices(ConfigureServices);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("Hello world");
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapHangfireDashboard(true);
});
app.UseHangfire();


await app.RunAsync();
