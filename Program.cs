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

app.MapGet("/auth/callback", async (Microsoft.AspNetCore.Http.HttpContext ctx, IServiceProvider serviceProvider) =>
{
    var code = ctx.Request.Query["code"].ToString();
    
    if (string.IsNullOrEmpty(code))
    {
        return Results.Text(@"<html><body>
            <h3>Error</h3>
            <p>No authorization code received.</p>
            <p><a href=""/"">Return to home</a></p>
            </body></html>", "text/html");
    }

    // Create a scope to resolve scoped services
    using var scope = serviceProvider.CreateScope();
    var upstoxService = scope.ServiceProvider.GetRequiredService<UpstoxService>();
    
    var success = await upstoxService.ExchangeCodeForTokenAsync(code);
    
    if (success)
    {
        // Return HTML page that closes parent tab (the /auth page) and redirects current tab to /intraday
        return Results.Text(@"<html><head><title>Authentication Successful</title></head><body>
            <script>
                (function() {
                    // Close the parent tab (the /auth page that opened this login tab)
                    if (window.opener && !window.opener.closed) {
                        try {
                            window.opener.close();
                        } catch (e) {
                            console.log('Could not close parent window:', e);
                        }
                    }
                    
                    // Small delay to ensure parent closes, then redirect this tab to /intraday
                    setTimeout(function() {
                        window.location.href = '/intraday';
                    }, 100);
                })();
            </script>
            <h3>Authentication Successful!</h3>
            <p>Token received and saved. Closing parent tab and redirecting...</p>
            <p>If you don't get redirected, <a href=""/intraday"">click here</a>.</p>
            </body></html>", "text/html");
    }
    else
    {
        return Results.Text(@"<html><body>
            <h3>Error</h3>
            <p>Failed to exchange authorization code for access token.</p>
            <p>Please try again.</p>
            <p><a href=""/"">Return to home</a></p>
            </body></html>", "text/html");
    }
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
