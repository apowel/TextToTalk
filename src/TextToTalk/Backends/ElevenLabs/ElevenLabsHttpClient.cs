using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using Newtonsoft.Json;

namespace TextToTalk.Backends.ElevenLabs;

public class ElevenLabsHttpClient : IDisposable
{
    public HttpClient _client;
    private const string BaseUrl = "https://api.elevenlabs.io/v1/";
    private readonly StreamSoundQueue _soundQueue;
    public string ApiKey { private get; set; }
    public string ApiSecret { private get; set; }
    public ElevenLabsHttpClient(StreamSoundQueue soundQueue, HttpClient http)
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri("https://api.elevenlabs.io/v1/");
        
        _soundQueue = soundQueue;
    }

    public TextSource GetCurrentlySpokenTextSource()
    {
        return this._soundQueue.GetCurrentlySpokenTextSource();
    }

    // public async Task<IDictionary<string, IList<ElevenLabsVoice>>> GetVoices()
    // {
    //     var voicesRes = await this._client.GetStringAsync(new Uri($"{BaseUrl}voices"));
    //     var voices = JsonConvert.DeserializeObject<List<ElevenLabsVoice>>(voicesRes);
    //     return voices
    //         .GroupBy(v => v.Category)
    //         .ToImmutableSortedDictionary(
    //             g => g.Key,
    //             g => (IList<ElevenLabsVoice>)g.OrderByDescending(v => v.Name).ToList());
    // }
    public async Task<IDictionary<string, IList<ElevenLabsVoice>>> GetVoices()
    {
        var requestUrl = $"{BaseUrl}voices";
        var response = await _client.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ElevenLabsVoices>(jsonResponse);
            var voices = result.Voices;
            return voices
                .GroupBy(v => v.Category)
                .ToImmutableSortedDictionary(
                    g => g.Key,
                    g => (IList<ElevenLabsVoice>)g.OrderByDescending(v => v.Name).ToList());
        }
        else
        {
            return null;
        }
        throw new Exception($"API request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
    }
    public async Task Say(string voiceId, string text, TextSource source, float volume, double? stability, double? similarityBoost)
    {
        
        var requestUrl = $"{BaseUrl}text-to-speech/{voiceId}/stream";
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
            _soundQueue.EnqueueSound(audio, source, StreamFormat.Mp3, volume);
            return;
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