namespace SoopWorkshop.Shared.Constants
{
    public static class TaskBundleFormat
    {
        // Version 1: Kategorien mit Symbol, Aufgaben mit Vertrag ueber mehrere
        // Klassen, Konsolen-Testfaelle, JUnit-Dateien und Gewichte.
        //
        // Wird nur erhoeht, wenn eine aeltere Datei nicht mehr gelesen werden
        // kann. Ein zusaetzliches Feld allein ist kein Grund - das faellt beim
        // Einlesen einfach auf seinen Standardwert.
        public const int CurrentVersion = 1;

        // Vorschlag fuer den Dateinamen beim Herunterladen.
        public const string FileNamePrefix = "soop-bestand";
    }
}
