using System.Collections.Generic;

namespace TextToTalk.Backends.ElevenLabs;
using Newtonsoft.Json;


public class Labels
    {
        [JsonProperty("additionalProp1")]
        public string? AdditionalProp1 { get; set; }
        [JsonProperty("additionalProp2")]
        public string? AdditionalProp2 { get; set; }
        [JsonProperty("additionalProp3")]
        public string? AdditionalProp3 { get; set; }
    }

    public class ElevenLabsVoices
    {
        [JsonProperty("voices")]
        public List<ElevenLabsVoice>? Voices { get; set; }
    }

    public class Settings
    {
        [JsonProperty("stability")]
        public int? Stability { get; set; }
        [JsonProperty("similarity_boost")]
        public int? Similarity { get; set; }
    }


    public class ElevenLabsVoice
    {
        [JsonProperty("voice_id")]
        public string? VoiceId { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("category")]
        public string? Category { get; set; }
        [JsonProperty("labels")]
        public Labels? Labels { get; set; }
        [JsonProperty("description")]
        public string? Description { get; set; }
        [JsonProperty("settings")]
        public Settings? Settings { get; set; }
    }
