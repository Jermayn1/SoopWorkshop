namespace SoopWorkshop.Frontend.Services.HttpClients;

/// <summary>
/// Ergebnis eines API-Aufrufs: Wert, "gibt es nicht" oder ein Grund zum Anzeigen.
/// </summary>
/// <remarks>
/// <para>
/// Es gibt drei Ausgaenge, und die Oberflaeche muss sie auseinanderhalten koennen:
/// <b>Erfolg</b>, <b>nicht vorhanden</b> (die Aufgabe wurde ausgeblendet - der Teilnehmer
/// soll das erfahren) und <b>fehlgeschlagen</b> (der Server antwortet nicht - hier hilft
/// nur ein zweiter Versuch).
/// </para>
/// <para>
/// Ein blosses <c>null</c> hat die letzten beiden zusammengeworfen: bei gestopptem Backend
/// stand auf der Aufgabenseite "Diese Aufgabe gibt es nicht (mehr)", obwohl es sie sehr wohl
/// gibt. Nicht das <c>Result&lt;T&gt;</c> aus der Application-Schicht - das Frontend kennt
/// laut Architekturregel nur <c>Shared</c>.
/// </para>
/// </remarks>
public sealed record ApiResult<T>(T? Value, string? ErrorMessage) where T : class
{
    /// <summary>Der Aufruf hat geklappt.</summary>
    public static ApiResult<T> Success(T value) => new(value, null);

    /// <summary>Es gibt den angefragten Gegenstand nicht. Kein Fehler, eine Auskunft.</summary>
    public static ApiResult<T> NotFound() => new(null, null);

    /// <summary>Der Aufruf ist gescheitert. Die Meldung ist fuer Teilnehmer gedacht.</summary>
    public static ApiResult<T> Failure(string errorMessage) => new(null, errorMessage);

    public bool Failed => ErrorMessage is not null;
}
