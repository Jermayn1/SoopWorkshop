namespace SoopWorkshop.Backend.Domain.Entities
{
    public class TaskCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; }

        // Name des Symbols in der Seitenleiste, z. B. "Layers".
        //
        // Frueher haben die Seitenleisten dafuer den Kategorienamen ausgewertet
        // ("oop" -> Layers, sonst BookOpen). Damit wechselte das Symbol beim
        // Umbenennen, und eine neue Kategorie bekam nie ein eigenes. Welche
        // Namen es gibt, weiss das Frontend (src/admin/icons.ts) - hier steht
        // nur der Name; ein unbekannter faellt dort auf das Standardsymbol
        // zurueck.
        //
        // Leer heisst "kein eigenes Symbol".
        public string IconName { get; set; } = string.Empty;

        public ICollection<TaskItem> Tasks { get; set; } = [];
    }
}