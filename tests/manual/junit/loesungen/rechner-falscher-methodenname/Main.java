import java.util.Scanner;

public class Main {
    // Heisst 'addieren' statt 'addiere' - die Testdatei kompiliert damit nicht.
    public static int addieren(int ersteZahl, int zweiteZahl) {
        return ersteZahl + zweiteZahl;
    }

    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);
        int ersteZahl = scanner.nextInt();
        int zweiteZahl = scanner.nextInt();

        System.out.println(addieren(ersteZahl, zweiteZahl));
    }
}
