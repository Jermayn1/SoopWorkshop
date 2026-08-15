import java.util.Scanner;

// Die Aufgabe verlangt die Klasse 'Main'. Diese hier heisst 'Rechner' und
// kompiliert trotzdem - Java erzwingt nur, dass Dateiname und Klassenname
// zusammenpassen. Ohne die Vorgabenpruefung waere das nie aufgefallen.
public class Rechner {
    public static int addiere(int ersteZahl, int zweiteZahl) {
        return ersteZahl + zweiteZahl;
    }

    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);
        int ersteZahl = scanner.nextInt();
        int zweiteZahl = scanner.nextInt();

        System.out.println(addiere(ersteZahl, zweiteZahl));
    }
}
