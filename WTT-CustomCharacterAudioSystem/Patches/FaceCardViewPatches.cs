using System;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using Arena.UI;
using CustomCharacterAudioSystem.Components;
using EFT.InventoryLogic;
using UnityEngine.UI;

namespace CustomCharacterAudioSystem.Patches
{
    internal class FaceCardViewInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(FaceCardView), "Init", new[] {
                typeof(string),
                typeof(InventoryEquipment),
                typeof(GClass1965),
                typeof(ToggleGroup),
                typeof(int),
                typeof(Action<int>),
                typeof(bool)
            });
        }

        [PatchPostfix]
        static void Postfix(FaceCardView __instance, string faceName)
        {
            if (CharacterAudioHandler.CharacterAudioMap.ContainsKey(faceName))
            {
                var audioHandler = __instance.gameObject.AddComponent<CharacterAudioHandler>();
                audioHandler.Initialize(faceName);
            }
        }
    }
    internal class FaceCardViewTogglePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(FaceCardView), "method_0", new[] { typeof(bool) });
        }

        [PatchPostfix]
        private static void Postfix(FaceCardView __instance, bool isSelected)
        {
            var handler = __instance.GetComponent<CharacterAudioHandler>();
            if (handler != null)
            {
                if (isSelected)
                {
                    handler.FadeIn();
                }
                else
                {
                    handler.FadeOut();
                }
            }
        }
    }

}
