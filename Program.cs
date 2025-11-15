using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IntradayUpstox.Services;

var builder = WebApplication.CreateBuilder(args);

// server interactive
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<TokenStore>();
builder.Services.AddScoped<UpstoxService>();
builder.Services.AddSingleton<FeatureService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapGet("/auth/callback", (Microsoft.AspNetCore.Http.HttpContext ctx) =>
{
    var code = ctx.Request.Query["code"].ToString();
    return Results.Text($@"<html><body>
        <h3>Authorization code</h3>
        <p>Code: <b>{code}</b></p>
        <p>Copy this code and paste it into the Blazor app <a href=""/"">home</a>.</p>
        </body></html>", "text/html");
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
