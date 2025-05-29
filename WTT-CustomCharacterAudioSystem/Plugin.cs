using BepInEx;
using CustomCharacterAudioSystem.Configuration;
using CustomCharacterAudioSystem.Patches;

namespace CustomCharacterAudioSystem
{
    [BepInPlugin("WTT-CustomCharacterAudioSystem.UniqueGUID", "WTT-CustomCharacterAudioSystem", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            RadioSettings.Init(Config);
            new FaceCardViewInitPatch().Enable();
            new FaceCardViewTogglePatch().Enable();
            new BoomboxAudioPatch().Enable();
        }
    }
}
