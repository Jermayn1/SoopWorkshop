import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * VORLAGE 2 — eine eigene Methode pruefen.
 *
 * Der @DisplayName ist nicht Zierde: er landet im XML-Report und ist genau der
 * Text, den der Teilnehmer im Ergebnis liest. Ein Methodenname wie
 * "addiereZweiPositiveZahlen" hilft ihm nicht, "Die Methode addiere addiert
 * zwei positive Zahlen" schon.
 *
 * Bewusst OHNE Eingabesimulation ueber System.setIn: was das Programm mit einer
 * Eingabe macht, gehoert in einen Konsolen-Testfall. Dort steht die Eingabe in
 * der Aufgabe und erscheint im Ergebnis unter "Eingabe" - eine hier im Testcode
 * versteckte Eingabe kann die Anzeige nicht kennen, und der Teilnehmer sieht
 * ein "Erwartet" ohne zu wissen, womit gerechnet wurde.
 *
 * Faustregel: JUnit prueft Methoden, Konsolen-Testfaelle pruefen das Programm.
 */
class RechnerTest {

    @Test
    @DisplayName("Die Methode addiere addiert zwei positive Zahlen")
    void addiereZweiPositiveZahlen() {
        assertEquals(5, Main.addiere(2, 3));
    }

    @Test
    @DisplayName("Die Methode addiere rechnet auch mit negativen Zahlen")
    void addiereMitNegativerZahl() {
        assertEquals(-1, Main.addiere(2, -3));
    }
}
