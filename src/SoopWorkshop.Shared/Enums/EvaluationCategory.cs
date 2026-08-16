using System.Text.Json.Serialization;

namespace SoopWorkshop.Shared.Enums
{
    // Die Werte liegen als int in der Datenbank. Neue Kategorien werden deshalb
    // ausschliesslich angehaengt - wird umsortiert, deutet das Altbestaende um.
    //
    // Ueber die Leitung gehen sie dagegen als Zeichenkette. Der Konverter steht
    // bewusst am Typ und nicht nur global in AddJsonOptions: der OpenAPI-Erzeuger
    // liest den Typ, die globale Registrierung nur die Laufzeit. Stand beides nicht
    // im Einklang, verschickte die API "Easy", waehrend der erzeugte Vertrag eine
    // Zahl versprach - und das Frontend haette sich still darauf verlassen.
    [JsonConverter(typeof(JsonStringEnumConverter<EvaluationCategory>))]
    public enum EvaluationCategory
    {
        // Altlast: seit der Bewertungs-Engine v2 sind Zeichensatz und
        // Namenskonventionen Teilpruefungen unter CleanCode. Die beiden Werte
        // werden nicht mehr vergeben, bleiben aber stehen, damit fruehere
        // Auswertungen weiterhin richtig gelesen werden. Nicht wiederverwenden.
        CharacterSet,
        NamingConventions,

        Compilability,
        CleanCode,

        // Altlast: Konsolen-Testfaelle und Aufgaben-Unittests pruefen dasselbe -
        // ob das Programm tut, was die Aufgabe verlangt. Sie waren kurzzeitig
        // getrennte Kategorien; das war eine Doppelung in der Anzeige und in der
        // Gewichtung. Beide zahlen jetzt auf Functionality ein.
        TestCases,
        UnitTests,

        // Erfuellt das Programm die Aufgabe? Speist sich aus Konsolen-Testfaellen
        // und JUnit-Testmethoden - welcher Weg genutzt wird, entscheidet die
        // Aufgabe ueber ihren EvaluationMode.
        Functionality,
    }
}
