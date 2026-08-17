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

    public boolean abheben(double betrag) {
        if (betrag > stand) {
            return false;
        }

        stand = stand - betrag;
        return true;
    }
}
