import static org.junit.jupiter.api.Assertions.assertEquals;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * VORLAGE 1 — Konsolenausgabe pruefen, ohne dass der Teilnehmer eine eigene
 * Methode schreiben muss.
 *
 * Fuer die ersten Aufgaben gedacht: der Teilnehmer schreibt nur eine main, und
 * geprueft wird trotzdem ueber JUnit statt ueber Konsolen-Testfaelle.
 *
 * Das funktioniert hier, weil die Aufgabe keine Eingabe hat. Sobald eine ins
 * Spiel kommt, gehoert die Pruefung in einen Konsolen-Testfall - nur dort
 * erscheint die Eingabe im Ergebnis (siehe RechnerTest).
 *
 * Drei Dinge, die hier absichtlich so stehen:
 *
 *  1. Der PrintStream bekommt StandardCharsets.UTF_8 ausdruecklich mit. Ohne
 *     das schreibt er in der Codepage des Systems (unter Windows Cp1252) und
 *     Umlaute in der Ausgabe kommen zerlegt an.
 *  2. System.setOut wird in @AfterEach zurueckgesetzt. Sonst beeinflussen sich
 *     Testmethoden gegenseitig.
 *  3. Statische Felder der Abgabe (z. B. ein 'static Scanner') ueberleben
 *     zwischen den Testmethoden, weil alle in derselben JVM laufen. Wer darauf
 *     angewiesen ist, prueft das lieber in nur einer Methode.
 */
class HalloSoopTest {

    private final PrintStream originalOut = System.out;
    private ByteArrayOutputStream ausgabe;

    @BeforeEach
    void ausgabeUmleiten() {
        ausgabe = new ByteArrayOutputStream();
        System.setOut(new PrintStream(ausgabe, true, StandardCharsets.UTF_8));
    }

    @AfterEach
    void ausgabeZuruecksetzen() {
        System.setOut(originalOut);
    }

    @Test
    @DisplayName("Das Programm gibt Hallo Soop aus")
    void mainGibtHalloSoopAus() {
        Main.main(new String[0]);

        assertEquals("Hallo Soop", ausgabe.toString(StandardCharsets.UTF_8).trim());
    }
}
