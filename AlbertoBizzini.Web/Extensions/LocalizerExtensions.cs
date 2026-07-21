using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Components;

namespace AlbertoBizzini.Web.Extensions
{
    public static class LocalizerExtensions
    {
        /// <summary>
        /// Restituisce un MarkupString dal testo localizzato con placeholders, permettendo HTML nei valori sostituiti.
        /// </summary>
        public static MarkupString FormatWithMarkup(this IStringLocalizer localizer, string key, params object[] args)
        {
            if (localizer == null) return new MarkupString(string.Empty);

            var text = localizer[key];
            var formatted = string.Format(text, args);
            return new MarkupString(formatted);
        }
    }
}
