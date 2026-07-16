using AlbertoBizzini.Web.Services;
using AlbertoBizzini.Web.ViewModels;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using System.Globalization;

namespace AlbertoBizzini.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSingleton<KindleClippingService>();
        services.AddScoped<ContactFormViewModel>();
        services.AddScoped<ContactFormViewModelFluentValidator>();

        return services;
    }

    public static async Task ConfigureCultureAsync(this WebAssemblyHost host)
    {
        var js = host.Services.GetRequiredService<IJSRuntime>();
        var culture = await js.InvokeAsync<string>("blazorCulture.get") ?? "en";
        var ci = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
    }
}
