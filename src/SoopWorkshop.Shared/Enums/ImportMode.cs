using System.Text.Json.Serialization;

namespace SoopWorkshop.Shared.Enums
{
    // Wie ein Import mit dem vorhandenen Bestand umgeht.
    //
    // Ueber die Leitung als Zeichenkette, und der Konverter steht hier am Typ und
    // nicht in AddJsonOptions: eine globale Registrierung wirkt nur zur Laufzeit,
    // der OpenAPI-Erzeuger liest den Typ. Beides getrennt zu pflegen hiesse, zwei
    // Wahrheiten zu haben (§9, Fund aus Phase 4).
    [JsonConverter(typeof(JsonStringEnumConverter<ImportMode>))]
    public enum ImportMode
    {
        // Was dieselbe Id hat, wird aktualisiert; alles Neue kommt dazu. Nichts
        // wird geloescht - eine zuhause geloeschte Aufgabe bleibt auf dem Server
        // also bestehen.
        Merge,

        // Der Bestand wird geleert, danach ist die Datei die Wahrheit.
        //
        // Achtung: das Loeschen einer Kategorie nimmt per Cascade alles mit, was
        // darunter haengt - einschliesslich der Abgaben der Teilnehmer und ihrer
        // Auswertungen. Der Bericht nennt die Zahl vorher.
        Replace
    }
}
