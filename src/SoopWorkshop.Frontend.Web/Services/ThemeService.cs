namespace SoopWorkshop.Frontend.Web.Services
{

    public enum AppTheme { Light, Dark, Oled }

    // Scope, um das aktuelle Theme zu verwalten, sodass alle Komponenten denselben State teilen
    public class ThemeService
    {
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public event Action? OnThemeChanged;

        public void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            OnThemeChanged?.Invoke();
        }

        public bool IsDarkMode => CurrentTheme is AppTheme.Dark or AppTheme.Oled;
    }
}
