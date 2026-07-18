using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AlbertoBizzini.Api;

public static class HttpContextExtensions
{
    /// <summary>
    /// Restituisce l'indirizzo IP del client.
    /// L'ordine di ricerca può essere modificato in base all'infrastruttura.
    /// </summary>
    public static string GetClientIp(this HttpContext context)
    {
        // 1. Cloudflare
        if (TryGetHeader(context, "CF-Connecting-IP", out var ip))
            return ip;

        // 2. Reverse proxy (Azure Front Door, Nginx, IIS, ecc.)
        if (TryGetHeader(context, "X-Forwarded-For", out ip))
        {
            // Il primo IP è quello del client originale
            return ip.Split(',', StringSplitOptions.TrimEntries)[0];
        }

        // 3. Connessione diretta
        if (context.Connection.RemoteIpAddress is not null)
            return context.Connection.RemoteIpAddress.ToString();

        // 4. Fallback (non deve essere una stringa constante perché usato nel "Rate limiting" (Vedi builder.Services.AddRateLimiter in Program.cs)
        return Guid.NewGuid().ToString();
    }

    private static bool TryGetHeader(
        HttpContext context,
        string headerName,
        out string value)
    {
        value = string.Empty;

        if (!context.Request.Headers.TryGetValue(headerName, out StringValues values))
            return false;

        value = values.ToString();

        return !string.IsNullOrWhiteSpace(value);
    }
}