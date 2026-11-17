using System.Globalization;
using Microsoft.AspNetCore.Localization;
using QuizCiel.Web;
using QuizCiel.Web.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews();
builder.Services.AddQuizData(builder.Configuration);
builder.Services.AddQuizServices(builder.Configuration);
builder.Services.AddQuizIdentity();

var app = builder.Build();
await app.SeedAccountsAsync();

var colombianCulture = new CultureInfo("es-CO");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(colombianCulture),
    SupportedCultures = [colombianCulture],
    SupportedUICultures = [colombianCulture]
});

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseSerilogRequestLogging();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers().WithStaticAssets();

app.Run();

/// <summary>
/// Entry point marker, exposed so integration tests can reference the application.
/// </summary>
public partial class Program
{
}
