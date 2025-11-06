using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using WTTServerCommonLib.Models;
using Range = SemanticVersioning.Range;

namespace WTTTheLongLostHeadsOfYojenkz;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.wtt.yojenkzheads";
    public override string Name { get; init; } = "WTT-TheLongLostHeadsOfYojenkz";
    public override string Author { get; init; } = "GrooveypenguinX";
    public override List<string>? Contributors { get; init; } = null;
    public override SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version?.ToString(3));
    public override Range SptVersion { get; init; } = new("~4.0.1");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~2.0.1") }
    };
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 20)]
public class YojenkzHeads(
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{
    internal static readonly Dictionary<string, string> CharacterAudioMap = new(StringComparer.OrdinalIgnoreCase)
    {
        {"Big Boss", "big_boss"},
        {"Kaz Miller", "kaz_miller"},
        {"Revolver Ocelot", "revolver_ocelot"},
        {"Homelander", "homelander"},
        {"Chris Redfield", "chris_redfield"},
        {"Dante", "dante"},
        {"Duke Nukem", "duke_nukem"},
        {"Geralt", "geralt"},
        {"Norman Reedus", "norman_reedus"},
        {"Sam Fisher", "sam_fisher"}
    };
    public async Task OnLoad()
    {
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        await wttCommon.CustomHeadService.CreateCustomHeads(assembly);
        await wttCommon.CustomVoiceService.CreateCustomVoices(assembly);
        wttCommon.CustomAudioService.RegisterAudioBundles(assembly);
        foreach (var kvp in CharacterAudioMap)
        {
            wttCommon.CustomAudioService.CreateFaceCardAudio(kvp.Key, kvp.Value, true);
        }
    }
}
