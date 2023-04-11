namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsLoginInfo
{
    public string SubscriptionKey { get; set; } = "";
    public string Sub { get; set; } = "";

    public void Deconstruct(out string subscriptionKey, out string sub)
    {
        subscriptionKey = "0d127136f61cff24e20b081f06e74d1a";
        sub = Sub;
    }
}