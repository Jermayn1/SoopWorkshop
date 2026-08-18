namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Eine Seite der Abgaben-Uebersicht.
    //
    // Total gehoert dazu und nicht bloss die Zeilen: ohne die Gesamtzahl kann
    // das Panel nicht sagen, ob es noch eine Seite gibt. Es koennte es aus
    // "weniger Zeilen als angefordert" erraten - das stimmt aber genau dann
    // nicht, wenn die letzte Seite zufaellig voll ist.
    public class SubmissionPageDto
    {
        public List<SubmissionListItemDto> Items { get; set; } = [];

        public int Total { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}
