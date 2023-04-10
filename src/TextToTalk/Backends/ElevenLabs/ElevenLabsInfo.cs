namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsLoginInfo
{
    public string SubscriptionKey { get; set; } = "";
    public void Deconstruct(out string subscriptionKey)
    {
        subscriptionKey = SubscriptionKey;
    }
}