using BepInEx;
using HarmonyLib;

namespace ContentSpectateEnemies
{
    [BepInPlugin("SpectateEnemies", "SpectateEnemies", "1.1.0")]
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
