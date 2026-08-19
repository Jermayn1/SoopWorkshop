import java.util.Scanner;

public class Restrechner {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.println("Erste Zahl?");
        int first = scanner.nextInt();

        System.out.println("Zweite Zahl?");
        int second = scanner.nextInt();

        int rest = first % second;

        System.out.println(first + " geteilt durch " + second + " ergibt Rest " + rest + ".");
    }
}
