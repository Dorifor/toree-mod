using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;

namespace TestPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Harmony.CreateAndPatchAll(typeof(Patches));
        }
    }

    public class Patches
    {
        private static AssetBundle bundle;
        private static GameObject shrekPrefab;
        private static GameObject shrekInstance;
        
        [HarmonyPatch(typeof(GameManager), "StartGame")]
        [HarmonyPostfix]
        static void StartGame()
        {
            // Don't hide cursor on game load
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Load asset bundle if not already loaded
            if (bundle == null)
            {
                // at runtime it will check inside the dataPath directory (a.k.a "Toree3d_Data/")
                bundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, "joj.cube"));

                if (bundle == null)
                {
                    Debug.Log("fail xd");
                    return;
                }
                
                // Load asset only once from bundle
                shrekPrefab = bundle.LoadAsset<GameObject>("assets/shrek.prefab");
            }
        }

        [HarmonyPatch(typeof(CharacterSelectionScript), "ChangeInfoPanel")]
        [HarmonyPostfix]
        static void ChangeInfoPanel(CharacterSelectionScript __instance)
        {
            // some tests 
            __instance.InfoPanelName.text = "Mao";
            __instance.InfoPanelName_Shadow.text = "Mao";
            __instance.InfoPanelFeature.text = "TESTETESTETEST";
        }

        [HarmonyPatch(typeof(PlayerSystem), "Start")]
        [HarmonyPostfix]
        static void ReplaceSkinByShrek(PlayerSystem __instance)
        {
            // most of this was done by trying things out, it's really context specific
            var playerModel = __instance.transform.Find("PlayerModel").gameObject;
            shrekInstance = GameObject.Instantiate(shrekPrefab, playerModel.transform);
            shrekInstance.transform.position = playerModel.transform.position;
            shrekInstance.layer = 8;

            var chickModel = playerModel.transform.Find("chick").gameObject;
            chickModel.SetActive(false);
            
            var shadowManager = chickModel.GetComponentInChildren<ShadowScript>();
            shadowManager.transform.SetParent(shrekInstance.transform);
            var shadow = shadowManager.ShadowObject.GetComponentInChildren<Projector>();
            shadow.transform.position += new Vector3(0, -.5f, 0);
        }

        
        // the 4 scripts from "GamePadScript" here are originally spamming a Debug.Log each frame, it was annoying while debugging
        // so I replicated the function without the logging
        [HarmonyPatch(typeof(GamePadScript), "cleft")]
        [HarmonyPrefix]
        static bool cleft(GamePadScript __instance, ref float __result)
        {
            __result = __instance.GetInt("CameraXInverted") == 1 ? __instance.crightIntern() : __instance.cleftIntern();
            return false;
        }

        [HarmonyPatch(typeof(GamePadScript), "cright")]
        [HarmonyPrefix]
        static bool cright(GamePadScript __instance, ref float __result)
        {
            __result = __instance.GetInt("CameraXInverted") == 1 ? __instance.cleftIntern() : __instance.crightIntern();
            return false;
        }

        [HarmonyPatch(typeof(GamePadScript), "cup")]
        [HarmonyPrefix]
        static bool cup(GamePadScript __instance, ref float __result)
        {
            __result = __instance.GetInt("CameraYInverted") == 1 ? __instance.cdownIntern() : __instance.cupIntern();
            return false;
        }

        [HarmonyPatch(typeof(GamePadScript), "cdown")]
        [HarmonyPrefix]
        static bool cdown(GamePadScript __instance, ref float __result)
        {
            __result = __instance.GetInt("CameraYInverted") == 1 ? __instance.cupIntern() : __instance.cdownIntern();
            return false;
        }

        // activate animations for my shrek prefab
        [HarmonyPatch(typeof(PlayerSystem), "MoveController")]
        [HarmonyPostfix]
        static void MoveController(PlayerSystem __instance, ref Vector3 ___accelerationDirection)
        {
            var animator = shrekInstance.GetComponent<Animator>();
            if (Input.GetButton("Jump")) {
                animator.SetTrigger("jump");
            }
            
            var speed = __instance.LastPos == __instance.transform.position ? 0f : 1f;
            animator.SetFloat("speed", speed);
        }
    }
}
