using MudBlazor;

namespace SoopWorkshop.Frontend.Web.Services;

/// <summary>
/// Das Farbsystem aus DESIGN.md, uebersetzt in die Paletten von MudBlazor.
/// </summary>
/// <remarks>
/// <para>
/// Farbwerte stehen <b>genau hier</b> und nirgends sonst. MudBlazor erzeugt aus der Palette
/// die <c>--mud-palette-*</c>-Variablen, auf die das eigene CSS zugreift; eine zweite
/// Farbliste in <c>app.css</c> waere eine zweite Wahrheit, die still auseinanderlaeuft.
/// Alles, was MudBlazor nicht kennt - Radien-Vokabular, Spacing, Tint-Toene - steht in
/// <c>wwwroot/app.css</c>.
/// </para>
/// <para>
/// Es gibt <b>ein</b> Theme mit beiden Paletten, nicht je eins pro Modus. Der
/// <c>MudThemeProvider</c> waehlt ueber <c>IsDarkMode</c> aus. Frueher trug jedes Theme-Objekt
/// nur eine gefuellte Palette - lief das Flag dagegen, kamen kommentarlos die
/// MudBlazor-Standardfarben heraus statt eines sichtbaren Fehlers.
/// </para>
/// </remarks>
public static class AppThemes
{
    // --- DESIGN.md, Tokens - Colors (Light) ---
    private const string CanvasWhite = "#ffffff";
    private const string PaperMist = "#f5f5f5";
    private const string Ash = "#e5e5e5";
    private const string Smoke = "#d4d4d4";
    private const string MidnightInk = "#0a0a0a";
    private const string Charcoal = "#171717";
    private const string Steel = "#525252";
    private const string Fog = "#737373";
    private const string ElectricBlue = "#2563eb";
    private const string VividGreen = "#16a34a";
    private const string Tangerine = "#ea580c";
    private const string Lavender = "#7c3aed";
    private const string PrimaryActionFill = "#000000";

    // Rot fehlt in DESIGN.md - das Dokument kennt nur Green/Orange/Violet als Akzente.
    // Eine Auswertungsoberflaeche braucht aber eine Fehlerfarbe. Bewusst aus demselben
    // Farbregister wie der Rest der Palette gewaehlt, damit sie sich einfuegt.
    private const string Crimson = "#dc2626";

    // --- Dark: aus demselben Prinzip abgeleitet, da DESIGN.md nur Light beschreibt.
    //     Dunkles Canvas, Hairline-Rahmen statt Schatten, ein Akzent. Die Akzente sind
    //     eine Stufe heller, weil dieselben Toene auf dunklem Grund sonst absaufen. ---
    private const string DarkCanvas = "#0a0a0a";
    private const string DarkSurface = "#121212";
    private const string DarkPaper = "#171717";
    private const string DarkAsh = "#262626";
    private const string DarkSmoke = "#404040";
    private const string DarkTextPrimary = "#ededed";
    private const string DarkTextSecondary = "#a3a3a3";
    private const string DarkElectricBlue = "#3b82f6";
    private const string DarkVividGreen = "#22c55e";
    private const string DarkTangerine = "#fb923c";
    private const string DarkCrimson = "#f87171";

    // --- Schriften. DESIGN.md nennt Satoshi fuer Display und Geist Mono fuer Code,
    //     beides keine frei per CDN geladenen Schriften - und geladen wird hier ohnehin
    //     nichts von aussen (workshop-intern, moeglicherweise ohne Internet). Wir nehmen
    //     die im Dokument selbst genannten Ersatzschriften. Der Unterschied zwischen
    //     Display und Body ist damit die Laufweite, siehe DisplayLetterSpacing. ---
    private static readonly string[] SansStack =
    [
        "Inter", "ui-sans-serif", "system-ui", "-apple-system", "Segoe UI", "Roboto", "sans-serif"
    ];

    private static readonly string[] MonoStack =
    [
        "ui-monospace", "SFMono-Regular", "Menlo", "Monaco", "Consolas", "monospace"
    ];

    // DESIGN.md fuer die Satoshi-Ersetzung: "Inter (weight 500, letter-spacing -0.02em)".
    // Nur ab 36px, darunter bleibt die Laufweite normal.
    private const string DisplayLetterSpacing = "-0.02em";

    /// <summary>Das Theme der Anwendung. Licht und Dunkel in einem Objekt.</summary>
    public static readonly MudTheme Soop = new()
    {
        PaletteLight = new PaletteLight
        {
            // Flaechen
            Background = CanvasWhite,
            Surface = CanvasWhite,
            BackgroundGray = PaperMist,
            AppbarBackground = CanvasWhite,
            DrawerBackground = CanvasWhite,

            // Linien - der tragende Teil des Systems. DESIGN.md definiert Container
            // ueber 1px-Kanten, nicht ueber Schatten.
            LinesDefault = Ash,
            LinesInputs = Smoke,
            Divider = Ash,
            DividerLight = Ash,
            TableLines = Ash,

            // Text
            TextPrimary = Charcoal,
            TextSecondary = Steel,
            TextDisabled = Fog,
            AppbarText = Charcoal,
            DrawerText = Charcoal,
            DrawerIcon = Steel,

            // Akzent - Links, aktive Zustaende, Fortschritt, Fokus.
            Primary = ElectricBlue,
            PrimaryContrastText = CanvasWhite,
            Secondary = Lavender,
            SecondaryContrastText = CanvasWhite,
            Tertiary = Tangerine,
            TertiaryContrastText = CanvasWhite,
            Info = ElectricBlue,
            InfoContrastText = CanvasWhite,

            // Zustaende
            Success = VividGreen,
            SuccessContrastText = CanvasWhite,
            Warning = Tangerine,
            WarningContrastText = CanvasWhite,
            Error = Crimson,
            ErrorContrastText = CanvasWhite,

            // Die eine gefuellte Aktion pro Flaeche. DESIGN.md: schwarze Fuellung, weisser
            // Text - "used once per surface for the primary conversion goal". Sie liegt
            // bewusst auf Color.Dark und nicht auf Primary: Primary ist das Akzentblau und
            // faerbt Links, Nav und Fortschrittsbalken mit.
            Dark = PrimaryActionFill,
            DarkContrastText = CanvasWhite
        },

        PaletteDark = new PaletteDark
        {
            Background = DarkCanvas,
            Surface = DarkSurface,
            BackgroundGray = DarkPaper,
            AppbarBackground = DarkCanvas,
            DrawerBackground = DarkCanvas,

            LinesDefault = DarkAsh,
            LinesInputs = DarkSmoke,
            Divider = DarkAsh,
            DividerLight = DarkAsh,
            TableLines = DarkAsh,

            TextPrimary = DarkTextPrimary,
            TextSecondary = DarkTextSecondary,
            TextDisabled = Fog,
            AppbarText = DarkTextPrimary,
            DrawerText = DarkTextPrimary,
            DrawerIcon = DarkTextSecondary,

            Primary = DarkElectricBlue,
            PrimaryContrastText = DarkCanvas,
            Secondary = Lavender,
            SecondaryContrastText = CanvasWhite,
            Tertiary = DarkTangerine,
            TertiaryContrastText = DarkCanvas,
            Info = DarkElectricBlue,
            InfoContrastText = DarkCanvas,

            Success = DarkVividGreen,
            SuccessContrastText = DarkCanvas,
            Warning = DarkTangerine,
            WarningContrastText = DarkCanvas,
            Error = DarkCrimson,
            ErrorContrastText = DarkCanvas,

            // Dreht sich um: auf dunklem Grund ist Hell das Kontraststaerkste. Die Rolle
            // bleibt dieselbe - die eine Aktion, die aus der Flaeche heraussticht.
            Dark = DarkTextPrimary,
            DarkContrastText = DarkCanvas
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = SansStack,
                FontSize = "16px",
                FontWeight = "400",
                LineHeight = "1.5"
            },

            // Display: nur ab 36px, mit engerer Laufweite.
            H1 = new H1Typography
            {
                FontFamily = SansStack, FontSize = "48px", FontWeight = "500",
                LineHeight = "1", LetterSpacing = DisplayLetterSpacing
            },
            H2 = new H2Typography
            {
                FontFamily = SansStack, FontSize = "48px", FontWeight = "500",
                LineHeight = "1", LetterSpacing = DisplayLetterSpacing
            },
            H3 = new H3Typography
            {
                FontFamily = SansStack, FontSize = "36px", FontWeight = "500",
                LineHeight = "1.11", LetterSpacing = DisplayLetterSpacing
            },

            // Ab hier normale Laufweite - DESIGN.md: "switch to Inter for everything 30px and below".
            H4 = new H4Typography
            {
                FontFamily = SansStack, FontSize = "30px", FontWeight = "500", LineHeight = "1.38"
            },
            H5 = new H5Typography
            {
                FontFamily = SansStack, FontSize = "24px", FontWeight = "500", LineHeight = "1.33"
            },
            H6 = new H6Typography
            {
                FontFamily = SansStack, FontSize = "20px", FontWeight = "500", LineHeight = "1.4"
            },

            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = SansStack, FontSize = "18px", FontWeight = "500", LineHeight = "1.56"
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = SansStack, FontSize = "14px", FontWeight = "600", LineHeight = "1.43"
            },

            Body1 = new Body1Typography
            {
                FontFamily = SansStack, FontSize = "16px", FontWeight = "400", LineHeight = "1.5"
            },
            Body2 = new Body2Typography
            {
                FontFamily = SansStack, FontSize = "14px", FontWeight = "400", LineHeight = "1.43"
            },

            // Versalien passen nicht zu diesem System - DESIGN.md zeigt Buttons in
            // normaler Schreibweise, 14px, Gewicht 500.
            Button = new ButtonTypography
            {
                FontFamily = SansStack, FontSize = "14px", FontWeight = "500",
                LineHeight = "1.43", TextTransform = "none"
            },

            // Caption traegt bei uns die Code-Ausgaben (siehe SubmissionResult.razor.css,
            // das --mud-typography-caption-family liest) - deshalb Monospace.
            Caption = new CaptionTypography
            {
                FontFamily = MonoStack, FontSize = "13px", FontWeight = "400", LineHeight = "1.43"
            },
            Overline = new OverlineTypography
            {
                FontFamily = SansStack, FontSize = "11px", FontWeight = "500",
                LineHeight = "1.5", TextTransform = "none"
            }
        },

        LayoutProperties = new LayoutProperties
        {
            // Karten-Radius als Standard; Buttons, Chips und Eingaben weichen davon ab und
            // werden in app.css nachgezogen - MudBlazor kennt nur diesen einen Wert.
            DefaultBorderRadius = "12px"
        }
    };
}
