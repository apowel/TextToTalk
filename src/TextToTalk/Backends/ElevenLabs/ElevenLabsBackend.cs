using System;
using System.Net.Http;
using ImGuiNET;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsBackend : VoiceBackend
{
    private readonly ElevenLabsBackendUI _ui;
    private readonly ElevenLabsBackendUIModel _uiModel;

    public ElevenLabsBackend(PluginConfiguration config, HttpClient http)
    {
        TitleBarColor = ImGui.ColorConvertU32ToFloat4(0xFFF96800);

        var lexiconManager = new DalamudLexiconManager();
        //LexiconUtils.LoadFromConfigElevenLabs(lexiconManager, config);

        this._uiModel = new ElevenLabsBackendUIModel(config, lexiconManager);
        this._ui = new ElevenLabsBackendUI(this._uiModel, config, lexiconManager, http);
    }

    public override void Say(TextSource source, VoicePreset preset, string speaker, string text)
    {
        if (preset is not ElevenLabsVoicePreset elevenLabsVoicePreset)
        {
            throw new InvalidOperationException("Invalid voice preset provided.");
        }

        if (this._uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = this._uiModel.ElevenLabs.Say(elevenLabsVoicePreset.VoiceName,
            elevenLabsVoicePreset.PlaybackRate, elevenLabsVoicePreset.Volume, source, text);
    }

    public override void CancelAllSpeech()
    {
        if (this._uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = this._uiModel.ElevenLabs.CancelAllSounds();
    }

    public override void CancelSay(TextSource source)
    {
        if (this._uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = this._uiModel.ElevenLabs.CancelFromSource(source);
    }

    public override void DrawSettings(IConfigUIDelegates helpers)
    {
        this._ui.DrawSettings(helpers);
    }

    public override TextSource GetCurrentlySpokenTextSource()
    {
        if (this._uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return TextSource.None;
        }

        return this._uiModel.ElevenLabs.GetCurrentlySpokenTextSource();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._uiModel.ElevenLabs?.Dispose();
        }
    }
}