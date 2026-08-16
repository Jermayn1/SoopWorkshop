using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using SoopWorkshop.Frontend.Services.StateManagement;

namespace SoopWorkshop.Frontend.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    // Schluessel, unter dem die Theme-Wahl vom Vorabrendern in den interaktiven
    // Kreislauf gereicht wird.
    private const string ThemeStateKey = "soop-theme";

    [Inject] private ThemeService ThemeService { get; set; } = default!;
    [Inject] private PersistentComponentState ApplicationState { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // Nur waehrend des Vorabrenderns nutzbar - im laufenden Kreislauf gibt es keinen
    // HttpContext mehr. Genau dafuer ist der Umweg ueber PersistentComponentState da,
    // siehe RestoreTheme.
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private PersistingComponentStateSubscription _persistSubscription;
    private ErrorBoundary? _errorBoundary;
    private bool _drawerOpen = true;

    protected override void OnInitialized()
    {
        ThemeService.Initialize(RestoreTheme());

        // Reicht die Wahl vom Vorabrendern an den Kreislauf weiter. Ohne das rendert der
        // Server erst korrekt und der Kreislauf danach wieder hell - genau das Flackern,
        // das das Cookie vermeiden soll.
        _persistSubscription = ApplicationState.RegisterOnPersisting(() =>
        {
            ApplicationState.PersistAsJson(ThemeStateKey, ThemeService.CurrentTheme);
            return Task.CompletedTask;
        });

        ThemeService.OnThemeChanged += OnThemeChanged;
        Navigation.LocationChanged += OnLocationChanged;
    }

    // Bewusst am Seitenwechsel und nicht in OnParametersSet: entsteht der Fehler beim
    // Rendern selbst, wuerde ein Zuruecksetzen bei jedem Render sofort denselben Fehler
    // ausloesen und sich im Kreis drehen.
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(() =>
        {
            _errorBoundary?.Recover();
            StateHasChanged();
        });

    private void RecoverFromError() => _errorBoundary?.Recover();

    /// <summary>
    /// Ermittelt das Theme fuer diesen Durchlauf: im Kreislauf aus dem uebergebenen
    /// Zustand, beim Vorabrendern aus dem Cookie.
    /// </summary>
    private AppTheme RestoreTheme()
    {
        if (ApplicationState.TryTakeFromJson<AppTheme>(ThemeStateKey, out var persisted))
            return persisted;

        var cookieValue = HttpContextAccessor.HttpContext?.Request.Cookies[ThemeCookie.Name];
        return ThemeCookie.Parse(cookieValue);
    }

    private async Task ToggleThemeAsync()
    {
        var next = ThemeService.IsDarkMode ? AppTheme.Light : AppTheme.Dark;
        ThemeService.SetTheme(next);

        // Das Cookie wird im Browser gesetzt, nicht ueber eine HTTP-Antwort: an dieser
        // Stelle laeuft laengst nur noch die Verbindung, es gibt keine Antwort mehr,
        // an die sich ein Set-Cookie haengen liesse.
        await JS.InvokeVoidAsync("soopTheme.set", ThemeCookie.ToCookieValue(next));
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    public void Dispose()
    {
        ThemeService.OnThemeChanged -= OnThemeChanged;
        Navigation.LocationChanged -= OnLocationChanged;
        _persistSubscription.Dispose();
    }
}
