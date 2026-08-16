namespace SoopWorkshop.Frontend.Services.StateManagement;

/// <summary>
/// Name und Format des Cookies, in dem die Theme-Wahl ueberlebt.
/// </summary>
/// <remarks>
/// Gelesen wird serverseitig beim Vorabrendern, geschrieben im Browser per JavaScript
/// (<c>wwwroot/js/theme.js</c>) - im laufenden Blazor-Server-Kreislauf gibt es keine
/// HTTP-Antwort mehr, an die sich ein Set-Cookie haengen liesse. Damit beide Seiten
/// denselben Namen und dieselben Werte benutzen, stehen sie hier an einer Stelle.
/// </remarks>
public static class ThemeCookie
{
    public const string Name = "soop-theme";

    private const string LightValue = "light";
    private const string DarkValue = "dark";

    /// <summary>
    /// Liest den Cookie-Wert. Alles Unbekannte oder Fehlende faellt auf Hell zurueck.
    /// </summary>
    public static AppTheme Parse(string? value) =>
        string.Equals(value, DarkValue, StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Dark
            : AppTheme.Light;

    public static string ToCookieValue(AppTheme theme) =>
        theme is AppTheme.Dark ? DarkValue : LightValue;
}
