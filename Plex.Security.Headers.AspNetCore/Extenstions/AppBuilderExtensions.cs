﻿using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Net.Http.Headers;
using Plex.Security.Headers.AspNetCore.Middlewares;

namespace Plex.Security.Headers.AspNetCore.Extenstions;
public static class AppBuilderExtensions
{
    public static IApplicationBuilder UseStrictTransportSecurity(
                                        this IApplicationBuilder app,
                                        HstsOptions hstsOptions)
    {
        return app.UseMiddleware<StrictTransportSecurityMiddleware>(hstsOptions);
    }
    public static IApplicationBuilder UseStrictTransportSecurity(this IApplicationBuilder app)
    {
        return app.UseMiddleware<StrictTransportSecurityMiddleware>();
    }
    public static IApplicationBuilder UseCps(this IApplicationBuilder app,
                                            string cpsHeader,
                                            string? nonceValue = null,
                                            bool isSpaApp = false)
    {
        nonceValue ??= Convert.ToBase64String(GenerateRandomNonce(16));
        return app.UseMiddleware<ContentSecurityPolicyMiddleware>(cpsHeader, nonceValue, isSpaApp);
    }
    public static IApplicationBuilder UseRemoveInsecureHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RemoveInsecureHeadersMiddleware>();
    }
    public static IApplicationBuilder UseCacheControl(this IApplicationBuilder app,
                                                      TimeSpan? cacheMaxAge = null,
                                                      TimeSpan? cacheMaxAgeStaticFiles = null,
                                                      bool cacheHttpGetMethods = false)
    {
        if (cacheMaxAge == null) cacheMaxAge = TimeSpan.FromMinutes(60);
        if (cacheMaxAgeStaticFiles == null) cacheMaxAgeStaticFiles = TimeSpan.FromDays(365);
        return app.UseMiddleware<CacheControlMiddleware>(cacheMaxAge, cacheMaxAgeStaticFiles, cacheHttpGetMethods);
    }
    public static IApplicationBuilder UseXHeaders(this IApplicationBuilder app, bool addXFrameOptions = true)
    {
        return app.UseMiddleware<XHeadersMiddleware>(addXFrameOptions);
    }
    public static IApplicationBuilder UseNoHtmlCacheControl(this IApplicationBuilder app)
    {
        return app.UseMiddleware<NoHtmlCacheControlMiddleware>();
    }
    public static IApplicationBuilder UseListEndpoints(this IApplicationBuilder app, EndpointDataSource endpointDataSource)
    {
        return app.UseMiddleware<ListEndpointsMiddleware>(endpointDataSource);
    }
    public static IApplicationBuilder UseCspMeta(this IApplicationBuilder app,
                                                 string cpsHeader,
                                                 string nonceValue)
    {
        return app.UseMiddleware<ContentSecurityPolicyMetaMiddleware>(cpsHeader, nonceValue);
    }
    public static IApplicationBuilder UseStaticFilesAndCache(this IApplicationBuilder app,
                                                             TimeSpan? cacheMaxAge = null)
    {
        if (cacheMaxAge == null) cacheMaxAge = TimeSpan.FromDays(100);
        // Cache static files
        string maxAgeSeconds = Convert.ToInt64(Math.Ceiling(cacheMaxAge.Value.TotalSeconds)).ToString("R", CultureInfo.InvariantCulture);
        string maxAgeDays = DateTime.UtcNow.AddDays(Convert.ToInt64(Math.Ceiling(cacheMaxAge.Value.TotalDays))).ToString("R", CultureInfo.InvariantCulture);
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.Remove(HeaderNames.CacheControl);
                ctx.Context.Response.Headers.Remove(HeaderNames.Expires);

                ctx.Context.Response.Headers.Append(HeaderNames.CacheControl, $"public, max-age={maxAgeSeconds}, immutable");
                ctx.Context.Response.Headers.Append(HeaderNames.Expires, maxAgeDays);
            }
        });

        return app;
    }
    static byte[] GenerateRandomNonce(int length)
    {
        // Create a byte array to hold the random nonce
        byte[] nonce = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            // Fill the nonce array with random data
            rng.GetBytes(nonce);
        }

        return nonce;
    }
}
