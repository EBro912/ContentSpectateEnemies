using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Input = UnityEngine.Input;

namespace ContentSpectateEnemies
{
    internal class SpectateEnemies : MonoBehaviour
    {
        public static SpectateEnemies Instance;

        private static FieldInfo lookDirection = null;
        private static MethodInfo look = null;
        private static MethodInfo switching = null;

        public int SpectatedEnemyIndex = -1;
        public bool SpectatingEnemies = false;
        public float ZoomLevel = 1f;

        private static MethodInfo start = null;

        private readonly GUIStyle centerStyle = new GUIStyle()
        {
            fontSize = 50,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            },
            alignment = TextAnchor.UpperCenter
        };

        private readonly GUIStyle rightStyle = new GUIStyle()
        {
            fontSize = 30,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            },
            alignment = TextAnchor.UpperRight,

        };

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            lookDirection = AccessTools.Field(typeof(Spectate), "lookDirection");
            look = AccessTools.Method(typeof(Spectate), "Look");
            switching = AccessTools.Method(typeof(Spectate), "Switching");
            start = AccessTools.Method(typeof(Spectate), "StartSpectate");

            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            if (Spectate.spectating)
            {
                if (SpectatingEnemies)
                {
                    Bot current = BotHandler.instance.bots.ElementAtOrDefault(SpectatedEnemyIndex);
                    if (current == null)
                    {
                        return;
                    }
                    GUI.Label(new Rect((Screen.width / 2) - 150, 20, 300, 100), string.Format("Spectating: {0}", current.transform.parent.name.Replace("(Clone)", "")), centerStyle);
                }
                else if (Player.observedPlayer != null)
                {
                    // why
                    string playerName = Player.observedPlayer.refs.view.Owner.NickName;
                    GUI.Label(new Rect((Screen.width / 2) - 150, 20, 300, 100), string.Format("Spectating: {0}", playerName), centerStyle);
                }
                GUI.Label(new Rect(Screen.width - 320, 20, 300, 300), $"Controls:\n[E] Spectate {(SpectatingEnemies ? "Players" : "Enemies")}\n[F] Toggle Light\n[A] Previous\n[D] Next", rightStyle);
            }
        }

        private void Update()
        {
            if (Spectate.spectating)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    SpectatingEnemies = !SpectatingEnemies;
                    if (SpectatingEnemies)
                    {
                        if (BotHandler.instance.bots.Count == 0)
                        {
                            SpectatingEnemies = false;
                            Debug.LogWarning("No enemies to spectate");
                            return;
                        }
                        if (SpectatedEnemyIndex == -1 || SpectatedEnemyIndex >= BotHandler.instance.bots.Count)
                        {
                            GetNextValidSpectatable();
                        }
                    }
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    Light light = MainCamera.instance.GetComponent<Light>();
                    light.enabled = !light.enabled;
                }
            }

            /*if (Input.GetKeyDown(KeyCode.K))
            {
                start.Invoke(FindObjectOfType<Spectate>(), null);
            }*/

            /*
            ZoomLevel -= Input.mouseScrollDelta.y * 0.1f;
            ZoomLevel = Mathf.Clamp(ZoomLevel, 0.1f, 10f);
            */
        }

        // rewrite Spectate.DoSpectate with our own code
        // TODO: do this in a better way
        public void DoSpectate(Spectate spectate)
        {
            if (!Player.localPlayer.data.dead)
            {
                SpectatingEnemies = false;
                return;
            }
            if (BotHandler.instance.bots.Count == 0)
            {
                SpectatingEnemies = false;
                return;
            }
            Bot currentEnemy = BotHandler.instance.bots.ElementAtOrDefault(SpectatedEnemyIndex);
            if (currentEnemy == null)
            {
                GetNextValidSpectatable();
                return;
            }
            spectate.transform.rotation = Quaternion.LookRotation((Vector3)lookDirection.GetValue(spectate));
            spectate.transform.position = currentEnemy.centerTransform.position + Vector3.up * 0.75f;

            Vector3 vector = spectate.transform.position + spectate.transform.forward * -3f;
            RaycastHit hit = HelperFunctions.LineCheck(spectate.transform.position, vector, HelperFunctions.LayerType.TerrainProp, 0f);
            if (hit.transform)
            {
                spectate.transform.position += -spectate.transform.forward * (hit.distance - 0.2f);
            }
            else
            {
                spectate.transform.position = vector + spectate.transform.forward * 0.2f;
            }
            look.Invoke(spectate, null);
            switching.Invoke(spectate, null);
        }

        public void SwitchEnemy()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                SpectatedEnemyIndex--;
                if (SpectatedEnemyIndex < 0)
                {
                    SpectatedEnemyIndex = BotHandler.instance.bots.Count - 1;
                }
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                SpectatedEnemyIndex++;
                if (SpectatedEnemyIndex >= BotHandler.instance.bots.Count)
                {
                    SpectatedEnemyIndex = 0;
                }
            }
        }

        private void GetNextValidSpectatable()
        {
            int enemiesChecked = 0;
            int current = SpectatedEnemyIndex;
            while (enemiesChecked < BotHandler.instance.bots.Count)
            {
                current++;
                if (current >= BotHandler.instance.bots.Count)
                {
                    current = 0;
                }
                Bot enemy = BotHandler.instance.bots.ElementAtOrDefault(current);
                if (enemy != null)
                {
                    SpectatedEnemyIndex = current;
                    return;
                }
                enemiesChecked++;
            }
            SpectatingEnemies = false;
            Debug.LogWarning("No enemies to spectate");
        }
    }

    [HarmonyPatch(typeof(Spectate), "DoSpectate")]
    internal class Spectate_DoSpectate
    {
        private static bool Prefix(Spectate __instance)
        {
            if (SpectateEnemies.Instance.SpectatingEnemies)
            {
                SpectateEnemies.Instance.DoSpectate(__instance);
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Spectate), "Switching")]
    internal class Spectate_Switching
    {
        private static bool Prefix()
        {
            if (SpectateEnemies.Instance.SpectatingEnemies)
            {
                SpectateEnemies.Instance.SwitchEnemy();
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(JumpScareSound), "Scare")]
    internal class JumpScareSound_Scare
    {
        private static bool Prefix()
        {
            // dont jumpscare the player when they're spectating
            if (SpectateEnemies.Instance.SpectatingEnemies)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(RoundSpawner), "Start")]
    internal class RoundSpawner_Start
    {
        private static void Postfix()
        {
            if (MainCamera.instance.gameObject.GetComponent<Light>() == null)
            {
                Light light = MainCamera.instance.gameObject.AddComponent<Light>();
                light.color = Color.white;
                light.type = LightType.Point;
                light.shadows = LightShadows.None;
                light.range = 100f;
                light.intensity = 8f;
                light.enabled = false;
            }
        }
    }
 
    [HarmonyPatch(typeof(MainMenuHandler), "Awake")]
    internal class MainMenuHandler_Awake
    {
        private static void Postfix()
        {
            if (SpectateEnemies.Instance == null)
            {
                GameObject obj = new GameObject("SpectateEnemiesObject");
                obj.AddComponent<SpectateEnemies>();
            }
        }
    }

    [HarmonyPatch(typeof(Spectate), "StopSpectate")]
    internal class Spectate_Stop
    {
        /*private static bool Prefix()
        {
            return false;
        }*/
        private static void Postfix()
        {
            Light light = MainCamera.instance.gameObject.GetComponent<Light>();
            if (light != null)
            {
                light.enabled = false;
            }
        }
    }
   
}
