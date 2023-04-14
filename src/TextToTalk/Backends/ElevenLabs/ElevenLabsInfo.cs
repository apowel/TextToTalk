namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsLoginInfo
{
    public string ApiKey { get; set; } = "";
    public string Sub { get; set; } = "";

    public void Deconstruct(out string apiKey, out string sub)
    {
        apiKey = ApiKey;
        sub = Sub;
    }
}