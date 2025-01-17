﻿using System;
using System.Reactive.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using TextToTalk.Events;
using TextToTalk.Middleware;
using TextToTalk.Talk;

namespace TextToTalk.TextProviders;

// This might be almost exactly the same as AddonTalkHandler, but it's too early to pull out a common base class.
public class AddonBattleTalkHandler : IDisposable
{
    private record struct AddonBattleTalkState(string? Speaker, string? Text, AddonPollSource PollSource);

    private readonly AddonBattleTalkManager addonTalkManager;
    private readonly MessageHandlerFilters filters;
    private readonly ObjectTable objects;
    private readonly PluginConfiguration config;
    private readonly Framework framework;
    private readonly ComponentUpdateState<AddonBattleTalkState> updateState;
    private readonly IDisposable subscription;

    public Action<TextEmitEvent> OnTextEmit { get; set; }

    public AddonBattleTalkHandler(AddonBattleTalkManager addonTalkManager, Framework framework,
        MessageHandlerFilters filters, ObjectTable objects, PluginConfiguration config)
    {
        this.addonTalkManager = addonTalkManager;
        this.framework = framework;
        this.filters = filters;
        this.objects = objects;
        this.config = config;
        this.updateState = new ComponentUpdateState<AddonBattleTalkState>();
        this.updateState.OnUpdate += HandleChange;
        this.subscription = HandleFrameworkUpdate();

        OnTextEmit = _ => { };
    }

    private IObservable<AddonPollSource> OnFrameworkUpdate()
    {
        return Observable.Create((IObserver<AddonPollSource> observer) =>
        {
            void Handle(Framework _)
            {
                if (!this.config.Enabled) return;
                if (!this.config.ReadFromQuestTalkAddon) return;
                observer.OnNext(AddonPollSource.FrameworkUpdate);
            }

            this.framework.Update += Handle;
            return () => { this.framework.Update -= Handle; };
        });
    }

    private IDisposable HandleFrameworkUpdate()
    {
        return OnFrameworkUpdate()
            .Subscribe(PollAddon);
    }

    public void PollAddon(AddonPollSource pollSource)
    {
        var state = GetTalkAddonState(pollSource);
        this.updateState.Mutate(state);
    }

    private void HandleChange(AddonBattleTalkState state)
    {
        var (speaker, text, pollSource) = state;

        if (state == default)
        {
            // The addon was closed
            return;
        }

        text = TalkUtils.NormalizePunctuation(text);

        DetailedLog.Debug($"AddonBattleTalk: \"{text}\"");

        if (pollSource == AddonPollSource.VoiceLinePlayback && this.config.SkipVoicedBattleText)
        {
            DetailedLog.Debug($"Skipping voice-acted line: {text}");
            return;
        }

        // Do postprocessing on the speaker name
        if (this.filters.ShouldProcessSpeaker(speaker))
        {
            this.filters.SetLastSpeaker(speaker);

            var speakerNameToSay = speaker;
            if (this.config.SayPartialName)
            {
                speakerNameToSay = TalkUtils.GetPartialName(speakerNameToSay, this.config.OnlySayFirstOrLastName);
            }

            text = $"{speakerNameToSay} says {text}";
        }

        // Find the game object this speaker is representing
        var speakerObj = speaker != null ? ObjectTableUtils.GetGameObjectByName(this.objects, speaker) : null;
        if (!this.filters.ShouldSayFromYou(speaker)) return;

        OnTextEmit.Invoke(speakerObj != null
            ? new TextEmitEvent(TextSource.AddonTalk, speakerObj.Name, text, speakerObj)
            : new TextEmitEvent(TextSource.AddonTalk, state.Speaker ?? "", text, null));
    }

    private AddonBattleTalkState GetTalkAddonState(AddonPollSource pollSource)
    {
        if (!this.addonTalkManager.IsVisible())
        {
            return default;
        }

        var addonTalkText = this.addonTalkManager.ReadText();
        return addonTalkText != null
            ? new AddonBattleTalkState(addonTalkText.Speaker, addonTalkText.Text, pollSource)
            : default;
    }

    public void Dispose()
    {
        subscription.Dispose();
    }
}