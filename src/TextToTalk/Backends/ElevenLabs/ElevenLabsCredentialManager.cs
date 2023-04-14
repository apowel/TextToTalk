﻿using System.Net;
using AdysTech.CredentialManager;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsCredentialManager
{
    private const string CredentialsTarget = "TextToTalk_AccessKeys_ElevenLabs";

    public static NetworkCredential? LoadCredentials()
    {
        var credentials = CredentialManager.GetCredentials(CredentialsTarget);
        return credentials;
    }

    public static void SaveCredentials(string username, string password)
    {
        var credentials = new NetworkCredential(username, password);
        CredentialManager.SaveCredentials(CredentialsTarget, credentials);
    }

    public static void DeleteCredentials()
    {
        CredentialManager.RemoveCredentials(CredentialsTarget);
    }
}