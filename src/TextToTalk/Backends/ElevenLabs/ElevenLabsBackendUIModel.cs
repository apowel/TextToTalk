﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextToTalk.Lexicons;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsBackendUIModel
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private readonly PluginConfiguration config;

    private List<string> voices;
    private ElevenLabsLoginInfo loginInfo;

    /// <summary>
    /// Gets the currently-instantiated ElevenLabs client instance.
    /// </summary>
    public ElevenLabsHttpClient? ElevenLabs { get; private set; }

    /// <summary>
    /// Gets the exception thrown by the most recent login, or null if the login was successful.
    /// </summary>
    public Exception? ElevenLabsLoginException { get; private set; }
    
    /// <summary>
    /// Gets the available voices.
    /// </summary>
    public IReadOnlyList<string> Voices => this.voices;

    public ElevenLabsBackendUIModel(PluginConfiguration config, LexiconManager lexiconManager)
    {
        this.config = config;
        this.voices = new List<string>();

        this.loginInfo = new ElevenLabsLoginInfo();
        var credentials = ElevenLabsCredentialManager.LoadCredentials();
        if (credentials != null)
        {
            this.loginInfo.SubscriptionKey = credentials.Password;

            TryElevenLabsLogin();
        }
    }
    
    /// <summary>
    /// Gets the client's current credentials.
    /// </summary>
    /// <returns>The client's current credentials.</returns>
    public ElevenLabsLoginInfo GetLoginInfo()
        => this.loginInfo;

    /// <summary>
    /// Logs in with the provided credentials.
    /// </summary>
    /// <param name="subscriptionKey">The client's subscription key.</param>
    public void LoginWith(string subscriptionKey)
    {
        var password = Whitespace.Replace(subscriptionKey, "");
        this.loginInfo = new ElevenLabsLoginInfo {SubscriptionKey = password };

        if (TryElevenLabsLogin())
        {
            // Only save the user's new credentials if the login succeeded
            ElevenLabsCredentialManager.SaveCredentials("", password);
        }
    }
    
    /// <summary>
    /// Gets the current voice preset.
    /// </summary>
    /// <returns>The current voice preset, or null if no voice preset is selected.</returns>
    public ElevenLabsVoicePreset? GetCurrentVoicePreset()
        => this.config.GetCurrentVoicePreset<ElevenLabsVoicePreset>();

    /// <summary>
    /// Sets the current voice preset.
    /// </summary>
    /// <param name="id">The preset ID.</param>
    public void SetCurrentVoicePreset(int id)
    {
        this.config.SetCurrentVoicePreset(id);
        this.config.Save();
    }

    private bool TryElevenLabsLogin()
    {
        ElevenLabsLoginException = null;
        ElevenLabs?.Dispose();
        try
        {
            DetailedLog.Info($"Logging into ElevenLabs");
            ElevenLabs = new ElevenLabsHttpClient(this.loginInfo.SubscriptionKey);
            // This should throw an exception if the login failed
            this.voices = ElevenLabs.GetVoices();
            return true;
        }
        catch (Exception e)
        {
            ElevenLabsLoginException = e;
            DetailedLog.Error(e, "Failed to initialize ElevenLabs client");
            return false;
        }
    }
}