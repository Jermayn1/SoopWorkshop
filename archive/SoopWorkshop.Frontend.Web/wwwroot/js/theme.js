// Schreibt die Theme-Wahl ins Cookie. Gelesen wird sie serverseitig beim Vorabrendern,
// damit die Seite gleich in der richtigen Farbe erscheint - deshalb ein Cookie und nicht
// localStorage, an das der Server zu diesem Zeitpunkt nicht herankommt.
//
// Kein Zustimmungsbanner noetig: gespeichert wird eine Bedienvorliebe, keine Kennung.
window.soopTheme = {
    set: function (value) {
        var oneYearInSeconds = 60 * 60 * 24 * 365;
        document.cookie = "soop-theme=" + encodeURIComponent(value)
            + ";path=/;max-age=" + oneYearInSeconds + ";samesite=lax";
    }
};
