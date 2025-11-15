# IntradayUpstox Blazor Server Demo

This project demonstrates integration with Upstox REST APIs to fetch intraday candles and compute a simple intraday score.

## Setup

1. Register an app on Upstox Developer portal and get **client_id** and **client_secret**.
2. Set `Upstox:ClientId`, `Upstox:ClientSecret`, and `Upstox:RedirectUri` in `appsettings.json`.
   - RedirectUri must match what you configure in Upstox app (example: http://localhost:5000/auth/callback).
3. Run the app: `dotnet run`
4. Open `/auth` to open the Upstox login and paste the returned `code` into the app to exchange for an access token.
5. Open `/intraday` to fetch real intraday candles and get a suggested score for the instrument.

## Notes
- Access tokens are stored only in-memory (TokenStore). For production, persist securely (Key Vault / database).
- Upstox WebSocket market feed and order placement are not included.
- This sample uses the `historical-candle/intraday` endpoint. Adjust parsing if Upstox response differs.
