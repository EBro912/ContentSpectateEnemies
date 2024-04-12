using BepInEx;
using HarmonyLib;

namespace ContentSpectateEnemies
{
    [ContentWarningPlugin("SpectateEnemies", "1.2.0", true)]
    [BepInPlugin("SpectateEnemies", "SpectateEnemies", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        private void Awake()
        {
            harmony = new Harmony("SpectateEnemies");
            harmony.PatchAll();

            Logger.LogInfo("SpectateEnemies loaded!");
        }
    }
}
