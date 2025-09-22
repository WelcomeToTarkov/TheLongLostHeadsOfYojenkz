using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EFT.UI;
using UnityEngine;

namespace CustomCharacterAudioSystem.Helpers;

public abstract class AudioHelpers
{

    internal static readonly Dictionary<string, string> CharacterIDAudioMap = new(StringComparer.OrdinalIgnoreCase)
    {
        {"6747aa6da1f90f53496a3409", "WTT-CustomCharacterAudioSystem.Audio.big_boss.wav"},
        {"6747aa8655e907f08493bf81", "WTT-CustomCharacterAudioSystem.Audio.kaz_miller.wav"},
        {"6747aa95abe95c1bba17c428", "WTT-CustomCharacterAudioSystem.Audio.revolver_ocelot.wav"},
        {"6747aa8b30c9993df151d732", "WTT-CustomCharacterAudioSystem.Audio.homelander.wav"},
        {"6747aa715be2c2e443264f32", "WTT-CustomCharacterAudioSystem.Audio.chris_redfield.wav"},
        {"6747aa75291e2a53acb3eb40", "WTT-CustomCharacterAudioSystem.Audio.dante.wav"},
        {"6747aa7a084084dbe8cb7958", "WTT-CustomCharacterAudioSystem.Audio.duke_nukem.wav"},
        {"6747aa80c8deb234cb9d4950", "WTT-CustomCharacterAudioSystem.Audio.geralt.wav"},
        {"6747aa90dfdb9c64d1dc2ee0", "WTT-CustomCharacterAudioSystem.Audio.norman_reedus.wav"},
        {"6747aa9ab2c23aa745b468ac", "WTT-CustomCharacterAudioSystem.Audio.sam_fisher.wav"},
        {"8a572166190d4833babf811d", "WTT-CustomCharacterAudioSystem.Audio.johnny_silverhand.wav" },
        {"3fe94530f8524f53953849ed", "WTT-CustomCharacterAudioSystem.Audio.cheech_marin.wav"},
        {"2e10f56dcbf747e89c15878f", "WTT-CustomCharacterAudioSystem.Audio.tommy_chong.wav"}
    };
    
    public static byte[] LoadCharacterAudio(string resourcePath)
    {
        byte[] wavData = AudioHelpers.LoadEmbeddedResource(resourcePath);
        if (wavData == null)
        {
            ConsoleScreen.LogError($"Failed to load audio resource: {resourcePath}");
            return null;
        }
        return wavData;
    }
    public static AudioClip ParseWAV(byte[] wavData, string name)
    {
        try
        {
            string cleanName = string.Join("", name.Split(Path.GetInvalidFileNameChars()));
            
            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            
            float[] audioData = Convert16BitByteArrayToAudioData(wavData, 44);
            
            AudioClip clip = AudioClip.Create(
                cleanName, // Use original cleaned name
                audioData.Length / channels, 
                channels, 
                sampleRate, 
                false
            );
            
            clip.SetData(audioData, 0);
            return clip;
        }
        catch (Exception e)
        {
            ConsoleScreen.LogError($"WAV parse error: {e.Message}");
            return null;
        }
    }

    private static byte[] LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    private static float[] Convert16BitByteArrayToAudioData(byte[] source, int headerSize)
    {
        int sampleCount = (source.Length - headerSize) / 2;
        float[] audioData = new float[sampleCount];
        
        for (int i = 0; i < sampleCount; i++)
        {
            int offset = headerSize + i * 2;
            short sample = BitConverter.ToInt16(source, offset);
            audioData[i] = sample / 32768f;
        }
        
        return audioData;
    }


}