using Newtonsoft.Json;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsVoicePreset : VoicePreset
{
    [JsonProperty("ElevenLabsVolume")] public float Volume { get; set; }
    [JsonProperty("stability")] public int Stability { get; set; }
    [JsonProperty("similarity_boost")] public int SimilarityBoost { get; set; }
    [JsonProperty("name")] public string? VoiceName { get; set; }
    [JsonProperty("voice_id")] public string? VoiceId { get; set; }
    public int PlaybackRate { get; set; }

    public override bool TrySetDefaultValues()
    {
        Volume = 1.0f;
        Stability = 0;
        SimilarityBoost = 0;
        VoiceId = "Lzt91aqyBlu8xGgcxUBR";
        VoiceName = "Adam";
        PlaybackRate = 100;
        EnabledBackend = TTSBackend.ElevenLabs;
        return true;
    }
}