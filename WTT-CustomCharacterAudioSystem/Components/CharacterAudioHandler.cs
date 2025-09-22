using System;
using System.Collections;
using System.Collections.Generic;
using CustomCharacterAudioSystem.Configuration;
using CustomCharacterAudioSystem.Helpers;
using UnityEngine;

namespace CustomCharacterAudioSystem.Components;

public class CharacterAudioHandler : MonoBehaviour
{
    // Map of character names to embedded resource paths
    internal static readonly Dictionary<string, string> CharacterAudioMap = new(StringComparer.OrdinalIgnoreCase)
    {
        {"Big Boss", "WTT-CustomCharacterAudioSystem.Audio.big_boss.wav"},
        {"Kaz Miller", "WTT-CustomCharacterAudioSystem.Audio.kaz_miller.wav"},
        {"Revolver Ocelot", "WTT-CustomCharacterAudioSystem.Audio.revolver_ocelot.wav"},
        {"Homelander", "WTT-CustomCharacterAudioSystem.Audio.homelander.wav"},
        {"Chris Redfield", "WTT-CustomCharacterAudioSystem.Audio.chris_redfield.wav"},
        {"Dante", "WTT-CustomCharacterAudioSystem.Audio.dante.wav"},
        {"Duke Nukem", "WTT-CustomCharacterAudioSystem.Audio.duke_nukem.wav"},
        {"Geralt", "WTT-CustomCharacterAudioSystem.Audio.geralt.wav"},
        {"Norman Reedus", "WTT-CustomCharacterAudioSystem.Audio.norman_reedus.wav"},
        {"Sam Fisher", "WTT-CustomCharacterAudioSystem.Audio.sam_fisher.wav"},
        {"Marin", "WTT-CustomCharacterAudioSystem.Audio.cheech_marin.wav"},
        {"Chong", "WTT-CustomCharacterAudioSystem.Audio.tommy_chong.wav"},
        {"Johnny Silverhand", "WTT-CustomCharacterAudioSystem.Audio.johnny_silverhand.wav"}
    };
    private AudioSource _audioSource;
    private bool _isInitialized;
    private bool _isFadingIn;
    private bool _isFadingOut;
    private Coroutine _fadeCoroutine;

    public void Initialize(string faceName)
    {
        if (!CharacterAudioMap.TryGetValue(faceName, out var resourcePath))
            return;

        byte[] audioFile = AudioHelpers.LoadCharacterAudio(resourcePath);
        if (audioFile != null)
        {
            AssignAudioToSource(audioFile, faceName);
        }
        RadioSettings.FaceCardVolume.SettingChanged += OnFaceCardVolumeChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        RadioSettings.FaceCardVolume.SettingChanged -= OnFaceCardVolumeChanged;
    }
    
    
    private void OnFaceCardVolumeChanged(object sender, EventArgs e)
    {
        if (!_isInitialized || _audioSource == null) return;
            
        if (_audioSource.isPlaying && !_isFadingOut)
        {
            // If audio is playing and not fading out, update volume immediately
            _audioSource.volume = RadioSettings.FaceCardVolume.Value;
        }
    }

    public void AssignAudioToSource(byte[] wavData, string originalName)
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = AudioHelpers.ParseWAV(wavData, originalName);
        _audioSource.loop = true;
        _audioSource.volume = 0f; // Start at 0 for fade-in
        _audioSource.playOnAwake = false;
        _isInitialized = true;
    }
    public void FadeIn()
    {
        if (!_isInitialized) return;
            
        // Stop any active fade
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
            
        _fadeCoroutine = StartCoroutine(FadeAudio(RadioSettings.FaceCardVolume.Value, 5f));
    }

    public void FadeOut()
    {
        if (!_isInitialized) return;
            
        // Stop any active fade
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
            
        _fadeCoroutine = StartCoroutine(FadeAudio(0f, 5f));
    }

    private IEnumerator FadeAudio(float targetVolume, float duration)
    {
        if (_audioSource == null || _audioSource.clip == null) yield break;

        _isFadingIn = targetVolume > 0;
        _isFadingOut = targetVolume == 0;
            
        float startVolume = _audioSource.volume;
        float time = 0f;

        if (_isFadingIn && !_audioSource.isPlaying)
            _audioSource.Play();

        while (time < duration && _audioSource != null)
        {
            time += Time.deltaTime;
                
            // Use current config value for fade-in target
            float currentTarget = _isFadingIn ? 
                RadioSettings.FaceCardVolume.Value : 
                targetVolume;
                
            _audioSource.volume = Mathf.Lerp(startVolume, currentTarget, time / duration);
            yield return null;
        }

        if (_audioSource != null)
        {
            _audioSource.volume = _isFadingIn ? 
                RadioSettings.FaceCardVolume.Value : 
                targetVolume;
                
            if (_isFadingOut) _audioSource.Stop();
        }

        _isFadingIn = false;
        _isFadingOut = false;
    }
}
