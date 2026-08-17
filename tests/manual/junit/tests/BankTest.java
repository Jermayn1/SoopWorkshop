import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * VORLAGE 4 — mehrere Klassen pruefen, die voneinander abhaengen.
 *
 * Der Fall fuer die OOP-Aufgaben am Ende des Workshops. Neu gegenueber den
 * Vorlagen 1 bis 3 ist nur, dass die Testklasse ZWEI Klassen der Abgabe
 * benutzt: sie baut einen Kunden und uebergibt ihn an ein Konto.
 *
 * Dass das funktioniert, ist keine Zusatzeinstellung: alle abgegebenen Dateien
 * landen in einem Arbeitsverzeichnis und gehen zusammen durch javac, und die
 * Testdatei wird danach mit ".", also gegen genau diese Klassen, uebersetzt.
 *
 * Worauf beim Schreiben solcher Tests zu achten ist:
 *
 *  - Jeder Test baut sich seine Objekte selbst. Statische Felder der Abgabe
 *    ueberleben zwischen Testmethoden (eine JVM pro Lauf) - geteilter Zustand
 *    macht die Reihenfolge der Tests plotzlich bedeutsam.
 *  - Kein System.setIn: Eingaben gehoeren in Konsolen-Testfaelle. Eine im
 *    Testcode versteckte Eingabe kann die Anzeige nicht kennen.
 *  - Der @DisplayName ist keine Zierde. Genau dieser Text erscheint beim
 *    Teilnehmer als Beschreibung der Teilpruefung.
 */
class BankTest {

    @Test
    @DisplayName("Ein Konto kennt den Namen seines Inhabers")
    void kontoKenntInhaber() {
        Kunde kunde = new Kunde("Anna");
        Konto konto = new Konto(kunde);

        assertEquals("Anna", konto.getInhaber().getName());
    }

    @Test
    @DisplayName("Ein neues Konto startet bei null")
    void neuesKontoIstLeer() {
        Konto konto = new Konto(new Kunde("Ben"));

        assertEquals(0.0, konto.getStand(), 0.001);
    }

    @Test
    @DisplayName("Die Methode einzahlen erhöht den Kontostand")
    void einzahlenErhoehtStand() {
        Konto konto = new Konto(new Kunde("Cem"));

        konto.einzahlen(50.0);
        konto.einzahlen(25.5);

        assertEquals(75.5, konto.getStand(), 0.001);
    }

    @Test
    @DisplayName("Die Methode abheben gibt das Geld heraus, wenn das Konto gedeckt ist")
    void abhebenBeiDeckung() {
        Konto konto = new Konto(new Kunde("Dana"));
        konto.einzahlen(100.0);

        assertTrue(konto.abheben(40.0), "abheben soll true liefern, wenn genug Geld da ist");
        assertEquals(60.0, konto.getStand(), 0.001);
    }

    @Test
    @DisplayName("Die Methode abheben lehnt ab, wenn das Konto nicht gedeckt ist")
    void abhebenOhneDeckung() {
        Konto konto = new Konto(new Kunde("Elif"));
        konto.einzahlen(20.0);

        assertFalse(konto.abheben(50.0), "abheben soll false liefern, wenn zu wenig Geld da ist");
        assertEquals(20.0, konto.getStand(), 0.001, "Ein abgelehntes Abheben darf den Stand nicht ändern");
    }
}
