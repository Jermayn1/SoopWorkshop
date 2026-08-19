import java.util.Scanner;

public class Taschenrechner {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.println("Erste Zahl?");
        int first = scanner.nextInt();

        System.out.println("Zweite Zahl?");
        int second = scanner.nextInt();

        System.out.println("Summe: " + (first + second));
        System.out.println("Differenz: " + (first - second));
        System.out.println("Produkt: " + (first * second));
        System.out.println("Quotient: " + (first / second));
    }
}
