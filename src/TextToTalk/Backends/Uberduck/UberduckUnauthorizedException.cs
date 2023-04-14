using System;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsUnauthorizedException : Exception
{
    public ElevenLabsUnauthorizedException(string message) : base(message)
    {
    }
}