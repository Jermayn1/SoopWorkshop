namespace SoopWorkshop.Backend.Domain.Entities
{
    // Eine Methode, die die Aufgabe verlangt. Der Admin schreibt die Signatur so
    // auf, wie sie in der Aufgabenstellung steht; der Name wird daraus abgeleitet.
    //
    // Haengt an der Klasse, nicht an der Aufgabe: nur so laesst sich pruefen, ob
    // die Methode auch dort steht, wo sie hingehoert.
    public class TaskExpectedMethod
    {
        public Guid Id { get; set; }
        public Guid TaskExpectedTypeId { get; set; }

        // Vollstaendig, fuer Anzeige und Fehlermeldung:
        // "public static int addiere(int ersteZahl, int zweiteZahl)"
        public string Signature { get; set; } = string.Empty;

        // Der reine Methodenname, gegen den geprueft wird: "addiere".
        // Bewusst nur der Name: die genauen Parametertypen prueft die
        // JUnit-Kompilierung ohnehin exakt, und ein Regex ueber Java-Quelltext
        // wuerde daran nur unzuverlaessig scheitern.
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        public TaskExpectedType Type { get; set; } = null!;
    }
}
