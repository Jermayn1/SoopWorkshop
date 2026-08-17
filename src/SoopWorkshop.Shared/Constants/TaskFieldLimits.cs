namespace SoopWorkshop.Shared.Constants
{
    // Die Laengengrenzen der Aufgabenverwaltung an einer Stelle.
    //
    // Sie stammen aus den EF-Konfigurationen (TaskCategoryConfiguration,
    // TaskItemConfiguration, TaskTestConfiguration, TaskUnitTestFileConfiguration,
    // TaskExpectedTypeConfiguration) und stehen bisher zusaetzlich als
    // DataAnnotations an den Request-DTOs. Der Import prueft gegen dieselben
    // Werte - eine Datei soll nicht erst in der Datenbank scheitern.
    public static class TaskFieldLimits
    {
        public const int CategoryName = 100;
        public const int CategoryIconName = 50;
        public const int TaskTitle = 200;
        public const int ExpectedTypeName = 200;
        public const int ExpectedMethodSignature = 500;
        public const int TestDescription = 500;
        public const int UnitTestFileName = 255;
    }
}
