using Assets.Scripts.Models;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Bloons.Behaviors;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Models.Rounds;
using Assets.Scripts.Models.Towers.Projectiles.Behaviors;
using Assets.Scripts.Simulation.Towers.Weapons.Behaviors;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Display;
using Assets.Scripts.Utils;
using Harmony;
using MelonLoader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace BonusBloon {

    public class BonusBloonMod : MelonMod { }

    [HarmonyPatch(typeof(Game), "GetVersionString")] // this method is called soon after the game is done initializing the models, hence why it's used to modify said models
    public class GameModel_Patch {
        [HarmonyPostfix]
        public static void Postfix() {
            Utils.AddBloonModel(BonusBloon.Get());
        }
    }

    [HarmonyPatch(typeof(UnityToSimulation), "StartRound")]
    public class StartRound_Patch {
        [HarmonyPostfix]
        public static void Postfix(UnityToSimulation __instance) {
            if (Random.Range(1, 10) == 9) {
                __instance.SpawnBloons(new BloonEmissionModel[] {
                    new BloonEmissionModel("BloonEmissionModel_", 0, "BonusBloon")
                }, __instance.GetCurrentRound(), 0);
            }
        }
    }

    [HarmonyPatch(typeof(Factory), "FindAndSetupPrototype")]
    public class UnityDisplayNodeFactory_Patch {
        [HarmonyPrefix]
        public static bool Prefix(Factory __instance, ref UnityDisplayNode __result, string objectId, bool cache) {
            if (!__instance.prototypes.ContainsKey(objectId)) {
                if (objectId.Contains("BonusBloon")) {
                    __result = __instance.FindAndSetupPrototype("a8c2a58144bb3aa468e4ba0a222b8107", false);
                    __result.name = objectId;
                    __result.isSprite = true;
                    foreach (Renderer renderer in __result.genericRenderers)
                        if (Il2CppType.Of<SpriteRenderer>().IsAssignableFrom(renderer.GetIl2CppType()))
                            Utils.SetTexture(renderer.Cast<SpriteRenderer>(), objectId, new Vector2(.5f, .5f));
                    if (cache)
                        __instance.prototypes.Add(objectId, __result);
                    return false;
                }
            }
            return true;
        }
    }

    public static class Utils {
        public static Texture2D GetTexture(string name) {
            object bitmap = Textures.ResourceManager.GetObject(name);
            if (bitmap != null) {
                MemoryStream memory = new MemoryStream();
                (bitmap as Bitmap).Save(memory, ImageFormat.Png);
                Texture2D texture = new Texture2D(0, 0);
                ImageConversion.LoadImage(texture, memory.ToArray());
                memory.Close();
                return texture;
            }
            return null;
        }
        public static void SetTexture(SpriteRenderer sprite, string name, Vector2 pivot = new Vector2()) {
            Texture2D texture = GetTexture(name);
            if (texture != null)
                sprite.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 5.4f);
        }

        public static T Clone<T>(T model) where T : Model => model.Clone().Cast<T>();
        public static T Clone<T>(Model model) where T : Model => model.Clone().Cast<T>();
        public static T[] Clone<T>(Il2CppArrayBase<T> array) where T : Model => Helpers.CloneArray(array);
        public static Model Clone<T>(Il2CppArrayBase<T> array, int index) where T : Model => array[index] = Clone(array[index]);

        public static void AddBloonModel(BloonModel bloonModel) =>
            Game.instance.model.bloons = AddModel(Game.instance.model.bloons, bloonModel);

        public static BloonModel GetBloon(string b) =>
            Game.instance.model.GetBloon(b);

        public static Model GetModel(Model[] bs, string partialName, int number = 0) =>
            bs.Where(b => b.name.Contains(partialName)).ElementAtOrDefault(number);
        public static Model GetBehaviorModel(BloonModel bloon, string partialName, int number = 0) =>
            GetModel(bloon.behaviors, partialName, number);
        public static T GetModel<T>(Model[] bs, string partialName, int number = 0) where T : Model =>
            GetModel(bs, partialName, number).Cast<T>();
        public static T GetBehaviorModel<T>(BloonModel bloon, string partialName, int number = 0) where T : Model =>
            GetModel<T>(bloon.behaviors, partialName, number);
        public static int GetModelIndex(Model[] bs, string partialName, int number = 0) {
            for (int i = 0, n = 0; i < bs.Length; i++)
                if (bs[i].name.Contains(partialName)) {
                    if (n == number) return i;
                    else n++;
                }
            return -1;
        }
        public static int GetBehaviorModelIndex(BloonModel bloon, string partialName, int number = 0) =>
            GetModelIndex(bloon.behaviors, partialName, number);
        public static SpawnChildrenModel GetSpawnChildrenModel(Model[] bs, int number = 0) =>
            GetModel<SpawnChildrenModel>(bs, "SpawnChildrenModel", number);
        public static SpawnChildrenModel GetSpawnChildrenModel(BloonModel bloon, int number = 0) =>
            GetSpawnChildrenModel(bloon.behaviors, number);
        public static int GetSpawnChildrenModelIndex(Model[] bs, int number = 0) =>
            GetModelIndex(bs, "SpawnChildrenModel", number);
        public static int GetSpawnChildrenModelIndex(BloonModel bloon, int number = 0) =>
            GetSpawnChildrenModelIndex(bloon.behaviors, number);
        public static DistributeCashModel GetDistributeCashModel(Model[] bs, int number = 0) =>
            GetModel<DistributeCashModel>(bs, "DistributeCashModel", number);
        public static DistributeCashModel GetDistributeCashModel(BloonModel bloon, int number = 0) =>
            GetDistributeCashModel(bloon.behaviors, number);
        public static int GetDistributeCashModelIndex(Model[] bs, int number = 0) =>
            GetModelIndex(bs, "DistributeCashModel", number);
        public static int GetDistributeCashModelIndex(BloonModel bloon, int number = 0) =>
            GetDistributeCashModelIndex(bloon.behaviors, number);

        public static B[] AddModel<B, T>(Il2CppArrayBase<B> array, T item, int index = 0) where B : Model where T : B =>
            array.Take(index).Append(item).Concat(array.Skip(index)).ToArray();
        public static void AddBehaviorModel(BloonModel bloonModel, Model behavior, int index = 0) =>
            bloonModel.behaviors = AddModel(bloonModel.behaviors, behavior, index);

        public static B[] RemoveModel<B, T>(Il2CppArrayBase<B> array, T item) where B : Model where T : B =>
            array.Where(i => !i.name.Equals(item.name)).ToArray();
        public static void RemoveBehaviorModel(BloonModel bloonModel, Model behavior) =>
            bloonModel.behaviors = RemoveModel(bloonModel.behaviors, behavior);
        public static void RemoveBehaviorModel(BloonModel bloonModel, string partialName, int number = 0) =>
            RemoveBehaviorModel(bloonModel, GetBehaviorModel(bloonModel, partialName, number));

        public static B[] RemoveModels<B>(Il2CppArrayBase<B> array, string partialName) where B : Model =>
            array.Where(i => !i.name.Contains(partialName)).ToArray();
        public static void RemoveBehaviorModels(BloonModel bloonModel, string partialName) =>
            bloonModel.behaviors = RemoveModels(bloonModel.behaviors, partialName);

        public static T CloneBehaviorModel<T>(BloonModel bloon, int index) where T : Model =>
            Clone(bloon.behaviors, index).Cast<T>();
        public static T CloneBehaviorModel<T>(BloonModel bloon, string partialName, int number = 0) where T : Model =>
            CloneBehaviorModel<T>(bloon, GetBehaviorModelIndex(bloon, partialName, number));
    }

    public static class BonusBloon {
        public static BloonModel Get() {
            BloonModel cerm = Utils.GetBloon("Ceramic");
            BloonModel lead = Utils.GetBloon("Lead");
            BloonModel pink = Utils.GetBloon("Pink");

            BloonModel bonusBloon = Utils.Clone(cerm);
            bonusBloon.baseId = "BonusBloon";
            bonusBloon.id = "BonusBloon";
            bonusBloon.name = "BonusBloon";
            bonusBloon.damageTypeImmunities = lead.damageTypeImmunities;
            bonusBloon.danger = 0;
            bonusBloon.distributeDamageToChildren = false;
            bonusBloon.isCamo = true;
            bonusBloon.layerNumber = 1;
            bonusBloon.leakDamage = 0;
            bonusBloon.maxHealth = 100;
            bonusBloon.speed = pink.speed;
            bonusBloon.tags = new string[] {
                "BonusBloon",
                "NA"
            };

            bonusBloon.behaviors = Utils.Clone(bonusBloon.behaviors);
            
            SpawnChildrenModel spawnChildren = Utils.CloneBehaviorModel<SpawnChildrenModel>(bonusBloon, "SpawnChildren");
            spawnChildren.children = new string[0];

            DistributeCashModel distributeCash = Utils.CloneBehaviorModel<DistributeCashModel>(bonusBloon, "DistributeCash");
            distributeCash.cash = 5000;

            Utils.RemoveBehaviorModel(bonusBloon, "CreateSoundOnDamageBloonModel");
            Utils.AddBehaviorModel(bonusBloon, Utils.GetBehaviorModel(lead, "CreateSoundOnDamageBloon"));

            DisplayModel display = Utils.CloneBehaviorModel<DisplayModel>(bonusBloon, "DisplayModel");
            display.display = bonusBloon.display = "BonusBloon";

            Utils.RemoveBehaviorModels(bonusBloon, "DamageStateModel");

            DamageStateModel d1 = new DamageStateModel("DamageStateModel_d1", "BonusBloonD1", .75f);
            Utils.AddBehaviorModel(bonusBloon, d1);

            DamageStateModel d2 = new DamageStateModel("DamageStateModel_d2", "BonusBloonD2", .50f);
            Utils.AddBehaviorModel(bonusBloon, d2);

            DamageStateModel d3 = new DamageStateModel("DamageStateModel_d3", "BonusBloonD3", .25f);
            Utils.AddBehaviorModel(bonusBloon, d3);

            bonusBloon.damageDisplayStates = new DamageStateModel[] { d3, d2, d1 }; //it has to be in reverse numerical order

            return bonusBloon;
        }
    }
}