using Assets.Scripts.Models.Powers;
using Assets.Scripts.Models.TowerSets;
using Assets.Scripts.Simulation.Input;
using Assets.Scripts.Unity;
using Harmony;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace AddBloon
{
    public class Utils
    {
        public static void registerTowerInInventory(ShopTowerDetailsModel details, string insertBefore, List<TowerDetailsModel> allTowersInTheGame)
        {
            // shift all towers after new tower by 1
            foreach (TowerDetailsModel allTowerDetails in allTowersInTheGame)
            {
                if (allTowerDetails.towerIndex >= details.towerIndex)
                {
                    allTowerDetails.towerIndex++;
                }
            }

            TowerDetailsModel towerAfter = allTowersInTheGame.ToArray().FirstOrDefault(tower => tower.towerId == insertBefore);
            allTowersInTheGame.Insert(allTowersInTheGame.IndexOf(towerAfter), details);
        }
    }

    public class Mod : MelonMod
    {
        public override void OnApplicationStart()
        {
            HarmonyInstance.Create("BowDown097.Cave Monkey").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Game), "GetVersionString")]
    public class GameModel_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Game __instance)
        {
            PowerModel caveModel = __instance.model.powers.FirstOrDefault(power => power.name == "CaveMonkey");
            if (caveModel.tower.cost == 170)
            {
                return;
            }

            caveModel.tower.cost = 170;
            caveModel.tower.towerSet = "Primary";
        }
    }

    [HarmonyPatch(typeof(TowerInventory), "Init")]
    public class TowerInit_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(TowerInventory __instance, ref List<TowerDetailsModel> allTowersInTheGame)
        {
            if (allTowersInTheGame.ToArray().Any(tower => tower.name.Contains("CaveMonkey")))
            {
                return true;
            }

            ShopTowerDetailsModel caveDetails = new ShopTowerDetailsModel("CaveMonkey", 8, 0, 0, 0, -1, null);
            Utils.registerTowerInInventory(caveDetails, "BoomerangMonkey", allTowersInTheGame);

            return true;
        }
    }
}