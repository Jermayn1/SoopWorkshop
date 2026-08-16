import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * VORLAGE 3 — eine Methode mit Rueckgabewert pruefen, Schwerpunkt Schleifen.
 *
 * Wie Vorlage 2 ohne Eingabesimulation: die Methode bekommt ihren Wert als
 * Parameter und gibt das Ergebnis zurueck. Damit steht in "Erwartet" und
 * "Erhalten" genau das, was der Teilnehmer vergleichen muss.
 *
 * Die Methode gibt bewusst NICHT selbst aus. Wer rechnet und ausgibt in einem
 * Zug, kann das Ergebnis nicht pruefen, ohne die Konsole abzufangen - und die
 * Trennung von Berechnung und Ausgabe ist genau das, was hier geuebt wird.
 *
 * Zeilen sind mit \n verbunden, ohne Zeilenumbruch am Ende. Das steht so in
 * der Aufgabenstellung, sonst raet der Teilnehmer.
 */
class PyramideTest {

    @Test
    @DisplayName("Die Methode zeichnePyramide liefert bei Höhe 1 ein einzelnes Sternchen")
    void hoeheEins() {
        assertEquals("*", Main.zeichnePyramide(1));
    }

    @Test
    @DisplayName("Die Methode zeichnePyramide baut bei Höhe 3 die Zeilen 1, 3 und 5 Sternchen")
    void hoeheDrei() {
        assertEquals("  *\n ***\n*****", Main.zeichnePyramide(3));
    }

    @Test
    @DisplayName("Die Methode zeichnePyramide rückt jede Zeile so ein, dass die Spitze mittig steht")
    void einrueckungStimmt() {
        String[] zeilen = Main.zeichnePyramide(4).split("\n");
        assertEquals(3, zeilen[0].indexOf('*'), "Die erste Zeile beginnt nach drei Leerzeichen");
        assertEquals(0, zeilen[3].indexOf('*'), "Die letzte Zeile beginnt ganz links");
    }

    @Test
    @DisplayName("Die Methode zeichnePyramide hängt keine Leerzeichen an das Zeilenende")
    void keineLeerzeichenAmEnde() {
        for (String zeile : Main.zeichnePyramide(4).split("\n")) {
            assertFalse(zeile.endsWith(" "), "Diese Zeile endet mit einem Leerzeichen: '" + zeile + "'");
        }
    }
}
