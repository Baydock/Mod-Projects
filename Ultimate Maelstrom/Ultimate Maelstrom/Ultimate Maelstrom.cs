using Assets.Scripts.Models;
using Assets.Scripts.Models.Towers;
using Assets.Scripts.Models.Towers.Behaviors.Abilities;
using Assets.Scripts.Models.Towers.Behaviors.Attack;
using Assets.Scripts.Models.Towers.Projectiles;
using Assets.Scripts.Models.Towers.Upgrades;
using Assets.Scripts.Models.Towers.Weapons;
using Assets.Scripts.Models.TowerSets;
using Assets.Scripts.Simulation.Input;
using Assets.Scripts.Simulation.Objects;
using Assets.Scripts.Simulation.Towers;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Display;
using Assets.Scripts.Unity.Localization;
using Assets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Assets.Scripts.Utils;
using Harmony;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;

namespace UltimateMaelstrom {

    public class UltimateMaelstromMod : MelonMod { }

    [HarmonyPatch(typeof(Game), "GetVersionString")]
    public class Game_Patch {
        public static UpgradeModel GetUlimateMaelstromUpgrade() {
            UpgradeModel bomb = Game.instance.model.upgradesByName["Faster Reload"];

            return new UpgradeModel("Ultimate Maelstrom", 30000, 0, bomb.icon, 1, 5, 0, "", "");
        }
        public static TowerModel GetUltimateMaelstrom(int t1, int t3) {
            TowerModel SuperMaelstrom = Utils.GetTower(TowerType.TackShooter, t1, 5, t3);
            TowerModel UltimateMaelstrom = Utils.Clone(SuperMaelstrom);

            UltimateMaelstrom.name = $"TackShooter-{t1}6{t3}";
            UltimateMaelstrom.tier = 5;
            UltimateMaelstrom.tiers = new int[] { t1, 5, t3 };

            SuperMaelstrom.upgrades = SuperMaelstrom.upgrades.Add(
                new UpgradePathModel("Ultimate Maelstrom", UltimateMaelstrom.name, t1 + t3 > 0 ? 2 : 1, 6)).ToArray();
            SuperMaelstrom.tier = 4;
            SuperMaelstrom.tiers[1] = 4;

            return UltimateMaelstrom;
        }
        [HarmonyPostfix]
        public static void Postfix() {
            Utils.AddTowerModel(GetUltimateMaelstrom(0, 0));
            for (int t = 1; t < 3; t++) {
                Utils.AddTowerModel(GetUltimateMaelstrom(t, 0));
                Utils.AddTowerModel(GetUltimateMaelstrom(0, t));
            }
            Utils.AddUpgradeModel(GetUlimateMaelstromUpgrade());
            Game.instance.model.upgradesByName["Super Maelstrom"].tier = 4;
        }
    }

    /*[HarmonyPatch(typeof(TowerInventory), "Init")] // this method tells the game to create buttons for a given list of towers, allTowersInTheGame, which we modify here
    public class TowerInventoryInit_Patch {
        [HarmonyPrefix]
        public static bool Prefix(ref List<TowerDetailsModel> allTowersInTheGame) {
            TowerDetailsModel tdm = allTowersInTheGame.ToArray().FirstOrDefault(tdm => tdm.towerId.Contains("TackShooter"));
            if (tdm != null)
                tdm.Cast<ShopTowerDetailsModel>().pathTwoMax = 6;

            return true;
        }
    }*/

    /*[HarmonyPatch(typeof(UpgradeObject), "GetUpgrade")]
    public class Allow6thTier_Patch {
        [HarmonyPostfix]
        public static void Postfix(ref UpgradeModel __result, TowerModel tm) {
            MelonLogger.Log(tm.name);
            if(tm.name.Contains("TackShooter") && tm.tiers[1] == 5) {

            }
        }
    }*/

    /*[HarmonyPatch(typeof(UpgradeObject), "OnUpgrade")]
    public class OnUpgrade_Patch {
        [HarmonyPostfix]
        public static void Postfix(UpgradeObject __instance) {
            if(__instance.tts.Def.name.Contains("TackShooter") && __instance.tts.Def.tiers[1] == 5 && __instance.path == 1) {
                __instance.upgradeButton.SetSpecialLocksNotActive();
                __instance.upgradeButton.SetUpgradeModel(Game_Patch.GetUlimateMaelstromUpgrade());
            }
        }
    }*/

    [HarmonyPatch(typeof(Factory), "FindAndSetupPrototype")]
    public class UnityDisplayNodeFactory_Patch {
        [HarmonyPrefix]
        public static bool Prefix(Factory __instance, ref UnityDisplayNode __result, string objectId, bool cache) {
            MelonLogger.Log(objectId);
            return true;
        }
    }

    public class Utils {
        public static Texture2D GetTexture(Bitmap bitmap) {
            MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            Texture2D texture = new Texture2D(0, 0);
            ImageConversion.LoadImage(texture, memory.ToArray());
            return texture;
        }
        public static Sprite GetSprite(Bitmap bitmap) => GetSprite(GetTexture(bitmap));
        public static Sprite GetSprite(Texture2D texture) {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
            return sprite;
        }

        public static void RegisterTowerInInventory(TowerDetailsModel details, string insertBefore, List<TowerDetailsModel> allTowersInTheGame) {
            // get the tower details for the tower insertBefore and insert the new tower into the index towerBefore is at, shifting everything after it by 1
            TowerDetailsModel towerAfter = allTowersInTheGame.ToArray().FirstOrDefault(tower => tower.towerId == insertBefore);
            details.towerIndex = towerAfter.towerIndex;
            int index = allTowersInTheGame.IndexOf(towerAfter);
            allTowersInTheGame.Insert(index, details);
            foreach (TowerDetailsModel tdm in allTowersInTheGame.ToArray().Skip(index + 1))
                tdm.towerIndex++;
        }
        public static void AddTextChange(string key, string value) => LocalizationManager.instance.textTable.Add(key, value);

        public static T Clone<T>(T model) where T : Model => model.Clone().Cast<T>();
        public static T[] Clone<T>(Il2CppArrayBase<T> array) where T : Model => Helpers.CloneArray(array);
        public static B[] AddModel<B, T>(Il2CppArrayBase<B> array, T item, int index = 0) where B : Model where T : B =>
            array.Take(index).Append(item).Concat(array.Skip(index)).ToArray();
        public static B[] RemoveModel<B, T>(Il2CppArrayBase<B> array, T item) where B : Model where T : B =>
            array.Where(i => i.name != item.name).ToArray();

        public static void AddTowerModel(TowerModel towerModel) =>
            Game.instance.model.towers = AddModel(Game.instance.model.towers, towerModel);
        public static void AddUpgradeModel(UpgradeModel upgradeModel) =>
            Game.instance.model.upgrades = AddModel(Game.instance.model.upgrades, upgradeModel);

        public static TowerModel GetTower(string t, int tier1 = 0, int tier2 = 0, int tier3 = 0) =>
            Game.instance.model.GetTower(t, tier1, tier2, tier3);

        public static Model GetModel(Model[] bs, string partialName, int number = 0) =>
            bs.Where(b => b.name.Contains(partialName)).ElementAtOrDefault(number);
        public static Model GetBehaviorModel(TowerModel tower, string partialName, int number = 0) =>
            GetModel(tower.behaviors, partialName, number);
        public static Model GetBehaviorModel(AttackModel attack, string partialName, int number = 0) =>
            GetModel(attack.behaviors, partialName, number);
        public static Model GetBehaviorModel(AbilityModel ability, string partialName, int number = 0) =>
            GetModel(ability.behaviors, partialName, number);
        public static Model GetBehaviorModel(WeaponModel weapon, string partialName, int number = 0) =>
            GetModel(weapon.behaviors, partialName, number);
        public static Model GetBehaviorModel(ProjectileModel projectile, string partialName, int number = 0) =>
            GetModel(projectile.behaviors, partialName, number);
        public static T GetModel<T>(Model[] bs, string partialName, int number = 0) where T : Model =>
            GetModel(bs, partialName, number).Cast<T>();
        public static T GetBehaviorModel<T>(TowerModel tower, string partialName, int number = 0) where T : Model =>
            GetModel<T>(tower.behaviors, partialName, number);
        public static T GetBehaviorModel<T>(AttackModel attack, string partialName, int number = 0) where T : Model =>
            GetModel<T>(attack.behaviors, partialName, number);
        public static T GetBehaviorModel<T>(AbilityModel ability, string partialName, int number = 0) where T : Model =>
            GetModel<T>(ability.behaviors, partialName, number);
        public static T GetBehaviorModel<T>(WeaponModel weapon, string partialName, int number = 0) where T : Model =>
            GetModel<T>(weapon.behaviors, partialName, number);
        public static T GetBehaviorModel<T>(ProjectileModel projectile, string partialName, int number = 0) where T : Model =>
            GetModel<T>(projectile.behaviors, partialName, number);
        public static int GetModelIndex(Model[] bs, string partialName, int number = 0) {
            for (int i = 0, n = 0; i < bs.Length; i++)
                if (bs[i].name.Contains(partialName)) {
                    if (n == number) return i;
                    else n++;
                }
            return -1;
        }
        public static int GetBehaviorModelIndex(TowerModel tower, string partialName, int number = 0) =>
            GetModelIndex(tower.behaviors, partialName, number);
        public static int GetBehaviorModelIndex(AttackModel attack, string partialName, int number = 0) =>
            GetModelIndex(attack.behaviors, partialName, number);
        public static int GetBehaviorModelIndex(AbilityModel ability, string partialName, int number = 0) =>
            GetModelIndex(ability.behaviors, partialName, number);
        public static int GetBehaviorModelIndex(WeaponModel weapon, string partialName, int number = 0) =>
            GetModelIndex(weapon.behaviors, partialName, number);
        public static int GetBehaviorModelIndex(ProjectileModel projectile, string partialName, int number = 0) =>
            GetModelIndex(projectile.behaviors, partialName, number);
        public static AttackModel GetAttackModel(Model[] bs, int attackNumber = 0) {
            AttackModel ground = GetModel<AttackModel>(bs, "AttackModel", attackNumber);
            if (ground == null) {
                AttackAirUnitModel air = GetModel<AttackAirUnitModel>(bs, "AttackAirUnitModel", attackNumber);
                return air;
            }
            return ground;
        }
        public static AttackModel GetAttackModel(TowerModel tower, int attackNumber = 0) =>
            GetAttackModel(tower.behaviors, attackNumber);
        public static int GetAttackModelIndex(Model[] bs, int attackNumber = 0) {
            int ground = GetModelIndex(bs, "AttackModel", attackNumber);
            if (ground == -1) {
                int air = GetModelIndex(bs, "AttackAirUnitModel", attackNumber);
                return air;
            }
            return ground;
        }
        public static int GetAttackModelIndex(TowerModel tower, int attackNumber = 0) =>
            GetAttackModelIndex(tower.behaviors, attackNumber);
        public static AbilityModel GetAbilityModel(Model[] bs, int abilityNumber = 0) =>
            GetModel<AbilityModel>(bs, "AbilityModel", abilityNumber);
        public static AbilityModel GetAbilityModel(TowerModel tower, int abilityNumber = 0) =>
            GetAbilityModel(tower.behaviors, abilityNumber);
        public static int GetAbilityModelIndex(Model[] bs, int abilityNumber = 0) =>
            GetModelIndex(bs, "AbilityModel", abilityNumber);
        public static int GetAbilityModelIndex(TowerModel tower, int abilityNumber = 0) =>
            GetAbilityModelIndex(tower.behaviors, abilityNumber);
        public static WeaponModel GetWeaponModel(Model[] bs, int attackNumber = 0, int index = 0) =>
            GetAttackModel(bs, attackNumber).weapons[index];
        public static WeaponModel GetWeaponModel(TowerModel tower, int attackNumber = 0, int index = 0) =>
            GetWeaponModel(tower.behaviors, attackNumber, index);
        public static ProjectileModel GetProjectileModel(Model[] bs, int attackNumber = 0, int index = 0) =>
            GetWeaponModel(bs, attackNumber, index).projectile;
        public static ProjectileModel GetProjectileModel(TowerModel tower, int attackNumber = 0, int index = 0) =>
            GetProjectileModel(tower.behaviors, attackNumber, index);

        public static void AddBehaviorModel(TowerModel towerModel, Model behavior, int index = 0) =>
            towerModel.behaviors = AddModel(towerModel.behaviors, behavior, index);
        public static void AddBehaviorModel(AttackModel attackModel, Model behavior, int index = 0) =>
            attackModel.behaviors = AddModel(attackModel.behaviors, behavior, index);
        public static void AddBehaviorModel(AbilityModel abilityModel, Model behavior, int index = 0) =>
            abilityModel.behaviors = AddModel(abilityModel.behaviors, behavior, index);
        public static WeaponBehaviorModel[] AddBehaviorModel(WeaponModel weaponModel, WeaponBehaviorModel behavior, int index = 0) =>
            weaponModel.behaviors = AddModel(weaponModel.behaviors, behavior, index);
        public static Model[] AddBehaviorModel(ProjectileModel projectileModel, Model behavior, int index = 0) =>
            projectileModel.behaviors = AddModel(projectileModel.behaviors, behavior, index);
        public static void RemoveBehaviorModel(TowerModel towerModel, Model behavior) =>
            towerModel.behaviors = RemoveModel(towerModel.behaviors, behavior);
        public static void RemoveBehaviorModel(AttackModel attackModel, Model behavior) =>
            attackModel.behaviors = RemoveModel(attackModel.behaviors, behavior);
        public static Model[] RemoveBehaviorModel(AbilityModel abilityModel, Model behavior) =>
            abilityModel.behaviors = RemoveModel(abilityModel.behaviors, behavior);
        public static WeaponBehaviorModel[] RemoveBehaviorModel(WeaponModel weaponModel, WeaponBehaviorModel behavior) =>
            weaponModel.behaviors = RemoveModel(weaponModel.behaviors, behavior);
        public static Model[] RemoveBehaviorModel(ProjectileModel projectileModel, Model behavior) =>
            projectileModel.behaviors = RemoveModel(projectileModel.behaviors, behavior);

        public static B GetBehavior<B>(SizedList<B> bs, string partialName, int number = 0) where B : RootBehavior =>
            bs.GetBackingList().ToArray().Where(b => {
                if (b?.model?.name == null) return false;
                return b.model.name.Contains(partialName);
            }).ElementAtOrDefault(number);

        public static bool[] GetUsedPaths(int tier1, int tier2, int tier3) => new bool[] { tier1 > 0, tier2 > 0, tier3 > 0 };
        public static int GetPathsUsed(int tier1, int tier2, int tier3) => GetUsedPaths(tier1, tier2, tier3).Count(p => p);
        public static int GetTier(int tier1, int tier2, int tier3) => new int[] { tier1, tier2, tier3 }.Max();
    }
}