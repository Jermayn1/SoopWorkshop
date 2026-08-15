public class VielAusgabe {
    public static void main(String[] args) {
        // Gegenprobe zum behobenen Deadlock: beide Ausgabestroeme laufen weit
        // ueber die Puffergroesse hinaus. Frueher blieb der Prozess hier haengen,
        // weil nur einer der beiden gelesen wurde.
        for (int i = 0; i < 50000; i++) {
            System.out.println("Ausgabezeile " + i);
            System.err.println("Fehlerzeile " + i);
        }
        System.out.println("Hallo SOOP Workshop!");
    }
}
