import java.util.Scanner;

public class Zaehlwerk {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.println("Startwert?");
        int value = scanner.nextInt();

        System.out.println("Vorher: " + value);

        value++;
        System.out.println("Nach ++: " + value);

        value--;
        System.out.println("Nach --: " + value);
    }
}
