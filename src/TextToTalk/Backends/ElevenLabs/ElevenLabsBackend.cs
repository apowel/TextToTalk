using System;
using ImGuiNET;
using System.Net.Http;
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

    public ElevenLabsBackend(PluginConfiguration config, HttpClient http)
    {
        TitleBarColor = ImGui.ColorConvertU32ToFloat4(0xFFDE7312);

        this.soundQueue = new StreamSoundQueue();
        this.ElevenLabs = new ElevenLabsHttpClient(this.soundQueue, http);
        LoadCredentials();
        var voices = this.ElevenLabs.GetVoices().GetAwaiter().GetResult();
        this.ui = new ElevenLabsBackendUI(config, this.ElevenLabs, () => voices);
    }

    public override void Say(TextSource source, VoicePreset preset, string speaker, string text)
    {
        if (preset is not ElevenLabsVoicePreset elevenLabsVoicePreset)
        {
            throw new InvalidOperationException("Invalid voice preset provided.");
        }

        if (this.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrEmpty(elevenLabsVoicePreset.VoiceId))
                {
                    elevenLabsVoicePreset.VoiceId = "Lzt91aqyBlu8xGgcxUBR";
                }
                await this.ElevenLabs.Say(elevenLabsVoicePreset.VoiceId, text, source, elevenLabsVoicePreset.Volume,
                    elevenLabsVoicePreset.Stability, elevenLabsVoicePreset.SimilarityBoost);
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