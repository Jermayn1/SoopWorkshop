import java.util.Scanner;

public class Namensabfrage {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.println("Wie heisst du?");
        String name = scanner.nextLine();

        System.out.println("Hallo, " + name + "!");
    }
}
