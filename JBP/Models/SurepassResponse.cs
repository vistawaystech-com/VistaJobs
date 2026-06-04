using System.Text.Json.Serialization;

namespace JBP.Models
{
    public class SurepassResponse
    {
        public bool Success { get; set; }

        public SurepassData? Data { get; set; }
    }

    public class SurepassData
    {
        public string? Uan { get; set; }

        [JsonPropertyName("employment_history")]
        public List<SurepassEmployment>? Employment_History { get; set; }
    }

    public class SurepassEmployment
    {
        [JsonPropertyName("establishment_name")]
        public string? Establishment_Name { get; set; }

        [JsonPropertyName("date_of_joining")]
        public string? Date_Of_Joining { get; set; }

        [JsonPropertyName("date_of_exit")]
        public string? Date_Of_Exit { get; set; }

        [JsonPropertyName("member_id")]
        public string? Member_Id { get; set; }
    }
}