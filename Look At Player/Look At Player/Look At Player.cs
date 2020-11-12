using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.UI_New.InGame;
using Harmony;
using MelonLoader;
using UnityEngine;

namespace LookAtPlayer {

    public class LookAtPlayerMod : MelonMod { }

    [HarmonyPatch(typeof(InGame), "Update")]
    public class Update_Patch {
        [HarmonyPostfix]
        public static void Postfix() {
            if (InGame.Bridge == null) return;
            foreach (TowerToSimulation tts in InGame.Bridge.GetAllTowers()) {
                Camera camera = InGame.instance.sceneCamera;
                tts?.tower?.Node?.graphic?.transform.LookAt(camera.ScreenToWorldPoint(
                    new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2, camera.nearClipPlane)));
            }
        }
    }
}