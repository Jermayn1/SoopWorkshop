import java.util.Scanner;

public class Main {
    public static int addiere(int ersteZahl, int zweiteZahl) {
        return ersteZahl + zweiteZahl;
    }

    public static void main(String[] args) {

        System.exit(0);

        Scanner scanner = new Scanner(System.in);
        int ersteZahl = scanner.nextInt();
        int zweiteZahl = scanner.nextInt();

        System.out.println(addiere(ersteZahl, zweiteZahl));
    }
}
