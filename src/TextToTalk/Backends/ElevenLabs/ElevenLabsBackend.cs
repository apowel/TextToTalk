using System;
using System.Collections.Generic;
using System.Net.Http;
using ElevenLabs.Voices;
using ImGuiNET;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsBackend : VoiceBackend
{
    private readonly ElevenLabsBackendUI ui;
    private readonly ElevenLabsBackendUIModel uiModel;

    public ElevenLabsBackend(PluginConfiguration config, HttpClient http)
    {
        TitleBarColor = ImGui.ColorConvertU32ToFloat4(0xFFF96800);

        var lexiconManager = new DalamudLexiconManager();
        //LexiconUtils.LoadFromConfigElevenLabs(lexiconManager, config);

        this.uiModel = new ElevenLabsBackendUIModel(config, lexiconManager);
        this.ui = new ElevenLabsBackendUI(this.uiModel, config, lexiconManager, http);
    }

    public override void Say(TextSource source, Voice voice, string speaker, string text)
    {
        if (preset is not ElevenLabsVoicePreset ElevenLabsVoicePreset)
        {
            throw new InvalidOperationException("Invalid voice preset provided.");
        }

        if (this.uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = this.uiModel.ElevenLabs.Say(voice,
            ElevenLabsVoicePreset.PlaybackRate, ElevenLabsVoicePreset.Volume, source, text);
    }

    public override void CancelAllSpeech()
    {
        if (this.uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = this.uiModel.ElevenLabs.CancelAllSounds();
    }

    public override void CancelSay(TextSource source)
    {
        if (this.uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return;
        }

        _ = this.uiModel.ElevenLabs.CancelFromSource(source);
    }

    public override void DrawSettings(IConfigUIDelegates helpers)
    {
        this.ui.DrawSettings(helpers);
    }

    public override TextSource GetCurrentlySpokenTextSource()
    {
        if (this.uiModel.ElevenLabs == null)
        {
            DetailedLog.Warn("ElevenLabs client has not yet been initialized");
            return TextSource.None;
        }

        return this.uiModel.ElevenLabs.GetCurrentlySpokenTextSource();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.uiModel.ElevenLabs?.Dispose();
        }
    }
}