public class Main {

    // Haeufiger Fehler: die Sternchen stimmen, die Einrueckung fehlt.
    // Faellt genau bei einer Teilpruefung durch und zeigt damit, dass eine
    // Aufgabe nicht nur "geht" oder "geht nicht" kennt.
    public static String zeichnePyramide(int hoehe) {
        StringBuilder pyramide = new StringBuilder();

        for (int zeile = 1; zeile <= hoehe; zeile++) {
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
