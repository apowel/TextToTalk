using Newtonsoft.Json;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsVoicePreset : VoicePreset
{
    [JsonProperty("ElevenLabsVolume")] public float Volume { get; set; }

    public int PlaybackRate { get; set; }

    [JsonProperty("ElevenLabsVoiceName")] public string? VoiceName { get; set; }

    public override bool TrySetDefaultValues()
    {
        Volume = 1.0f;
        PlaybackRate = 100;
        VoiceName = "en-US-JennyNeural";
        EnabledBackend = TTSBackend.ElevenLabs;
        return true;
    }
}