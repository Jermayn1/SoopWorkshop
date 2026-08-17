namespace SoopWorkshop.Backend.Domain.Entities
{
    // Eine Klasse, die die Aufgabe verlangt - samt der Methoden, die IN DIESER
    // Klasse stehen muessen.
    //
    // Bis Phase 5.2 gab es dafuer ein einzelnes Feld ExpectedClassName auf der
    // Aufgabe und daneben eine flache Methodenliste. Fuer die OOP-Aufgaben am
    // Ende des Workshops reicht das nicht: dort haengen mehrere Klassen
    // voneinander ab, und "einzahlen" gehoert zu 'Konto' und nicht irgendwohin.
    // Mit der flachen Liste zaehlte die Methode auch dann als vorhanden, wenn
    // sie in einer ganz anderen Klasse stand.
    public class TaskExpectedType
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }

        // Der geforderte Name: "Konto". Ob es Klasse, Interface, Enum oder Record
        // sein soll, steht in der Aufgabenbeschreibung - geprueft wird der Name.
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        public TaskItem Task { get; set; } = null!;

        public ICollection<TaskExpectedMethod> Methods { get; set; } = [];
    }
}
