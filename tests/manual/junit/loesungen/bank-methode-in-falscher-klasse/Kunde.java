public class Kunde {

    private final String name;

    public Kunde(String name) {
        this.name = name;
    }

    public String getName() {
        return name;
    }

    // Der Fehler dieser Abgabe: getStand gehoert laut Aufgaben-Vertrag in Konto,
    // steht hier aber in Kunde.
    //
    // Bis Phase 5.2 waere das durchgegangen - der ContractChecker suchte im
    // gesamten Quelltext und fand den Namen ja. Jetzt sucht er im Rumpf der
    // geforderten Klasse, und die Teilpruefung faellt durch.
    //
    // Zusaetzlich uebersetzt die JUnit-Datei nicht mehr gegen die Abgabe: sie
    // ruft konto.getStand() auf, das es in Konto nicht gibt. Die Compilermeldung
    // wird in einen Satz uebersetzt, der die erwartete Signatur nennt.
    public double getStand() {
        return 0.0;
    }
}
