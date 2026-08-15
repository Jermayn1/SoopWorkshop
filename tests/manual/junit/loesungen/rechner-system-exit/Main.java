import java.util.Scanner;

public class Main {
    public static int addiere(int ersteZahl, int zweiteZahl) {
        return ersteZahl + zweiteZahl;
    }

    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);
        int ersteZahl = scanner.nextInt();
        int zweiteZahl = scanner.nextInt();

        System.out.println(addiere(ersteZahl, zweiteZahl));

        // Beendet die JVM - und damit auch den Testlauf, in dem main aufgerufen wird.
        System.exit(0);
    }
}
