import java.util.Scanner;

public class Schreibweise {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.println("Gib einen Text ein:");
        String text = scanner.nextLine();

        System.out.println("GROSS: " + text.toUpperCase());
        System.out.println("klein: " + text.toLowerCase());
    }
}
