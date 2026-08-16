namespace SoopWorkshop.Frontend.Services.StateManagement;

/// <summary>Die waehlbaren Erscheinungsbilder.</summary>
public enum AppTheme
{
    Light = 0,
    Dark = 1
}

/// <summary>
/// Haelt das gewaehlte Erscheinungsbild fuer die Dauer einer Verbindung.
/// </summary>
/// <remarks>
/// Bewusst ohne MudBlazor-Typen: der Dienst kennt nur die Auswahl, die Farben dazu stehen
/// in <c>AppThemes</c> im Web-Projekt. Persistiert wird ueber ein Cookie, siehe
/// <see cref="ThemeCookie"/> - der Server kennt die Wahl damit schon beim Vorabrendern und
/// die Seite baut sich nicht erst hell und dann dunkel auf.
/// </remarks>
public class ThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    /// <summary>Wird ausgeloest, wenn der Benutzer umschaltet.</summary>
    public event Action? OnThemeChanged;

    /// <summary>
    /// Setzt den Startwert, ohne <see cref="OnThemeChanged"/> auszuloesen.
    /// </summary>
    /// <remarks>
    /// Getrennt von <see cref="SetTheme"/>, weil beim Aufbau der Seite noch niemand auf das
    /// Ereignis hoert und ein Rendern zu diesem Zeitpunkt nur Arbeit ohne Wirkung waere.
    /// </remarks>
    public void Initialize(AppTheme theme) => CurrentTheme = theme;

    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme)
            return;

        CurrentTheme = theme;
        OnThemeChanged?.Invoke();
    }

    public bool IsDarkMode => CurrentTheme is AppTheme.Dark;
}
