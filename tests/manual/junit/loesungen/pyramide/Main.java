public class Main {

    // Musterloesung: baut die Pyramide auf und gibt sie zurueck.
    // Berechnen und Ausgeben sind getrennt - deshalb laesst sie sich pruefen.
    public static String zeichnePyramide(int hoehe) {
        StringBuilder pyramide = new StringBuilder();

        for (int zeile = 1; zeile <= hoehe; zeile++) {
            for (int leerzeichen = 0; leerzeichen < hoehe - zeile; leerzeichen++) {
                pyramide.append(' ');
            }
            for (int stern = 0; stern < 2 * zeile - 1; stern++) {
                pyramide.append('*');
            }
            if (zeile < hoehe) {
                pyramide.append('\n');
            }
        }

        return pyramide.toString();
    }

    public static void main(String[] args) {
        System.out.println(zeichnePyramide(5));
    }
}
