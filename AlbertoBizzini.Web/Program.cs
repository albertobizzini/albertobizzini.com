using AlbertoBizzini.Web;
using AlbertoBizzini.Web.Services;
using AlbertoBizzini.Web.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using MudBlazor.Translations;
using Soenneker.Blazor.Turnstile.Registrars;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection(ApiOptions.SectionName));

builder.Services.AddMudServices();
builder.Services.AddMudTranslations();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddTurnstileInteropAsScoped();

builder.Services.AddAppServices();

var host = builder.Build();

await host.ConfigureCultureAsync();

await host.RunAsync();
