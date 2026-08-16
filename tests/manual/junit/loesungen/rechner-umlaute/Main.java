import java.util.Scanner;

public class Main {
    public static int addiere(int ersteZahl, int zweiteZahl) {
        return ersteZahl + zweiteZahl;
    }

    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);
        int ersteZahl = scanner.nextInt();
        int zweiteZahl = scanner.nextInt();

        System.out.println("Die Größe der Summe beträgt: " + addiere(ersteZahl, zweiteZahl));
    }
}
