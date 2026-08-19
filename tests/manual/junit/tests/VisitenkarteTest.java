import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.sun.source.tree.AssignmentTree;
import com.sun.source.tree.CompilationUnitTree;
import com.sun.source.tree.VariableTree;
import com.sun.source.util.JavacTask;
import com.sun.source.util.TreeScanner;

import java.io.File;
import java.util.LinkedHashMap;
import java.util.Map;

import javax.tools.JavaCompiler;
import javax.tools.StandardJavaFileManager;
import javax.tools.ToolProvider;

import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;

/*
 * AUFGABE 1.6 — Strings verketten.
 *
 * Die dritte Pruefung ist der Kern: greeting muss aus firstName und lastName
 * ZUSAMMENGESETZT werden. Wer den fertigen Satz von Hand hintippt, kommt am
 * Ergebnis vorbei - im Syntaxbaum steht dann kein Variablenname.
 */
class VisitenkarteTest {

    // ------------------------------------------------------------------
    // Liest die abgegebenen Java-Dateien mit dem Parser des JDK ein.
    //
    // Lokale Variablen sind von aussen sonst unsichtbar: sie leben nur im
    // Stack-Frame, Reflection kommt nicht an sie heran, und ihr Name steht
    // erst mit "javac -g" in der .class-Datei.
    //
    // Bewusst kein Regex: ein auskommentiertes "// int x = 1;" ist damit
    // kein Treffer, und "int x; x = 1;" wird genauso erkannt wie
    // "int x = 1;". Geparst, nicht typaufgeloest - bei "var x = 1" stuende
    // im Typ "var", und genau das soll bei Einstiegsaufgaben auffallen.
    // ------------------------------------------------------------------

    // Name -> deklarierter Typ, z. B. "age" -> "int"
    private static final Map<String, String> TYPEN = new LinkedHashMap<>();

    // Name -> zugewiesener Wert als Quelltext, z. B. "age" -> "25"
    private static final Map<String, String> WERTE = new LinkedHashMap<>();

    @BeforeAll
    static void quelltextEinlesen() throws Exception {
        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();
        assertNotNull(compiler, "Auf diesem System steht kein JDK-Compiler bereit.");

        // Alles ausser den Testdateien selbst. Ob die Abgabe richtig heisst,
        // prueft bereits der Aufgaben-Vertrag.
        File[] quellen = new File(".").listFiles(
                (ordner, datei) -> datei.endsWith(".java") && !datei.endsWith("Test.java"));
        assertNotNull(quellen, "Im Arbeitsverzeichnis liegt keine Java-Datei.");

        try (StandardJavaFileManager dateien = compiler.getStandardFileManager(null, null, null)) {
            JavacTask aufgabe = (JavacTask) compiler.getTask(
                    null, dateien, diagnose -> { }, null, null,
                    dateien.getJavaFileObjects(quellen));

            for (CompilationUnitTree baum : aufgabe.parse()) {
                baum.accept(new TreeScanner<Void, Void>() {
                    @Override
                    public Void visitVariable(VariableTree knoten, Void unbenutzt) {
                        TYPEN.put(knoten.getName().toString(), knoten.getType().toString());
                        if (knoten.getInitializer() != null) {
                            WERTE.put(knoten.getName().toString(),
                                      knoten.getInitializer().toString());
                        }
                        return super.visitVariable(knoten, unbenutzt);
                    }

                    @Override
                    public Void visitAssignment(AssignmentTree knoten, Void unbenutzt) {
                        WERTE.put(knoten.getVariable().toString(),
                                  knoten.getExpression().toString());
                        return super.visitAssignment(knoten, unbenutzt);
                    }
                }, null);
            }
        }
    }

    // Vorhanden, richtiger Typ, mit einem Wert versehen.
    private static void pruefeVariable(String name, String erwarteterTyp) {
        assertTrue(TYPEN.containsKey(name),
                "Es gibt keine Variable namens " + name + ".");
        assertEquals(erwarteterTyp, TYPEN.get(name),
                "Die Variable " + name + " hat den falschen Typ.");
        assertTrue(WERTE.containsKey(name),
                "Die Variable " + name + " bekommt keinen Wert zugewiesen.");
    }

    // Der Inhalt eines String-Literals ohne die Anfuehrungszeichen.
    private static String textWert(String name) {
        String roh = WERTE.getOrDefault(name, "");
        return roh.length() >= 2 && roh.startsWith("\"") && roh.endsWith("\"")
                ? roh.substring(1, roh.length() - 1)
                : roh;
    }

    // ------------------------------------------------------------------
    // Konsolenausgabe abfangen. Der PrintStream bekommt UTF-8 ausdruecklich
    // mit - ohne das schreibt er in der Codepage des Systems.
    // ------------------------------------------------------------------

    private final PrintStream originalOut = System.out;
    private ByteArrayOutputStream puffer;

    @BeforeEach
    void ausgabeUmleiten() {
        puffer = new ByteArrayOutputStream();
        System.setOut(new PrintStream(puffer, true, StandardCharsets.UTF_8));
    }

    @AfterEach
    void ausgabeZurueckgeben() {
        System.setOut(originalOut);
    }

    private String ausgabe() throws Exception {
        Visitenkarte.main(new String[0]);
        return puffer.toString(StandardCharsets.UTF_8).trim();
    }

    @Test
    @DisplayName("Vorname und Nachname stehen in den String-Variablen firstName und lastName")
    void namenSindDeklariert() {
        pruefeVariable("firstName", "String");
        pruefeVariable("lastName", "String");
    }

    @Test
    @DisplayName("Die Variable greeting setzt sich aus firstName und lastName zusammen")
    void greetingIstZusammengesetzt() {
        pruefeVariable("greeting", "String");

        String bauplan = WERTE.getOrDefault("greeting", "");
        assertTrue(bauplan.contains("firstName") && bauplan.contains("lastName"),
                "greeting benutzt nicht beide Variablen, sondern steht als fertiger Text da.");
    }

    @Test
    @DisplayName("Das Programm gibt die vollständige Begrüßung aus")
    void ausgabeStimmt() throws Exception {
        assertEquals("Guten Tag, " + textWert("firstName") + " " + textWert("lastName") + "!",
                ausgabe(), "Die Ausgabe hat nicht die geforderte Form.");
    }
}
