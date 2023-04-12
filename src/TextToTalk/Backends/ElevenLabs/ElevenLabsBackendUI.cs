﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TextToTalk.UI;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsBackendUI
{
    private readonly PluginConfiguration config;
    private readonly ElevenLabsHttpClient ElevenLabs;
    private readonly Func<IDictionary<string, IList<ElevenLabsVoice>>> getVoices;
    IDictionary<string, IList<ElevenLabsVoice>> loadedVoices;
    private string apiKey = string.Empty;
    private string apiSecret = string.Empty;

    public ElevenLabsBackendUI(PluginConfiguration config, ElevenLabsHttpClient ElevenLabs,
        Func<IDictionary<string, IList<ElevenLabsVoice>>> getVoices)
    {
        this.config = config;
        this.ElevenLabs = ElevenLabs;
        var credentials = ElevenLabsCredentialManager.LoadCredentials();
        if (credentials != null)
        {
            this.apiKey = credentials.UserName;
            this.apiSecret = credentials.Password;
        }

        this.ElevenLabs.ApiKey = this.apiKey;
        this.ElevenLabs.ApiSecret = this.apiSecret;
        this.getVoices = getVoices;
        loadedVoices = this.getVoices.Invoke();
    }

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public void DrawSettings(IConfigUIDelegates helpers)
    {
        ImGui.TextColored(BackendUI.HintColor, "TTS may be delayed due to rate-limiting.");
        ImGui.Spacing();

        ImGui.InputTextWithHint($"##{MemoizedId.Create()}", "API key", ref this.apiKey, 100,
            ImGuiInputTextFlags.Password);

        if (ImGui.Button($"Save ApiKey##{MemoizedId.Create()}"))
        {
            var username = Whitespace.Replace(this.apiKey, "");
            var password = "poop";
            ElevenLabsCredentialManager.SaveCredentials(username, password);
            this.ElevenLabs.ApiKey = username;
            this.ElevenLabs.ApiSecret = password;
        }

        ImGui.SameLine();
        if (ImGui.Button($"Register##{MemoizedId.Create()}"))
        {
            WebBrowser.Open("https://ElevenLabs.io/");
        }

        ImGui.TextColored(BackendUI.HintColor, "Credentials secured with Windows Credential Manager");

        ImGui.Spacing();

        var currentVoicePreset = this.config.GetCurrentVoicePreset<ElevenLabsVoicePreset>();
        var presets = this.config.GetVoicePresetsForBackend(TTSBackend.ElevenLabs).ToList();
        presets.Sort((a, b) => a.Id - b.Id);

        if (presets.Any())
        {
            var presetIndex = currentVoicePreset is not null ? presets.IndexOf(currentVoicePreset) : -1;
            if (ImGui.Combo($"Preset##{MemoizedId.Create()}", ref presetIndex, presets.Select(p => p.Name).ToArray(),
                    presets.Count))
            {
                this.config.SetCurrentVoicePreset(presets[presetIndex].Id);
                this.config.Save();
            }
        }
        else
        {
            ImGui.TextColored(BackendUI.Red, "You have no presets. Please create one using the \"New preset\" button.");
        }

        BackendUI.NewPresetButton<ElevenLabsVoicePreset>($"New preset##{MemoizedId.Create()}", this.config);

        if (!presets.Any() || currentVoicePreset is null)
        {
            return;
        }

        ImGui.SameLine();
        BackendUI.DeletePresetButton(
            $"Delete preset##{MemoizedId.Create()}",
            currentVoicePreset,
            TTSBackend.ElevenLabs,
            this.config);
        if (string.IsNullOrEmpty(currentVoicePreset.VoiceName))
        {
            var defaultVoice = loadedVoices.SelectMany(e => e.Value).Where(e => e.Category == "generated").FirstOrDefault();
            currentVoicePreset.VoiceName = defaultVoice.Name;
            currentVoicePreset.VoiceId = defaultVoice.VoiceId;
        }
        var presetName = currentVoicePreset.Name;
        if (ImGui.InputText($"Preset name##{MemoizedId.Create()}", ref presetName, 64))
        {
            currentVoicePreset.Name = presetName;
            this.config.Save();
        }
        
        {
            var voiceCategories = this.getVoices.Invoke();
            var voiceCategoriesFlat = voiceCategories.SelectMany(vc => vc.Value).ToList();
            var voiceNames = voiceCategoriesFlat.Select(v => v.Name).ToArray();
            var voiceIds = voiceCategoriesFlat.Select(v => v.Name).ToArray();
            var voiceIndex = Array.IndexOf(voiceIds, currentVoicePreset.VoiceName);
            if (ImGui.BeginCombo($"Voice##{MemoizedId.Create()}", voiceNames[voiceIndex]))
            {
                foreach (var (category, voices) in voiceCategories)
                {
                    ImGui.Selectable(category, false, ImGuiSelectableFlags.Disabled);
                    foreach (var voice in voices)
                    {
                        if (ImGui.Selectable($"  {voice.Name}"))
                        {
                            currentVoicePreset.VoiceName = voice.Name;
                            currentVoicePreset.VoiceId = voice.VoiceId;
                            this.config.Save();
                        }
        
                        if (voice.Name == currentVoicePreset.VoiceName)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }
        
                ImGui.EndCombo();
            }
        
            if (voiceCategoriesFlat.Count == 0)
            {
                ImGui.TextColored(BackendUI.Red,
                    "No voices were found. This might indicate a temporary service outage.");
            }
        }

        var playbackRate = currentVoicePreset.PlaybackRate;
        if (ImGui.SliderInt($"Playback rate##{MemoizedId.Create()}", ref playbackRate, 20, 200, "%d%%",
                ImGuiSliderFlags.AlwaysClamp))
        {
            currentVoicePreset.PlaybackRate = playbackRate;
            this.config.Save();
        }

        var volume = (int)(currentVoicePreset.Volume * 100);
        if (ImGui.SliderInt($"Volume##{MemoizedId.Create()}", ref volume, 0, 100))
        {
            currentVoicePreset.Volume = (float)Math.Round((double)volume / 100, 2);
            this.config.Save();
        }
        var stability = (int)(currentVoicePreset.Stability * 100);
        if (ImGui.SliderInt($"Stability##{MemoizedId.Create()}", ref stability, 0, 100))
        {
            currentVoicePreset.Stability = Math.Round((double)stability / 100, 2);
            this.config.Save();
        }
        var similarity = (int)(currentVoicePreset.SimilarityBoost * 100);
        if (ImGui.SliderInt($"Similarity##{MemoizedId.Create()}", ref similarity, 0, 100))
        {
            currentVoicePreset.SimilarityBoost = Math.Round((double)similarity / 100, 2);
            this.config.Save();
        }

        ImGui.Text("Lexicons");
        ImGui.TextColored(BackendUI.HintColor, "Lexicons are not supported on the ElevenLabs backend.");

        ImGui.Spacing();

        {
            ConfigComponents.ToggleUseGenderedVoicePresets(
                    $"Use gendered voices##{MemoizedId.Create()}",
                    this.config);

            ImGui.Spacing();
            if (this.config.UseGenderedVoicePresets)
            {
                
                BackendUI.GenderedPresetConfig("ElevenLabs", TTSBackend.ElevenLabs, this.config, presets);
            }
        }
    }
}