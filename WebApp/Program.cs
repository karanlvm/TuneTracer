using System.IO;
using CoreApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

var builder = WebApplication.CreateBuilder(args);

// 1) Ensure appsettings.json from WebApp/ is loaded
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var config = builder.Configuration;

// 2) Sanity check for Spotify creds
var clientId     = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    throw new Exception("❌ SPOTIFY_CLIENT_ID / SPOTIFY_CLIENT_SECRET environment variables are not set");


// 3) Register the fingerprint database
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "fingerprints.db");
builder.Services.AddSingleton<Database>(_ => new Database(dbPath));

// 4) Register SpotifyClient via Client Credentials
builder.Services.AddSingleton<SpotifyClient>(_ =>
{
    var oauth = new OAuthClient();
    var token = oauth
        .RequestToken(new ClientCredentialsRequest(clientId, clientSecret))
        .GetAwaiter().GetResult();

    return new SpotifyClient(token.AccessToken);
});
// 5) Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// 6) Middleware
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
