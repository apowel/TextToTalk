using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextToTalk.Backends.ElevenLabs;

/// <summary>
/// The logic for the ElevenLabs backend. ElevenLabs changed its offerings to not include a
/// free option, so this likely won't see many updates going forward.
/// </summary>
public class ElevenLabsBackend : VoiceBackend
{
    private readonly StreamSoundQueue soundQueue;
    private readonly ElevenLabsBackendUI ui;
    private readonly ElevenLabsHttpClient? ElevenLabs;
    private string apiKey;
    private string apiSecret;
    private IDictionary<string,IList<ElevenLabsVoice>> myVoices;

    public ElevenLabsBackend(PluginConfiguration config, HttpClient http)
    {
        TitleBarColor = ImGui.ColorConvertU32ToFloat4(0xFFDE7312);

        this.soundQueue = new StreamSoundQueue();
        this.ElevenLabs = new ElevenLabsHttpClient(this.soundQueue, http);
        LoadCredentials();
        IDictionary<string, IList<ElevenLabsVoice>> voices =  new Dictionary<string, IList<ElevenLabsVoice>>();
        if (myVoices == null || myVoices?.Count == 0)
        {
            voices = this.ElevenLabs.GetVoices().GetAwaiter().GetResult();
            this.myVoices = voices;
        }
        this.ui = new ElevenLabsBackendUI(config, this.ElevenLabs, () => voices);
    }

    public override void Say(TextSource source, VoicePreset preset, string speaker, string text)
    {
        //ElevenLabsVoice voice = GetVoiceByName(speaker);
        
        
        if (preset is not ElevenLabsVoicePreset elevenLabsVoicePreset)
        {
            throw new InvalidOperationException("Invalid voice preset provided.");
        }

        //elevenLabsVoicePreset.VoiceId = voice.VoiceId;
        if (this.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                
                await this.ElevenLabs.Say(elevenLabsVoicePreset, text, source);
            }
            catch (ElevenLabsFailedException e)
            {
                DetailedLog.Error(e, $"Failed to make ElevenLabs TTS request ({e.StatusCode}).");
            }
            catch (ElevenLabsMissingCredentialsException e)
            {
                DetailedLog.Warn(e.Message);
            }
            catch (ElevenLabsUnauthorizedException e)
            {
                DetailedLog.Error(e, "ElevenLabs API keys are incorrect or invalid.");
            }
        });
    }
    //miqo'te compatible name getter
    public ElevenLabsVoice GetVoiceByName(string voiceName)
    {
        var regex = new Regex("[^a-zA-Z0-9]+");

        var sanitizedVoiceName = regex.Replace(voiceName, "").ToLowerInvariant();
    
        var voice = myVoices.SelectMany(pair => pair.Value)
            .FirstOrDefault(v => regex.Replace(v.Name, "").ToLowerInvariant() == sanitizedVoiceName);
        if (voice == null)
        {
            voice = myVoices.SelectMany(pair => pair.Value).Where(e => e.Category == "generated").FirstOrDefault();
        }
        return voice;
    }
    public void LoadCredentials()
    {
        var credentials = ElevenLabsCredentialManager.LoadCredentials();
        if (credentials != null)
        {
            this.apiKey = credentials.UserName;
            this.apiSecret = credentials.Password;
        }


        this.ElevenLabs.ApiKey = this.apiKey;
        this.ElevenLabs.ApiSecret = this.apiSecret;
        this.ElevenLabs._client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
    }

    public override void CancelAllSpeech()
    {
        this.soundQueue.CancelAllSounds();
    }

    public override void CancelSay(TextSource source)
    {
        this.soundQueue.CancelFromSource(source);
    }

    public override void DrawSettings(IConfigUIDelegates helpers)
    {
        this.ui.DrawSettings(helpers);
    }

    public override TextSource GetCurrentlySpokenTextSource()
    {
        return this.soundQueue.GetCurrentlySpokenTextSource();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.soundQueue.Dispose();
        }
    }
}