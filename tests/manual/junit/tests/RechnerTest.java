import static org.junit.jupiter.api.Assertions.assertEquals;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * VORLAGE 2 — eigene Methode pruefen und zusaetzlich eine Eingabe simulieren.
 *
 * Der @DisplayName ist nicht Zierde: er landet im XML-Report und ist genau der
 * Text, den der Teilnehmer im Ergebnis liest. Ein Methodenname wie
 * "mainGibtSummeAus" hilft ihm nicht, "Das Programm gibt die Summe aus" schon.
 *
 * Achtung bei simulierter Eingabe: liest die Abgabe ihren Scanner in einem
 * statischen Feld, wird der einmal beim Laden der Klasse gebaut — und zwar
 * bevor System.setIn wirkt. Solche Abgaben fallen hier durch, obwohl sie von
 * Hand bedient funktionieren.
 */
class RechnerTest {

    private final PrintStream originalOut = System.out;
    private final InputStream originalIn = System.in;
    private ByteArrayOutputStream ausgabe;

    @BeforeEach
    void stroemeUmleiten() {
        ausgabe = new ByteArrayOutputStream();
        System.setOut(new PrintStream(ausgabe, true, StandardCharsets.UTF_8));
    }

    @AfterEach
    void stroemeZuruecksetzen() {
        System.setOut(originalOut);
        System.setIn(originalIn);
    }

    @Test
    @DisplayName("addiere(2, 3) ergibt 5")
    void addiereZweiPositiveZahlen() {
        assertEquals(5, Main.addiere(2, 3));
    }

    @Test
    @DisplayName("addiere rechnet auch mit negativen Zahlen")
    void addiereMitNegativerZahl() {
        assertEquals(-1, Main.addiere(2, -3));
    }

    @Test
    @DisplayName("Das Programm gibt die Summe der eingelesenen Zahlen aus")
    void mainGibtSummeAus() {
        System.setIn(new ByteArrayInputStream("3\n4\n".getBytes(StandardCharsets.UTF_8)));

        Main.main(new String[0]);

        assertEquals("7", ausgabe.toString(StandardCharsets.UTF_8).trim());
    }
}
