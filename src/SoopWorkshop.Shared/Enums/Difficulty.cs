using System.Text.Json.Serialization;

namespace SoopWorkshop.Shared.Enums
{
    // Als Zeichenkette ueber die Leitung, siehe Hinweis in EvaluationCategory.
    [JsonConverter(typeof(JsonStringEnumConverter<Difficulty>))]
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
    }
}
