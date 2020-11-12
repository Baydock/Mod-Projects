using Assets.Scripts.Models;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Bloons.Behaviors;
using Assets.Scripts.Models.Effects;
using Assets.Scripts.Models.Towers.Mods;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.UI_New.InGame.BloonMenu;
using Harmony;
using Il2CppSystem.Collections.Generic;
using NKHook6;
using MelonLoader;
using System.Linq;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Models.Rounds;
using Assets.Scripts.Data.Rounds;
using Assets.Scripts.Models.GenericBehaviors;
using UnityEngine;

namespace AddBloon
{
    public class Utils
    {
    }

    public class Mod : MelonMod
    {
        public override void OnApplicationStart()
        {
            HarmonyInstance.Create("Baydock.Add bloon").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Game), "GetVersionString")]
    public class GameModel_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Game __instance)
        {
            if (__instance.model.bloonsByName.ContainsKey("YouNeedAMib"))
                return;

            DamageTypeImunityModel[] dti = new DamageTypeImunityModel[] { new DamageTypeImunityModel("DamageTypeImunityModel_default_imunity",
                new string[] { "Sharp", "Shatter", "Explosion", "Cold", "Fire", "Energy", "Plasma" },
                new string[0], true, 0, 0) };

            BloonModel Red = __instance.model.bloons.ToArray().FirstOrDefault(bm => bm.id == "Red");
            BloonModel youNeedAMib = new BloonModel("YouNeedAMib", "YouNeedAMib", 30, 8, Red.display,
                   new DamageStateModel[0], Red.icon, false,
                   new Model[] {
                       Red.behaviors.FirstOrDefault(m => m.name == "PopEffectModel_"),
                       dti[0],
                       Red.behaviors.FirstOrDefault(m => m.name == "DistributeCashModel_"),
                       Red.behaviors.FirstOrDefault(m => m.name == "DisplayModel_BloonDisplay"),
                       new SpawnChildrenModel("SpawnChildrenModel_",
                       new string[] { "Lead", "Zebra", "Purple" })
                   },
                   "YouNeedAMib", new string[] { "YouNeedAMib", "NA", "Ice" }, null, null, 9, true, 9, false, false, false,
                   new EffectModel[0], false, false, dti, 1, 1, true);

            __instance.model.bloonsByName.Add("YouNeedAMib", youNeedAMib);

            foreach (RoundSetModel rs in __instance.model.roundSets)
                foreach (RoundModel r in rs.rounds)
                    foreach (BloonGroupModel bg in r.groups)
                        if (bg.bloon == "Red")
                            bg.bloon = "YouNeedAMib";
        }
    }
}