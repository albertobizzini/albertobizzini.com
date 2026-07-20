using AlbertoBizzini.Api;
using AlbertoBizzini.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

#region AppSettings handlers

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));

builder.Services.Configure<TurnstileOptions>(
    builder.Configuration.GetSection(TurnstileOptions.SectionName));

builder.Services.Configure<RateLimiterOptions>(
    builder.Configuration.GetSection(RateLimiterOptions.SectionName));

#endregion

#region Rate limiting

var rateLimiterOptions = builder.Configuration
    .GetSection(RateLimiterOptions.SectionName)
    .Get<RateLimiterOptions>()
    ?? new RateLimiterOptions();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimiterPolicies.Contact, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.GetClientIp(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimiterOptions.PermitLimit,
                Window =  TimeSpan.FromMinutes(rateLimiterOptions.WindowMinutes),
                QueueLimit = rateLimiterOptions.QueueLimit,
                AutoReplenishment = rateLimiterOptions.AutoReplenishment
            }));

    options.OnRejected = async (context, token) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogWarning(
            "Rate limit exceeded from IP {Ip}",
            context.HttpContext.GetClientIp());

        await Task.CompletedTask;
    };

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

#endregion

#region Application services

builder.Services.AddTransient<IContactService, ContactService>();
builder.Services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddTransient<IEmailSender, Smtp2GoEmailSender>();

#endregion

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();


builder.Services.Configure<CorsOptions>(
    builder.Configuration.GetSection(CorsOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        var origins = builder.Configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()?
            .AllowedOrigins ?? [];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("BlazorClient");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
