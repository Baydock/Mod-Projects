using Assets.Scripts.Models;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Models.Profile;
using Assets.Scripts.Models.Towers;
using Assets.Scripts.Models.Towers.Behaviors.Abilities;
using Assets.Scripts.Models.Towers.Behaviors.Attack;
using Assets.Scripts.Models.Towers.Projectiles;
using Assets.Scripts.Models.Towers.Upgrades;
using Assets.Scripts.Models.Towers.Weapons;
using Assets.Scripts.Models.TowerSets;
using Assets.Scripts.Simulation.Bloons;
using Assets.Scripts.Simulation.Factory;
using Assets.Scripts.Simulation.Input;
using Assets.Scripts.Simulation.Objects;
using Assets.Scripts.Simulation.Towers;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Display;
using Assets.Scripts.Unity.Localization;
using Assets.Scripts.Unity.UI_New.InGame.RightMenu;
using Assets.Scripts.Unity.UI_New.InGame.StoreMenu;
using Assets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Assets.Scripts.Utils;
using Harmony;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Runtime.InteropServices;
using MelonLoader;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace MonkeyCasino {

    public class MonkeyCasinoMod : MelonMod { }

    [HarmonyPatch(typeof(ProfileModel), "Validate")] // this method is called after the profile data is parsed, hence why it's used to modify said profile data
    public class ProfileModel_Patch {
        public static bool AddIfNotContained(List<string> list, string s) {
            if (!list.Contains(s)) {
                list.Add(s);
                return true;
            }
            return false;
        }

        [HarmonyPostfix]
        public static void Postfix(ProfileModel __instance) {
            List<string> unlockedTowers = __instance.unlockedTowers;

            if (AddIfNotContained(unlockedTowers, MonkeyCasino.BaseName))
                MelonLogger.Log($"Unlocked {MonkeyCasino.DisplayName}");
        }
    }

    [HarmonyPatch(typeof(Game), "GetVersionString")] // this method is called soon after the game is done initializing the models, hence why it's used to modify said models
    public class GameModel_Patch {
        [HarmonyPostfix]
        public static void Postfix() {
            if (Game.instance.model.towers.ToArray().Any(tower => MonkeyCasino.IsMonkeyCasino(tower)))
                return;

            Utils.AddTowerModel(MonkeyCasino.Casino());
            Utils.AddTextChange(MonkeyCasino.BaseName, MonkeyCasino.DisplayName);

            MelonLogger.Log($"Made {MonkeyCasino.DisplayName}");
        }
    }

    [HarmonyPatch(typeof(TowerInventory), "Init")] // this method tells the game to create buttons for a given list of towers, allTowersInTheGame, which we modify here
    public class TowerInventoryInit_Patch {
        public static List<TowerDetailsModel> towerList;
        [HarmonyPrefix]
        public static bool Prefix(ref List<TowerDetailsModel> allTowersInTheGame) {
            if (allTowersInTheGame.ToArray().Any(tower => MonkeyCasino.IsMonkeyCasino(tower)) /*||
                !allTowersInTheGame.ToArray().Any(tower => tower.name.Contains("Sniper"))*/)
                return true;

            ShopTowerDetailsModel newTowerDetails =
                new ShopTowerDetailsModel(MonkeyCasino.BaseName, -1, 0, 0, 0, -1, null);
            Utils.RegisterTowerInInventory(newTowerDetails, MonkeyCasino.InsertBefore, allTowersInTheGame);

            towerList = allTowersInTheGame;

            return true;
        }
    }

    [HarmonyPatch(typeof(ResourceLoader), "LoadSpriteFromSpriteReferenceAsync")]
    public class ResourceLoader_Patch {
        [HarmonyPostfix]
        public static void Postfix(SpriteReference reference, Image image) {
            if (reference != null)
                Utils.SetTexture(image, reference.GUID);
        }
    }

    [HarmonyPatch(typeof(Factory), "FindAndSetupPrototype")]
    public class UnityDisplayNodeFactory_Patch {
        [HarmonyPrefix]
        public static bool Prefix(Factory __instance, ref UnityDisplayNode __result, string objectId, bool cache) {
            if (objectId.Contains("Monkey Casino Skin") && !__instance.prototypes.ContainsKey(objectId)) {
                __result = __instance.FindAndSetupPrototype(Utils.GetTower(TowerType.BananaFarm, 0, 5, 0).display, false);
                __result.name = "Monkey Casino Skin";
                foreach (Renderer renderer in __result.genericRenderers)
                    if (Il2CppType.Of<SkinnedMeshRenderer>().IsAssignableFrom(renderer.GetIl2CppType()))
                        Utils.SetTexture(renderer.Cast<SkinnedMeshRenderer>(), objectId);
                if (cache)
                    __instance.prototypes.Add(objectId, __result);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(UpgradeObject), "OnUpgrade")]
    public class UpgradeClick_Patch {
        [HarmonyPostfix]
        public static void Postfix(UpgradeObject __instance) {
            if(MonkeyCasino.IsMonkeyCasino(__instance.tts)) {
                
            }
        }
    }

    [HarmonyPatch(typeof(TowerSelectionMenu), "SelectTower")]
    public class TowerSelect_Patch {
        public static void ShowUpgrades(TowerSelectionMenu tsm, TowerToSimulation tts) {
            if (MonkeyCasino.IsMonkeyCasino(tts)) {
                tsm.upgradeButtons[2].gameObject.SetActive(false);
                foreach (UpgradeObject uo in tsm.upgradeButtons) {
                    uo.restricted.SetActive(false);
                    uo.locked.SetActive(false);
                }
                tsm.scalar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1800);

                Utils.SetTexture(tsm.upgradeButtons[0].upgradeButton.background, "Gamble Money");
                tsm.upgradeButtons[0].upgradeButton.cost.SetText($"${MonkeyCasino.bettedCash}");
                Utils.SetTexture(tsm.upgradeButtons[1].upgradeButton.background, "Gamble Lives");
                tsm.upgradeButtons[1].upgradeButton.cost.SetText($"♥{MonkeyCasino.bettedLives}");
                for (int i = 0; i < 2; i++) {
                    tsm.upgradeButtons[i].tiers[0].transform.parent.gameObject.SetActive(false);
                    tsm.upgradeButtons[i].upgradeButton.lockedText.enabled = false;
                    tsm.upgradeButtons[i].upgradeButton.cost.enabled = true;
                }
            } else {
                if (tts.Def.isSubTower || tts.Def.dontDisplayUpgrades)
                    tsm.scalar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1100);
                else {
                    tsm.upgradeButtons[2].gameObject.SetActive(true);
                    tsm.scalar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 2150);
                }

                for (int i = 0; i < 2; i++)
                    tsm.upgradeButtons[i].tiers[0].transform.parent.gameObject.SetActive(true);
            }
        }
        [HarmonyPostfix]
        public static void Postfix(TowerSelectionMenu __instance, TowerToSimulation tower) => ShowUpgrades(__instance, tower);
    }
    [HarmonyPatch(typeof(TowerSelectionMenu), "UpgradeTower", new Type[] { typeof(UpgradeModel), typeof(int), typeof(float) })]
    public class TowerUpgrade_Patch {
        [HarmonyPostfix]
        public static void Postfix(TowerSelectionMenu __instance) =>
            TowerSelect_Patch.ShowUpgrades(__instance, __instance.selectedTower);
    }

    public static class Utils {
        public static Texture2D GetTexture(string name) {
            object bitmap = Icons.ResourceManager.GetObject(name);
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
        public static void SetTexture(Image image, string name) {
            Texture2D texture = GetTexture(name);
            if (texture != null) {
                image.canvasRenderer.SetTexture(texture);
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(), 5.4f);
                image.color = new UnityEngine.Color(1, 1, 1, 1);
            }
        }
        public static void SetTexture(SkinnedMeshRenderer skin, string name) {
            Texture2D texture = GetTexture(name);
            if (texture != null)
                skin.material.mainTexture = texture;
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
        public static T Clone<T>(Model model) where T : Model => model.Clone().Cast<T>();
        public static T[] Clone<T>(Il2CppArrayBase<T> array) where T : Model => Helpers.CloneArray(array);
        public static B[] AddModel<B, T>(Il2CppArrayBase<B> array, T item, int index = 0) where B : Model where T : B =>
            array.Take(index).Append(item).Concat(array.Skip(index)).ToArray();
        public static B[] RemoveModel<B, T>(Il2CppArrayBase<B> array, T item) where B : Model where T : B =>
            array.Where(i => i.name != item.name).ToArray();

        public static void AddTowerModel(TowerModel towerModel) =>
            Game.instance.model.towers = AddModel(Game.instance.model.towers, towerModel);
        public static void AddUpgradeModel(UpgradeModel upgradeModel) =>
            Game.instance.model.upgrades = AddModel(Game.instance.model.upgrades, upgradeModel);

        public static TowerModel GetTower(string t, int tier1, int tier2, int tier3) =>
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
        public static void AddBehaviorModel(WeaponModel weaponModel, WeaponBehaviorModel behavior, int index = 0) =>
            weaponModel.behaviors = AddModel(weaponModel.behaviors, behavior, index);
        public static void AddBehaviorModel(ProjectileModel projectileModel, Model behavior, int index = 0) =>
            projectileModel.behaviors = AddModel(projectileModel.behaviors, behavior, index);
        public static void RemoveBehaviorModel(TowerModel towerModel, string partialName, int number = 0) =>
            RemoveBehaviorModel(towerModel, GetBehaviorModel(towerModel, partialName, number));
        public static void RemoveBehaviorModel(AttackModel attackModel, string partialName, int number = 0) =>
            RemoveBehaviorModel(attackModel, GetBehaviorModel(attackModel, partialName, number));
        public static void RemoveBehaviorModel(AbilityModel abilityModel, string partialName, int number = 0) =>
            RemoveBehaviorModel(abilityModel, GetBehaviorModel(abilityModel, partialName, number));
        public static void RemoveBehaviorModel(WeaponModel weaponModel, string partialName, int number = 0) =>
            RemoveBehaviorModel(weaponModel, GetBehaviorModel<WeaponBehaviorModel>(weaponModel, partialName, number));
        public static void RemoveBehaviorModel(ProjectileModel projectileModel, string partialName, int number = 0) =>
            RemoveBehaviorModel(projectileModel, GetBehaviorModel(projectileModel, partialName, number));
        public static void RemoveBehaviorModel(TowerModel towerModel, Model behavior) =>
            towerModel.behaviors = RemoveModel(towerModel.behaviors, behavior);
        public static void RemoveBehaviorModel(AttackModel attackModel, Model behavior) =>
            attackModel.behaviors = RemoveModel(attackModel.behaviors, behavior);
        public static void RemoveBehaviorModel(AbilityModel abilityModel, Model behavior) =>
            abilityModel.behaviors = RemoveModel(abilityModel.behaviors, behavior);
        public static void RemoveBehaviorModel(WeaponModel weaponModel, WeaponBehaviorModel behavior) =>
            weaponModel.behaviors = RemoveModel(weaponModel.behaviors, behavior);
        public static void RemoveBehaviorModel(ProjectileModel projectileModel, Model behavior) =>
            projectileModel.behaviors = RemoveModel(projectileModel.behaviors, behavior);

        public static B GetBehavior<B>(SizedList<B> bs, string partialName, int number = 0) where B : RootBehavior =>
            bs.GetBackingList().ToArray().Where(b => b.model.name.Contains(partialName)).ElementAtOrDefault(number);

        public static bool[] GetUsedPaths(int tier1, int tier2, int tier3) => new bool[] { tier1 > 0, tier2 > 0, tier3 > 0 };
        public static bool[] GetUsedPaths(int[] tiers) => GetUsedPaths(tiers[0], tiers[1], tiers[2]);
        public static int GetPathsUsed(int tier1, int tier2, int tier3) => GetUsedPaths(tier1, tier2, tier3).Count(p => p);
        public static int GetPathsUsed(int[] tiers) => GetPathsUsed(tiers[0], tiers[1], tiers[2]);
        public static int GetTier(int tier1, int tier2, int tier3) => new int[] { tier1, tier2, tier3 }.Max();
        public static int GetTier(int[] tiers) => GetTier(tiers[0], tiers[1], tiers[2]);
    }

    public static class MonkeyCasino {
        public static string BaseName { get; } = "MonkeyCasino";
        public static string DisplayName { get; } = "Monkey Casino";
        public static string InsertBefore { get; } = TowerType.SpikeFactory;
        public static string InsertAfter { get; } = TowerType.BananaFarm;
        public static string CashGambleName { get; } = $"{BaseName}-CashGamble";
        public static string LifeGambleName { get; } = $"{BaseName}-LifeGamble";
        public static string CashGambleDisplayName { get; } = "Cash Gamble";
        public static string LifeGambleDisplayName { get; } = "Life Gamble";
        public static string CashGambleDescription { get; } = "Spend some cash for the chance to win more";
        public static string LifeGambleDescription { get; } = "Spend some lives for the chance to win more";

        public static int bettedLives { get; set; } = 10;
        public static int bettedCash { get; set; } = 10;

        public static bool IsMonkeyCasino(string name) => name.Contains(BaseName);
        public static bool IsMonkeyCasino(Model model) => IsMonkeyCasino(model.name);
        public static bool IsMonkeyCasino(Tower tower) => IsMonkeyCasino(tower.towerModel);
        public static bool IsMonkeyCasino(TowerToSimulation tts) => IsMonkeyCasino(tts.Def);

        public static TowerModel Casino() {
            TowerModel bana = Utils.GetTower(TowerType.BananaFarm, 0, 5, 0);
            TowerModel bbana = Utils.GetTower(TowerType.BananaFarm, 0, 0, 0);

            TowerModel casino = Utils.Clone(bana);
            casino.name = BaseName;
            casino.baseId = BaseName;
            casino.appliedUpgrades = new string[0];
            casino.towerSet = "Support";
            casino.cost = 1000;
            casino.tier = 0;
            casino.tiers = new int[] { 0, 0, 0 };
            casino.upgrades = new UpgradePathModel[] { };
            casino.portrait = new SpriteReference { guidRef = "Monkey Casino" };
            casino.icon = new SpriteReference { guidRef = "Monkey Casino Icon" };
            casino.behaviors = Utils.Clone(casino.behaviors);
            int displayIndex = Utils.GetBehaviorModelIndex(casino, "DisplayModel");
            casino.behaviors[displayIndex] = Utils.Clone(casino.behaviors[displayIndex]);
            casino.behaviors[displayIndex].Cast<DisplayModel>().display = casino.display = "Monkey Casino Skin";
            int attackIndex = Utils.GetBehaviorModelIndex(casino, "Attack");
            casino.behaviors[attackIndex] = Utils.Clone(casino.behaviors[attackIndex]);
            casino.behaviors[attackIndex].Cast<AttackModel>().weapons = new WeaponModel[0];

            Utils.AddBehaviorModel(casino, Utils.GetBehaviorModel(bbana, "Animation"));

            Utils.RemoveBehaviorModel(casino, "Bank");
            Utils.RemoveBehaviorModel(casino, "Ability");

            return casino;
        }
    }
}