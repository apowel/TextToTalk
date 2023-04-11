﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ElevenLabs;
using ElevenLabs.Voices;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using TextToTalk.Lexicons;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsHttpClient : IDisposable
{
    private readonly HttpClient _client;
    private const string BaseUrl = "https://api.elevenlabs.io/v1/";
    private readonly StreamSoundQueue _soundQueue;
    public string ApiKey { private get; set; }
    public string ApiSecret { private get; set; }
    public ElevenLabsHttpClient(StreamSoundQueue soundQueue, HttpClient http)
    {
        _client = http;
        _client.BaseAddress = new Uri("https://api.elevenlabs.io/v1/");
        _client.DefaultRequestHeaders.Add("xi-api-key", ApiKey);
        _soundQueue = soundQueue;
    }

    public TextSource GetCurrentlySpokenTextSource()
    {
        return this._soundQueue.GetCurrentlySpokenTextSource();
    }

    public async Task<IDictionary<string, IList<ElevenLabsVoice>>> GetVoices()
    {
        var voicesRes = await this._client.GetStringAsync(new Uri("https://api.elevenlabs.io/v1/voices"));
        var voices = JsonConvert.DeserializeObject<List<ElevenLabsVoice>>(voicesRes);
        return voices
            .GroupBy(v => v.Category)
            .ToImmutableSortedDictionary(
                g => g.Key,
                g => (IList<ElevenLabsVoice>)g.OrderByDescending(v => v.Name).ToList());
    }
    public async Task Say(string voiceId, string text, TextSource source, float volume, int? stability, int? similarityBoost)
    {
        
        var requestUrl = $"{BaseUrl}/text-to-speech/{voiceId}/stream";
        var requestBody = new
        {
            text,
            voice_settings = new
            {
                stability,
                similarity_boost = similarityBoost
            }
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(requestUrl, content);
        
        if (response.IsSuccessStatusCode)
        {
            var audio = new MemoryStream();
            await response.Content.CopyToAsync(audio);
            audio.Position = 0;
            _soundQueue.EnqueueSound(audio, source, StreamFormat.Wave, volume);
        }

        throw new Exception($"API request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
    }
    public Task CancelAllSounds()
    {
        _soundQueue.CancelAllSounds();
        return Task.CompletedTask;
    }

    public Task CancelFromSource(TextSource source)
    {
        _soundQueue.CancelFromSource(source);
        return Task.CompletedTask;
    }

    private static void HandleResult(SynthesisVoicesResult res)
    {
        if (!string.IsNullOrEmpty(res.ErrorDetails))
        {
            DetailedLog.Error($"ElevenLabs request error: ({res.Reason}) \"{res.ErrorDetails}\"");
        }
    }

    private static void HandleResult(SpeechSynthesisResult res)
    {
        if (res.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(res);
            if (cancellation.Reason == CancellationReason.Error)
            {
                DetailedLog.Error($"ElevenLabs request error: ({cancellation.ErrorCode}) \"{cancellation.ErrorDetails}\"");
            }
            else
            {
                DetailedLog.Warn($"ElevenLabs request failed in state \"{cancellation.Reason}\"");
            }

            return;
        }

        if (res.Reason != ResultReason.SynthesizingAudioCompleted)
        {
            DetailedLog.Warn($"Speech synthesis request completed in incomplete state \"{res.Reason}\"");
        }
    }

    public void Dispose()
    {
        _soundQueue?.Dispose();
    }
}