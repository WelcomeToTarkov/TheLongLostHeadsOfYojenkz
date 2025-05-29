using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CustomCharacterAudioSystem.Components;
using CustomCharacterAudioSystem.Configuration;
using CustomCharacterAudioSystem.Helpers;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

internal class BoomboxAudioPatch : ModulePatch
{
    private static readonly Dictionary<string, AudioClip> _audioCache = new();
    private static readonly HashSet<string> _pendingLoads = new(); // Changed to HashSet
    private static readonly System.Random _random = new();
    private static readonly object _lock = new();

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(AudioArray), nameof(AudioArray.PlayWithOffset));
    }

    [PatchPrefix]
    private static bool Prefix(
        AudioArray __instance,
        ref AudioClip sound, bool volumetric, float offset, Action onFinish, Action onCancel,
        out AudioClip __state)
    {
        __state = null;

        try
        {
            if (__instance.gameObject.name != "BoomboxAudio")
                return true;

            var tracker = __instance.gameObject.GetComponent<HideoutRadioStateTracker>();
            if (tracker == null)
            {
                tracker = __instance.gameObject.AddComponent<HideoutRadioStateTracker>();
            }

            if (ShouldSkipPlayback(tracker))
                return true;

            var player = GamePlayerOwner.MyPlayer;
            if (!TryGetAudioResource(player, out var audioResource))
                return true;

            if (!_audioCache.TryGetValue(audioResource, out var customClip))
            {
                StartAsyncLoad(audioResource, __instance, sound);
                return true;
            }

            __state = sound;
            sound = customClip;
            tracker.HasPlayedFirstEntranceAudio = true;
        

            float volume = GetVolumeForLocation(tracker.Location);
            __instance.GetComponent<AudioSource>().volume = volume;

            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            ConsoleScreen.LogError($"Prefix error: {ex}");
#endif
            return true;
        }
    }
    
    [PatchPostfix]
    private static void Postfix(
        AudioArray __instance,
        AudioClip sound, bool volumetric, float offset, Action onFinish, Action onCancel,
        AudioClip __state)
    {
        if (__state == null || sound == __state) return;

        try
        {
            var tracker = __instance.gameObject.GetComponent<HideoutRadioStateTracker>();
            if (tracker == null) return;

            var sources = __instance.GetComponents<AudioSource>();
            foreach (var source in sources)
            {
                if (source.clip == sound)
                {
                    var revert = source.gameObject.AddComponent<RevertClipComponent>();
                    revert.Initialize(source, __state, () => { });
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            ConsoleScreen.LogError($"Postfix error: {ex}");
#endif
        }
    }
    
    private static void StartAsyncLoad(string audioResource, AudioArray instance, AudioClip originalClip)
    {
        lock (_lock)
        {
            // Skip if already loaded or loading
            if (_audioCache.ContainsKey(audioResource) || 
                !_pendingLoads.Add(audioResource)) // Fixed Linq type inference
                return;
        }
        
        // Start loading coroutine
        instance.StartCoroutine(LoadAndCacheAudioCoroutine(audioResource, originalClip));
    }
    
    private static IEnumerator LoadAndCacheAudioCoroutine(string audioResource, AudioClip originalClip)
    {
        // Load audio bytes
        byte[] audioData = null;
        yield return LoadResourceAsync(audioResource, data => audioData = data);
        
        if (audioData == null)
        {
            RemovePending(audioResource);
            yield break;
        }

        // Parse WAV
        AudioClip clip = null;
        yield return ParseWavAsync(audioResource, audioData, result => clip = result);
        
        if (clip == null)
        {
            RemovePending(audioResource);
            yield break;
        }

        // Add to cache
        lock (_lock)
        {
            _audioCache[audioResource] = clip;
            _pendingLoads.Remove(audioResource);
        }

        // Update all instances
        HotSwapAllInstances(audioResource, originalClip);
    }
    
    private static void RemovePending(string audioResource)
    {
        lock (_lock)
        {
            _pendingLoads.Remove(audioResource);
        }
    }
    
    private static void HotSwapAllInstances(string audioResource, AudioClip originalClip)
    {
        if (!_audioCache.TryGetValue(audioResource, out var customClip)) 
            return;

        var boomboxes = UnityEngine.Object.FindObjectsOfType<AudioArray>()
            .Where(a => a.gameObject.name == "BoomboxAudio");

        foreach (var instance in boomboxes)
        {
            var tracker = instance.gameObject.GetComponent<HideoutRadioStateTracker>();
            if (tracker == null) continue;

            float volume = GetVolumeForLocation(tracker.Location);

            var sources = instance.GetComponents<AudioSource>();
            foreach (var source in sources)
            {
                if (source.clip == originalClip && source.isPlaying)
                {
                    source.clip = customClip;
                    source.volume = volume; // Set volume here
                    source.Play();
#if DEBUG
                    ConsoleScreen.Log($"Hot-swapped audio for {audioResource} on {instance.gameObject.name} at volume {volume}");
#endif
                }
            }
        }
    }

    private static float GetVolumeForLocation(RadioLocation location)
    {
        return location switch
        {
            RadioLocation.Gym => RadioSettings.GymRadioVolume.Value,
            RadioLocation.RestSpace => RadioSettings.RestSpaceRadioVolume.Value,
            _ => 1f
        };
    }

    private static IEnumerator LoadResourceAsync(string resourcePath, Action<byte[]> callback)
    {
#if DEBUG
                ConsoleScreen.Log($"LoadResourceAsync: {resourcePath}");

#endif
        
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            ConsoleScreen.LogError($"Resource not found: {resourcePath}");
            callback(null);
            yield break;
        }

        var buffer = new byte[stream.Length];
        int read;
        var totalRead = 0;
            
        while ((read = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
        {
            totalRead += read;
#if DEBUG
                ConsoleScreen.Log($"Loaded {totalRead}/{buffer.Length} bytes");
#endif
            yield return null;
        }
            
        callback(buffer);
    }

    private static IEnumerator ParseWavAsync(string resourcePath, byte[] wavData, Action<AudioClip> callback)
    {
#if DEBUG
        
        ConsoleScreen.Log($"ParseWavAsync started: {resourcePath}");
        
#endif
        Task<AudioClip> parseTask = null;
        try
        {
            parseTask = Task.Run(() => 
                AudioHelpers.ParseWAV(wavData, Path.GetFileNameWithoutExtension(resourcePath)));
        }
        catch (Exception ex)
        {
            ConsoleScreen.LogError($"Parse task failed: {ex}");
            callback(null);
            yield break;
        }

        while (!parseTask.IsCompleted)
        {
#if DEBUG
            ConsoleScreen.Log("Waiting for WAV parsing...");
#endif
            yield return null;
        }

        if (parseTask.Exception != null)
        {
            ConsoleScreen.LogError($"Parse error: {parseTask.Exception}");
            callback(null);
            yield break;
        }

        var clip = parseTask.Result;
        if (clip != null)
        {
#if DEBUG
            ConsoleScreen.Log($"Parsed successfully: {clip.name} ({clip.length}s)");
#endif
            callback(clip);
        }
        else
        {
            ConsoleScreen.LogError($"Null clip returned for: {resourcePath}");
            callback(null);
        }
    }

    // REMOVED: private static async Task<AudioClip> LoadAudioAsync(...)

    private static bool ShouldSkipPlayback(HideoutRadioStateTracker tracker)
    {
        if (!tracker.Enabled)
        {
#if DEBUG
            ConsoleScreen.Log($"Skipping playback for {tracker.Location} - radio disabled.");
#endif
            return true;
        }

        if (tracker.PlayOnFirstEntrance && !tracker.HasPlayedFirstEntranceAudio)
        {
#if DEBUG
            tracker.HasPlayedFirstEntranceAudio = true;
            ConsoleScreen.Log($"Playing intro track for {tracker.Location}");
#endif
            return false;
        }

        if (_random.Next(0, 100) > tracker.ReplacementChance)
        {
#if DEBUG
            ConsoleScreen.Log($"Random chance prevented replacement for {tracker.Location}");
#endif
            return true;
        }

        return false;
    }

    private static bool TryGetAudioResource(Player player, out string audioResource)
    {
        audioResource = null;
        if (player?.Profile == null || 
            !player.Profile.Customization.TryGetValue(EBodyModelPart.Head, out var headId))
            return false;

        return AudioHelpers.CharacterIDAudioMap.TryGetValue(headId, out audioResource);
    }
}