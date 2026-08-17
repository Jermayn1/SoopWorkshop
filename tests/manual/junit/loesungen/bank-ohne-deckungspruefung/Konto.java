public class Konto {

    private final Kunde inhaber;
    private double stand;

    public Konto(Kunde inhaber) {
        this.inhaber = inhaber;
    }

    public Kunde getInhaber() {
        return inhaber;
    }

    public double getStand() {
        return stand;
    }

    public void einzahlen(double betrag) {
        stand = stand + betrag;
    }

    // Der Fehler dieser Abgabe: hier fehlt die Pruefung, ob das Konto ueberhaupt
    // gedeckt ist. Abgehoben wird immer, und zurueckgemeldet wird immer true.
    //
    // Der Vertrag stimmt vollstaendig, die Abgabe kompiliert - nur ein einziger
    // JUnit-Test faellt durch. Genau der Fall, den man ohne Unit-Tests uebersieht.
    public boolean abheben(double betrag) {
        stand = stand - betrag;
        return true;
    }
}
