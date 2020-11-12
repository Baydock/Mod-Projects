using Assets.Scripts.Models;
using Assets.Scripts.Models.Effects;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Models.Profile;
using Assets.Scripts.Models.Towers;
using Assets.Scripts.Models.Towers.Behaviors;
using Assets.Scripts.Models.Towers.Behaviors.Abilities;
using Assets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Assets.Scripts.Models.Towers.Behaviors.Attack;
using Assets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Assets.Scripts.Models.Towers.Behaviors.Emissions;
using Assets.Scripts.Models.Towers.Filters;
using Assets.Scripts.Models.Towers.Mods;
using Assets.Scripts.Models.Towers.Projectiles;
using Assets.Scripts.Models.Towers.Projectiles.Behaviors;
using Assets.Scripts.Models.Towers.Upgrades;
using Assets.Scripts.Models.Towers.Weapons;
using Assets.Scripts.Models.Towers.Weapons.Behaviors;
using Assets.Scripts.Models.TowerSets;
using Assets.Scripts.Simulation;
using Assets.Scripts.Simulation.Input;
using Assets.Scripts.Simulation.Objects;
using Assets.Scripts.Simulation.Towers;
using Assets.Scripts.Simulation.Towers.Behaviors;
using Assets.Scripts.Simulation.Towers.Behaviors.Abilities.Behaviors;
using Assets.Scripts.Simulation.Towers.Behaviors.Attack.Behaviors;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Display;
using Assets.Scripts.Unity.Localization;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Assets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu.TowerSelectionMenuThemes;
using Assets.Scripts.Unity.UI_New.Upgrade;
using Assets.Scripts.Utils;
using Harmony;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using AssetBundleDictionary = System.Collections.Generic.Dictionary<string, UnityEngine.AssetBundle>;
using Image = UnityEngine.UI.Image;
using InputManager = Assets.Scripts.Unity.UI_New.InGame.InputManager;
using TowerDictionary = System.Collections.Generic.Dictionary<string, Assets.Scripts.Models.Towers.TowerModel>;
using UpgradeDictionary = System.Collections.Generic.Dictionary<string, Assets.Scripts.Models.Towers.Upgrades.UpgradeModel>;
using Vector2 = Assets.Scripts.Simulation.SMath.Vector2;

namespace DartlingGun {

    public class DartlingGunMod : MelonMod { }

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
            bool added = false;
            List<string> unlockedTowers = __instance.unlockedTowers;
            List<string> aquiredUpgrades = __instance.acquiredUpgrades;

            added = AddIfNotContained(unlockedTowers, DartlingGun.BaseName) || added;

            for (int t = 1; t < 6; t++)
                for (int p = 0; p < 3; p++)
                    added = AddIfNotContained(aquiredUpgrades, DartlingGun.GetUpgradeNameFull(p, t)) || added;

            if (added)
                MelonLogger.Log($"Unlocked {DartlingGun.DisplayName}");
        }
    }

    [HarmonyPatch(typeof(Game), "GetVersionString")] // this method is called soon after the game is done initializing the models, hence why it's used to modify said models
    public class GameModel_Patch {
        [HarmonyPostfix]
        public static void Postfix() {
            if (Game.instance.model.towers.ToArray().Any(tower => DartlingGun.IsDartlingGun(tower)))
                return;

            for (int t1 = 0; t1 < 6; t1++)
                for (int t2 = 0; t2 < (t1 > 2 ? 3 : 6); t2++)
                    for (int t3 = 0; t3 < (t1 > 0 && t2 > 0 ? 1 : (t1 > 2 || t2 > 2 ? 3 : 6)); t3++)
                        DartlingGun.Add(t1, t2, t3);

            MelonLogger.Log($"Made {DartlingGun.DisplayName}");
        }
    }

    [HarmonyPatch(typeof(TowerInventory), "Init")] // this method tells the game to create buttons for a given list of towers, allTowersInTheGame, which we modify here
    public class TowerInventoryInit_Patch {
        public static List<TowerDetailsModel> towerList;
        [HarmonyPrefix]
        public static bool Prefix(ref List<TowerDetailsModel> allTowersInTheGame) {
            if (allTowersInTheGame.ToArray().Any(tower => DartlingGun.IsDartlingGun(tower)) /*||
                !allTowersInTheGame.ToArray().Any(tower => tower.name.Contains("Sniper"))*/)
                return true;

            ShopTowerDetailsModel newTowerDetails =
                new ShopTowerDetailsModel(DartlingGun.BaseName, -1, 5, 5, 5, -1, null);
            Utils.RegisterTowerInInventory(newTowerDetails, DartlingGun.InsertBefore, allTowersInTheGame);

            towerList = allTowersInTheGame;
            
            return true;
        }
    }

    [HarmonyPatch(typeof(UpgradeScreen), "UpdateUi")]
    public class UpgradeScreen_Patch {
        public static void DestroyButtons(Il2CppReferenceArray<UpgradeDetails> upgrades) {
            if (upgrades != null)
                for (int i = 0; i < 5; i++)
                    UnityEngine.Object.Destroy(upgrades[i].gameObject);
        }
        public static UpgradeDetails[] RemoveDartlingButtons(UpgradeDetails[] upgrades) {
            if(upgrades.Length > 5)
                return upgrades.Skip(5).ToArray();
            return upgrades;
        }
        [HarmonyPrefix]
        public static bool Prefix(ref UpgradeScreen __instance, string towerId) {
            if (DartlingGun.IsDartlingGun(towerId)) {
                DestroyButtons(__instance.path1Upgrades);
                DestroyButtons(__instance.path2Upgrades);
                DestroyButtons(__instance.path3Upgrades);
                TowerModel tower = DartlingGun.GetTower(0, 0, 0);
                __instance.hasTower = true;
                __instance.towerTitle.text = DartlingGun.DisplayName;
                __instance.towerDescription.text = DartlingGun.Description;
                __instance.xpToSpend.text = "XP: Don't worry about it";
                __instance.purchaseTowerXP.gameObject.SetActive(false);
                __instance.purchaseFullTowerUnlock.gameObject.SetActive(false);
                List<UpgradeDetails> needTurnOn = new List<UpgradeDetails>();
                List<UpgradeDetails> ability = new List<UpgradeDetails>();
                for (int r = 0; r < 3; r++) {
                    UpgradeDetails[] details = new UpgradeDetails[5];
                    GameObject container = r switch {
                        0 => __instance.path1Container,
                        1 => __instance.path2Container,
                        2 => __instance.path3Container,
                        _ => null
                    };
                    for (int t = 0; t < 5; t++) {
                        details[t] = __instance.BuildUpgradeButton(__instance.upgradePrefab, container);
                        details[t].SetUpgradeScreen(__instance);
                        details[t].upgrade = DartlingGun.GetUpgrade(r, t + 1);
                        details[t].portrait = tower.portrait;
                        details[t].theme = t == 4 ? details[t].tier5Theme : details[t].standardTheme;
                        details[t].theme.upgradeButton.sprite = details[t].theme.owned;
                        details[t].prevHadUpgrade = t > 0;
                        Utils.SetTexture(details[t].icon, DartlingGun.GetUpgradeName(r, t + 1));
                        if (t == 0 && r == 0) {
                            __instance.selectedDetails = details[t];
                            details[t].OnClick();
                        } else details[t].theme.selected.enabled = false;
                        if (t == 3) needTurnOn.Add(details[t]);
                        if ((t > 2 && r == 1) || (t == 4 && r == 0)) ability.Add(details[t]);
                    }
                    __instance.PopulatePath(tower, r, r switch {
                        0 => __instance.path1Upgrades = details,
                        1 => __instance.path2Upgrades = details,
                        2 => __instance.path3Upgrades = details,
                        _ => default
                    });
                }
                foreach (UpgradeDetails ud in needTurnOn) ud.icon.gameObject.SetActive(true);
                foreach (UpgradeDetails ud in ability) ud.abilityObject.SetActive(true);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(UpgradeScreen __instance, string towerId) {
            for (int i = 0; i < TowerInventoryInit_Patch.towerList.Count; i++)
                if (TowerInventoryInit_Patch.towerList[i].towerId.Contains(towerId)) {
                    __instance.currentIndex = i;
                    MelonLogger.Log(i);
                    return;
                }
        }
    }
    [HarmonyPatch(typeof(UpgradeScreen), "NextTower")]
    public class UpgradeScreenNext_Patch {
        [HarmonyPrefix]
        public static bool Prefix(UpgradeScreen __instance) {
            int index = __instance.currentIndex;
            if (DartlingGun.IsDartlingGun(TowerInventoryInit_Patch.towerList[index])) {
                UpgradeScreen_Patch.DestroyButtons(__instance.path1Upgrades);
                UpgradeScreen_Patch.DestroyButtons(__instance.path2Upgrades);
                UpgradeScreen_Patch.DestroyButtons(__instance.path3Upgrades);
            } else {

            }
            __instance.UpdateUi(TowerInventoryInit_Patch.towerList[index + 1].towerId, "");
            return false;
        }
    }
    [HarmonyPatch(typeof(UpgradeScreen), "PrevTower")]
    public class UpgradeScreenPrev_Patch {
        [HarmonyPrefix]
        public static bool Prefix(UpgradeScreen __instance) {
            int index = __instance.currentIndex;
            if (DartlingGun.IsDartlingGun(TowerInventoryInit_Patch.towerList[index])) {
                UpgradeScreen_Patch.DestroyButtons(__instance.path1Upgrades);
                UpgradeScreen_Patch.DestroyButtons(__instance.path2Upgrades);
                UpgradeScreen_Patch.DestroyButtons(__instance.path3Upgrades);
            }
            __instance.UpdateUi(TowerInventoryInit_Patch.towerList[index - 1].towerId, "");
            return false;
        }
    }

    [HarmonyPatch(typeof(UpgradeDetails), "OnClick")]
    public class UpgradeDetailsClicked_Patch {
        [HarmonyPrefix]
        public static bool Prefix(UpgradeDetails __instance) {
            if (DartlingGun.IsDartlingGun(__instance.upgrade)) {
                __instance.upgradeScreen.selectedDetails.theme.selected.enabled = false;
                __instance.upgradeScreen.selectedDetails = __instance;
                __instance.theme.selected.enabled = true;
                int t = __instance.upgrade.tier, r = __instance.upgrade.path;
                int[] ts = new int[] { r == 0 ? t : 0, r == 1 ? t : 0, r == 2 ? t : 0 };
                __instance.upgradeScreen.selectedUpgrade.SetUpgrade(DartlingGun.GetName(ts[0], ts[1], ts[2]), __instance);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TSMThemeWithActionButton), "Selected")]
    public class TargetType_Patch {
        [HarmonyPostfix]
        public static void Postfix(TSMThemeWithActionButton __instance, TowerToSimulation tower) {
            if (DartlingGun.IsDartlingGun(tower) && tower.Def.tiers[0] > 1)
                __instance.targetTypesToShowButtonFor = new string[] { "TargetSelectedPoint" };
        }
    }

    [HarmonyPatch(typeof(TowerSelectionMenu), "SelectTower")]
    public class TowerSelect_Patch {
        public static void ShowUpgrades(TowerSelectionMenu tsm, TowerToSimulation tts) {
            foreach (UpgradeObject uo in tsm.upgradeButtons) uo.restricted.SetActive(false);
            if (Utils.GetPathsUsed(tts.Def.tiers) == 2) {
                int pathUnused = tts.Def.tiers[0] > 0 ? tts.Def.tiers[1] > 0 ? 2 : 1 : 0;
                tsm.upgradeButtons[pathUnused].locked.GetComponent<Image>().enabled = true;
                tsm.upgradeButtons[pathUnused].locked.transform.Find("Icon").gameObject.GetComponent<Image>().enabled = true;
            }
        }
        [HarmonyPostfix]
        public static void Postfix(TowerSelectionMenu __instance, TowerToSimulation tower) {
            if (DartlingGun.IsDartlingGun(tower))
                ShowUpgrades(__instance, tower);
        }
    }
    [HarmonyPatch(typeof(TowerSelectionMenu), "UpgradeTower", new Type[] { typeof(UpgradeModel), typeof(int), typeof(float) })]
    public class TowerUpgrade_Patch {
        [HarmonyPostfix]
        public static void Postfix(TowerSelectionMenu __instance, UpgradeModel upgrade) {
            if (DartlingGun.IsDartlingGun(upgrade)) {
                TowerSelect_Patch.ShowUpgrades(__instance, __instance.selectedTower);
                if (upgrade.tier == 2 && upgrade.path == 0) {
                    Utils.GetBehavior(__instance.selectedTower.tower.attackBehaviorsInDependants, "TargetSelectedPoint")
                        .Cast<TargetSelectedPoint>().StartDraw();
                    __instance.NextTargetType();
                }
            }
        }
    }

    [HarmonyPatch(typeof(InGame), "Update")]
    public class Update_Patch {
        [HarmonyPostfix]
        public static void Postfix() {
            if (InGame.Bridge == null) return;
            foreach (TowerToSimulation tts in InGame.Bridge.GetAllTowers()) {
                Tower tower = tts.tower;
                InputManager im = InGame.instance.inputManager;
                if (DartlingGun.IsDartlingGun(tower)) {
                    if (tower.targetType != null && tower.targetType.id.Contains("TargetSelected")) {
                        TargetSelectedPoint tsp =
                            Utils.GetBehavior(tower.attackBehaviorsInDependants, "TargetSelected").Cast<TargetSelectedPoint>();
                        tower.Rotation = (tsp.targetPoint - tower.Position).ToVector2().Rotation;
                    } else if (im.cursorInWorld && InGame.instance.HitScene())
                        tower.Rotation = (new Vector2(im.cursorPositionWorld) - tower.Position.ToVector2()).Rotation;
                }
            }
        }
    }

    [HarmonyPatch(typeof(BloodSacrifice), "IsBanned")]
    public class SacrificeIsBanned_Patch {
        [HarmonyPostfix]
        public static bool Postfix(bool banned, BloodSacrifice __instance, Tower tower) {
            if (DartlingGun.IsDartlingGun(__instance.ability.tower))
                return banned || tower.towerModel.towerSet == "Hero" ||
                    (DartlingGun.IsDartlingGun(tower) && tower.towerModel.tiers[0] == 5);
            return banned;
        }
    }
    [HarmonyPatch(typeof(Hero), "AddXp")]
    public class HarnessedSunTiers_Patch {
        [HarmonyPostfix]
        public static void Postfix(Hero __instance, float amount) {
            Tower t = __instance.tower;
            if (DartlingGun.IsDartlingGun(t) && t.towerModel.tiers[0] == 5) {
                __instance.xp += amount; //Doesn't do it on its own for some reason

                TowerModel changedBySacrifice = DartlingGun.GetTower(5, t.towerModel.tiers[1], t.towerModel.tiers[2]);

                DamageModel damage = Utils.GetBehaviorModel<DamageModel>(Utils.GetProjectileModel(changedBySacrifice), "Damage");
                if (__instance.xp > 10000) damage.damage++;
                if (__instance.xp > 20000) damage.damage++;
                if (__instance.xp > 30000) damage.damage++;
                if (__instance.xp > 40000) damage.damage++;
                if (__instance.xp > 50000) {
                    __instance.xp = 50001;
                    damage.damage++;
                }

                t.UpdateRootModel(changedBySacrifice);
            }
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
            if (objectId.Contains("Ray Of Doom Laser")) {
                __result = __instance.FindAndSetupPrototype("bdbeaa256e6c63b45829535831843376", false);
                __result.name = "Ray Of Doom Laser";
                foreach (Renderer renderer in __result.genericRenderers)
                    if (Il2CppType.Of<SpriteRenderer>().IsAssignableFrom(renderer.GetIl2CppType()))
                        Utils.SetTexture(renderer.Cast<SpriteRenderer>(), objectId);
                if(cache)
                    __instance.prototypes.Add(objectId, __result);
                return false;
            }
            return true;
        }
    }

    public static class Utils {
        private static AssetBundleDictionary AssetBundleCache { get; } = new AssetBundleDictionary() {
            {"dartling meshes", AssetBundle.LoadFromMemory(Meshes.dartling_meshes) }
        };
        public static AssetBundle GetAssetBundle(string name) => AssetBundleCache.ContainsKey(name) ? AssetBundleCache[name] : null;
        public static MeshFilter GetMeshFilter(string name) =>
            GetAssetBundle("dartling meshes").LoadAsset(name).Cast<GameObject>()
            .GetComponent(Il2CppType.Of<MeshFilter>()).Cast<MeshFilter>();
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
        public static void SetTexture(Image image, string name) {
            Texture2D texture = GetTexture(name);
            if (texture != null) {
                image.canvasRenderer.SetTexture(texture);
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new UnityEngine.Vector2());
            }
        }
        public static void SetTexture(SpriteRenderer sprite, string name, UnityEngine.Vector2 pivot = new UnityEngine.Vector2()) {
            Texture2D texture = GetTexture(name);
            if (texture != null) {
                sprite.size = new UnityEngine.Vector2(texture.width, texture.height);
                sprite.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 5.4f);
            }
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

        public static TowerModel GetTower(TowerType t, int tier1, int tier2, int tier3) =>
            GetTower(t.ToString(), tier1, tier2, tier3);
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

    public static class DartlingGun {
        public static string DisplayName => "Dartling Gun";
        public static string BaseName => "DartlingGun";
        public static string Description => "Shoots darts like a machine gun, super fast but not very accurate. " +
            "The Dartling Gun will shoot towards wherever your mouse is, so you control how effective it is!";
        public static string InsertAfter => TowerType.MortarMonkey;
        public static string InsertBefore => TowerType.WizardMonkey;
        private static TowerDictionary TowerCache { get; } = new TowerDictionary() {
            { "0 0 0", Get000() },
            { "1 0 0", Get100() },
            { "0 1 0", Get010() },
            { "0 0 1", Get001() },
            { "1 1 0", Get110() },
            { "1 0 1", Get101() },
            { "0 1 1", Get011() },
            { "2 0 0", Get200() },
            { "0 2 0", Get020() },
            { "0 0 2", Get002() },
            { "2 1 0", Get210() },
            { "2 0 1", Get201() },
            { "1 2 0", Get120() },
            { "0 2 1", Get021() },
            { "1 0 2", Get102() },
            { "0 1 2", Get012() },
            { "2 2 0", Get220() },
            { "2 0 2", Get202() },
            { "0 2 2", Get022() },
            { "3 0 0", Get300() },
            { "0 3 0", Get030() },
            { "0 0 3", Get003() },
            { "3 1 0", Get310() },
            { "3 0 1", Get301() },
            { "1 3 0", Get130() },
            { "0 3 1", Get031() },
            { "1 0 3", Get103() },
            { "0 1 3", Get013() },
            { "3 2 0", Get320() },
            { "3 0 2", Get302() },
            { "2 3 0", Get230() },
            { "0 3 2", Get032() },
            { "2 0 3", Get203() },
            { "0 2 3", Get023() },
            { "4 0 0", Get400() },
            { "0 4 0", Get040() },
            { "0 0 4", Get004() },
            { "4 1 0", Get410() },
            { "4 0 1", Get401() },
            { "1 4 0", Get140() },
            { "0 4 1", Get041() },
            { "1 0 4", Get104() },
            { "0 1 4", Get014() },
            { "4 2 0", Get420() },
            { "4 0 2", Get402() },
            { "2 4 0", Get240() },
            { "0 4 2", Get042() },
            { "2 0 4", Get204() },
            { "0 2 4", Get024() },
            { "5 0 0", Get500() },
            { "0 5 0", Get050() },
            { "0 0 5", Get005() },
            { "5 1 0", Get510() },
            { "5 0 1", Get501() },
            { "1 5 0", Get150() },
            { "0 5 1", Get051() },
            { "1 0 5", Get105() },
            { "0 1 5", Get015() },
            { "5 2 0", Get520() },
            { "5 0 2", Get502() },
            { "2 5 0", Get250() },
            { "0 5 2", Get052() },
            { "2 0 5", Get205() },
            { "0 2 5", Get025() }
        };
        private static UpgradeDictionary UpgradeCache { get; } = new UpgradeDictionary() {
            { "0 1", GetUpgrade100() },
            { "1 1", GetUpgrade010() },
            { "2 1", GetUpgrade001() },
            { "0 2", GetUpgrade200() },
            { "1 2", GetUpgrade020() },
            { "2 2", GetUpgrade002() },
            { "0 3", GetUpgrade300() },
            { "1 3", GetUpgrade030() },
            { "2 3", GetUpgrade003() },
            { "0 4", GetUpgrade400() },
            { "1 4", GetUpgrade040() },
            { "2 4", GetUpgrade004() },
            { "0 5", GetUpgrade500() },
            { "1 5", GetUpgrade050() },
            { "2 5", GetUpgrade005() }
        };
        public static string GetName(int tier1, int tier2, int tier3) => $"{BaseName}-{tier1}{tier2}{tier3}";
        public static string GetUpgradeName(int path, int tier) => ($"{path} {tier}") switch
        {
            "0 1" => "Focused Firing",
            "1 1" => "Wider Darts",
            "2 1" => "Powerful Darts",
            "0 2" => "Target Lock",
            "1 2" => "Lucky Shot",
            "2 2" => "Faster Barrel Spin",
            "0 3" => "Laser Cannon",
            "1 3" => "Hydra Rocket Pods",
            "2 3" => "Fine Tuning",
            "0 4" => "Ray of Doom",
            "1 4" => "Bloon Area Denial System",
            "2 4" => "Double Barrel",
            "0 5" => "Harness the Sun",
            "1 5" => "System Upgrade",
            "2 5" => "Industrial Dart Supply",
            _ => default
        };
        public static string GetUpgradeNameFull(int path, int tier) => $"{BaseName}-{GetUpgradeName(path, tier)}";
        public static string GetUpgradeDescription(int path, int tier) => ($"{path} {tier}") switch
        {
            "0 1" => "Greatly reduces the spread of the gun.",
            "1 1" => "Shoots larger darts that can pop frozen bloons.",
            "2 1" => "Darts move faster and have increased pierce.",
            "0 2" => "A new targeting option enables Dartling to lock its targeting.",
            "1 2" => "Allows Dartling to pop camo.",
            "2 2" => "Makes gun fire faster.",
            "0 3" => "Converts the gun into a super powerful laser cannon.",
            "1 3" => "Shoots vicious little missiles instead of darts.",
            "2 3" => "Makes gun fire even faster.",
            "0 4" => "The Ray of Doom is a persistent solid beam of bloon destruction.",
            "1 4" => "The BADS covers a wide area with each shot. Rocket Storm Ability: Shoots a storm of 100 missiles.",
            "2 4" => "Double the barrels, double the fun.",
            "0 5" => "The TRUE Sun God has bestowed upon this cannon near UNPARALLELED power! Make prayer.",
            "1 5" => "More! MORE!!",
            "2 5" => "Too...many...darts...",
            _ => default
        };

        public static TowerModel GetTower(int tier1, int tier2, int tier3) => TowerCache[$"{tier1} {tier2} {tier3}"];
        public static UpgradeModel GetUpgrade(int path, int tier) => UpgradeCache[$"{path} {tier}"];
        public static UpgradeModel GetUpgrade(int path, int tier, int cost, int xpCost, SpriteReference icon, int locked, string confirmation, string localizedNameOverride) =>
            new UpgradeModel(GetUpgradeNameFull(path, tier), cost, xpCost, icon, path, tier, locked, confirmation, localizedNameOverride);
        public static UpgradePathModel GetUpgradePath(int tier1, int tier2, int tier3, int namePath) =>
            new UpgradePathModel(GetUpgradeNameFull(namePath, new int[] { tier1, tier2, tier3 }[namePath]), GetName(tier1, tier2, tier3),
                Utils.GetPathsUsed(tier1, tier2, tier3), Utils.GetTier(tier1, tier2, tier3));
        public static TowerModel GetTowerCommonSet(TowerModel tower, int tier1, int tier2, int tier3) {
            tower.name = GetName(tier1, tier2, tier3);
            tower.tier = Utils.GetTier(tier1, tier2, tier3);
            tower.tiers = new int[] { tier1, tier2, tier3 };

            string[] appliedUpgrades = new string[tier1 + tier2 + tier3];
            for (int p = 0, i = 0; p < 3; p++)
                for (int t = 1; t <= tower.tiers[p]; t++, i++)
                    appliedUpgrades[i] = GetUpgradeNameFull(p, t);
            tower.appliedUpgrades = appliedUpgrades;

            UpgradePathModel[] upgrades;
            bool[] upgradesAvailable = new bool[3];
            for (int p = 0; p < 3; p++)
                upgradesAvailable[p] = tower.tiers[p] < 5 && (Utils.GetPathsUsed(tier1, tier2, tier3) < 2 ||
                    (tower.tiers[p] > 0 && (tower.tier < 3 || tower.tiers[p] != 2)));
            upgrades = new UpgradePathModel[upgradesAvailable.Count(p => p)];
            for (int p = 0, i = 0; p < 3; p++)
                if (upgradesAvailable[p]) {
                    int[] upgradeTiers = { tower.tiers[0], tower.tiers[1], tower.tiers[2] };
                    upgradeTiers[p]++;
                    upgrades[i] = GetUpgradePath(upgradeTiers[0], upgradeTiers[1], upgradeTiers[2], p);
                    i++;
                }
            tower.upgrades = upgrades;

            return tower;
        }
        public static bool IsDartlingGun(string name) => name.Contains(BaseName);
        public static bool IsDartlingGun(Model model) => IsDartlingGun(model.name);
        public static bool IsDartlingGun(Tower tower) => IsDartlingGun(tower.towerModel);
        public static bool IsDartlingGun(TowerToSimulation tts) => IsDartlingGun(tts.Def);

        public static void Add(int tier1, int tier2, int tier3) {
            Utils.AddTowerModel(GetTower(tier1, tier2, tier3));
            bool[] pathsUsed = Utils.GetUsedPaths(tier1, tier2, tier3);
            if (Utils.GetPathsUsed(tier1, tier2, tier3) == 1) {
                int path = Array.FindIndex(pathsUsed, p => p);
                int tier = new int[] { tier1, tier2, tier3 }[path];
                Utils.AddUpgradeModel(GetUpgrade(path, tier));
                Utils.AddTextChange(GetUpgradeNameFull(path, tier), GetUpgradeName(path, tier));
                Utils.AddTextChange($"{GetUpgradeNameFull(path, tier)} Description", GetUpgradeDescription(path, tier));
            } else if (tier1 + tier2 + tier3 == 0)
                Utils.AddTextChange(BaseName, DisplayName);
        }

        private static TowerModel Get000() {
            TowerModel bomb = Utils.GetTower(TowerType.BombShooter, 0, 0, 0);
            TowerModel dart = Utils.GetTower(TowerType.DartMonkey, 0, 0, 0);
            TowerModel mort = Utils.GetTower(TowerType.MortarMonkey, 0, 0, 0);

            TowerModel dartling = Utils.Clone(bomb);
            dartling.name = BaseName;
            dartling.baseId = BaseName;
            dartling.towerSet = "Military";
            dartling.cost = 1000;
            dartling.range = 20;
            dartling.isGlobalRange = true;
            dartling.radius = 11;
            //dartling.portrait = new SpriteReference() { guidRef = GetUpgradeName(0, 1) };
            //dartling.display = "cube";
            dartling.footprint = new CircleFootprintModel($"CircleFootprintModel_{BaseName}", 11, false, false, false);
            dartling.mods = new ApplyModModel[] {
                new ApplyModModel($"{BaseName}_Knowledge", "GlobalAbilityCooldowns", ""),
                new ApplyModModel($"{BaseName}_Knowledge", "VeteranMonkeyTraining", ""),
                new ApplyModModel($"{BaseName}_Knowledge", "MonkeyEducation", ""),
                new ApplyModModel($"{BaseName}_Knowledge", "BetterSellDeals", ""),
                new ApplyModModel($"{BaseName}_Knowledge", "EliteMilitaryTraining", ""),
            };
            dartling.upgrades = new UpgradePathModel[] {
                GetUpgradePath(1, 0, 0, 0),
                GetUpgradePath(0, 1, 0, 1),
                GetUpgradePath(0, 0, 1, 2)
            };
            dartling.targetTypes = new TargetType[] { };

            dartling.behaviors = Utils.Clone(dartling.behaviors);
            int attackIndex = Utils.GetAttackModelIndex(dartling);
            dartling.behaviors[attackIndex] = Utils.Clone(dartling.behaviors[attackIndex]);

            AttackModel attack = dartling.behaviors[attackIndex].Cast<AttackModel>();
            attack.range = 9999999;
            attack.fireWithoutTarget = true;
            attack.behaviors = Utils.Clone(Utils.GetAttackModel(mort).behaviors);
            attack.weapons = Utils.Clone(attack.weapons);

            Utils.RemoveBehaviorModel(attack, "TargetSelected");
            attack.targetProvider = null;

            WeaponModel weapon = attack.weapons[0] = Utils.Clone(attack.weapons[0]);
            weapon.Rate = 0.25f;
            weapon.fireWithoutTarget = true;
            weapon.ejectZ = 2.6f;
            weapon.emission = new SingleEmissionModel($"SingleEmissionModel_{BaseName}", null);
            Utils.AddBehaviorModel(weapon, new OffsetModel($"OffsetModel_{BaseName}", 23, 0));

            ProjectileModel projectile = weapon.projectile = Utils.Clone(Utils.GetProjectileModel(dart, 0));
            projectile.pierce = 1;
            projectile.behaviors = Utils.Clone(projectile.behaviors);
            int travelStraightIndex = Utils.GetBehaviorModelIndex(projectile, "TravelStrait");
            projectile.behaviors[travelStraightIndex] = Utils.Clone(projectile.behaviors[travelStraightIndex]);

            TravelStraitModel travelStrait = projectile.behaviors[travelStraightIndex].Cast<TravelStraitModel>();
            travelStrait.lifespan = 9999999;
            travelStrait.speed = 325;

            Utils.AddBehaviorModel(projectile,
                new ExpireProjectileAtScreenEdgeModel($"ExpireProjectileModelAtScreenEdgeModel_{BaseName}"));

            return dartling;
        }

        private static UpgradeModel GetUpgrade100() => 
            GetUpgrade(0, 1, 250, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(0, 1) }, 0, "", "");
        private static TowerModel Get100(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling100 = dontSetCommon ? baseTower ?? Get000() : GetTowerCommonSet(baseTower ?? Get000(), 1, 0, 0);

            WeaponModel weapon = Utils.GetWeaponModel(dartling100.behaviors, 0);
            Utils.GetBehaviorModel<OffsetModel>(weapon, "Offset").range = 8f;

            return dartling100;
        }

        private static UpgradeModel GetUpgrade010() => 
            GetUpgrade(1, 1, 100, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(1, 1) }, 0, "", "");
        private static TowerModel Get010(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling010 = dontSetCommon ? baseTower ?? Get000() : GetTowerCommonSet(baseTower ?? Get000(), 0, 1, 0);

            ProjectileModel projectile = Utils.GetProjectileModel(dartling010);
            projectile.radius = 3;
            projectile.scale = 1.5f;
            int damageIndex = Utils.GetBehaviorModelIndex(projectile, "DamageModel");
            projectile.behaviors[damageIndex] = Utils.Clone(projectile.behaviors[damageIndex]);

            DamageModel damage = projectile.behaviors[damageIndex].Cast<DamageModel>();
            damage.damageTypes = new string[] { "Shatter" };

            return dartling010;
        }

        private static UpgradeModel GetUpgrade001() => 
            GetUpgrade(2, 1, 600, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(2, 1) }, 0, "", "");
        private static TowerModel Get001(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling001 = dontSetCommon ? baseTower ?? Get000() : GetTowerCommonSet(baseTower ?? Get000(), 0, 0, 1);

            ProjectileModel projectile = Utils.GetProjectileModel(dartling001);
            projectile.pierce += 2;
            Utils.GetBehaviorModel<TravelStraitModel>(projectile, "TravelStrait").speed += 130;

            return dartling001;
        }

        private static TowerModel Get110() => GetTowerCommonSet(Get100(true, Get010(true)), 1, 1, 0);

        private static TowerModel Get101() => GetTowerCommonSet(Get100(true, Get001(true)), 1, 0, 1);

        private static TowerModel Get011() => GetTowerCommonSet(Get010(true, Get001(true)), 0, 1, 1);

        private static UpgradeModel GetUpgrade200() =>
            GetUpgrade(0, 2, 500, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(0, 2) }, 0, "", "");
        private static TowerModel Get200(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel mort = Utils.GetTower(TowerType.MortarMonkey, 0, 0, 1);

            TowerModel dartling200 = dontSetCommon ? Get100(true, baseTower) : GetTowerCommonSet(Get100(true, baseTower), 2, 0, 0);
            dartling200.towerSelectionMenuThemeId = "ActionButton";
            dartling200.targetTypes = new TargetType[] {
                new TargetType(mort.targetTypes[0].Pointer) { id = "FollowTouch" },
                new TargetType(mort.targetTypes[0].Pointer) { id = "TargetSelectedPoint" }
            };

            AttackModel attack = Utils.GetAttackModel(dartling200);
            TargetSelectedPointModel target = Utils.GetBehaviorModel<TargetSelectedPointModel>(Utils.GetAttackModel(mort), "TargetSelect");
            attack.targetProvider = target;
            Utils.AddBehaviorModel(attack, target);
            Utils.AddBehaviorModel(attack, new FollowTouchSettingModel($"FollowTouchModel_{BaseName}", true, false));

            return dartling200;
        }

        private static UpgradeModel GetUpgrade020() =>
            GetUpgrade(1, 2, 600, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(1, 2) }, 0, "", "");
        private static TowerModel Get020(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling020 = dontSetCommon ? Get010(true, baseTower) : GetTowerCommonSet(Get010(true, baseTower), 0, 2, 0);

            FilterInvisibleModel filterInvisible = new FilterInvisibleModel($"FilterInvisibleModel_{BaseName}", false, false);

            ProjectileModel projectile = Utils.GetProjectileModel(dartling020);
            projectile.filters = new FilterModel[] { filterInvisible };
            int filterIndex = Utils.GetBehaviorModelIndex(projectile, "ProjectileFilter");
            projectile.behaviors[filterIndex] = Utils.Clone(projectile.behaviors[filterIndex]);

            ProjectileFilterModel projectileFilter = projectile.behaviors[filterIndex].Cast<ProjectileFilterModel>();
            projectileFilter.filters = new FilterModel[] { filterInvisible };

            return dartling020;
        }

        private static UpgradeModel GetUpgrade002() =>
            GetUpgrade(2, 2, 1200, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(2, 2) }, 0, "", "");
        private static TowerModel Get002(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling002 = dontSetCommon ? Get001(true, baseTower) : GetTowerCommonSet(Get001(true, baseTower), 0, 0, 2);

            Utils.GetWeaponModel(dartling002).rate -= 0.075f;

            return dartling002;
        }

        private static TowerModel Get210() => GetTowerCommonSet(Get200(true, Get010(true)), 2, 1, 0);

        private static TowerModel Get201() => GetTowerCommonSet(Get200(true, Get001(true)), 2, 0, 1);

        private static TowerModel Get120() => GetTowerCommonSet(Get020(true, Get100(true)), 1, 2, 0);

        private static TowerModel Get021() => GetTowerCommonSet(Get001(true, Get020(true)), 0, 2, 1);

        private static TowerModel Get102() => GetTowerCommonSet(Get002(true, Get100(true)), 1, 0, 2);

        private static TowerModel Get012() => GetTowerCommonSet(Get002(true, Get010(true)), 0, 1, 2);

        private static TowerModel Get220() => GetTowerCommonSet(Get200(true, Get020(true)), 2, 2, 0);

        private static TowerModel Get202() => GetTowerCommonSet(Get200(true, Get002(true)), 2, 0, 2);

        private static TowerModel Get022() => GetTowerCommonSet(Get020(true, Get002(true)), 0, 2, 2);

        private static UpgradeModel GetUpgrade300() =>
            GetUpgrade(0, 3, 3000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(0, 3) }, 0, "", "");
        private static TowerModel Get300(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel heli = Utils.GetTower(TowerType.HeliPilot, 5, 0, 0);

            TowerModel dartling300 = dontSetCommon ? Get200(true, baseTower) : GetTowerCommonSet(Get200(true, baseTower), 3, 0, 0);

            ProjectileModel projectile = Utils.GetProjectileModel(dartling300);
            projectile.radius += 1;
            projectile.scale = projectile.radius / 3;
            projectile.pierce += 13;
            int displayIndex = Utils.GetBehaviorModelIndex(projectile, "Display");
            int damageIndex = Utils.GetBehaviorModelIndex(projectile, "Damage");
            projectile.behaviors[displayIndex] = Utils.Clone(projectile.behaviors[displayIndex]);
            projectile.behaviors[damageIndex] = Utils.Clone(projectile.behaviors[damageIndex]);

            DisplayModel display = projectile.behaviors[displayIndex].Cast<DisplayModel>();
            projectile.display = display.display = Utils.GetProjectileModel(heli).display;

            DamageModel damage = projectile.behaviors[damageIndex].Cast<DamageModel>();
            damage.damage += 1;
            damage.damageTypes = new string[] { "Energy" };

            return dartling300;
        }

        private static UpgradeModel GetUpgrade030() =>
            GetUpgrade(1, 3, 2000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(1, 3) }, 0, "", "");
        private static TowerModel Get030(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel heli = Utils.GetTower(TowerType.HeliPilot, 4, 0, 0);
            ProjectileModel missile = Utils.GetProjectileModel(heli, 1);

            TowerModel dartling030 = dontSetCommon ? Get020(true, baseTower) : GetTowerCommonSet(Get020(true, baseTower), 0, 3, 0);

            WeaponModel weapon = Utils.GetWeaponModel(dartling030);
            weapon.emission = new ArcEmissionModel($"ArcEmissionModel_{BaseName}", 1, 0, 30, null, false, false);

            ProjectileModel projectile = Utils.GetProjectileModel(dartling030);
            projectile.pierce = 1;
            projectile.scale = 1;
            int displayIndex = Utils.GetBehaviorModelIndex(projectile, "Display");

            DisplayModel display = projectile.behaviors[displayIndex].Cast<DisplayModel>();
            projectile.display = display.display = missile.display;

            Utils.RemoveBehaviorModel(projectile, "Damage");

            Utils.AddBehaviorModel(projectile, Utils.GetBehaviorModel(missile, "CreateEffect"));
            Utils.AddBehaviorModel(projectile, Utils.GetBehaviorModel(missile, "CreateSound"));

            CreateProjectileOnContactModel createProjectile =
                Utils.Clone(Utils.GetBehaviorModel(missile, "CreateProjectile").Cast<CreateProjectileOnContactModel>());
            Utils.AddBehaviorModel(projectile, createProjectile);

            ProjectileModel hydra = createProjectile.projectile = Utils.Clone(createProjectile.projectile);
            hydra.filters = projectile.filters;
            int hydraDamageIndex = Utils.GetBehaviorModelIndex(hydra, "Damage");
            int hydraFilterIndex = Utils.GetBehaviorModelIndex(hydra, "Filter");
            hydra.behaviors[hydraDamageIndex] = Utils.Clone(hydra.behaviors[hydraDamageIndex]);
            hydra.behaviors[hydraFilterIndex] = Utils.GetBehaviorModel(projectile, "Filter");

            DamageModel hydraDamage = hydra.behaviors[hydraDamageIndex].Cast<DamageModel>();
            hydraDamage.damage = 1;

            return dartling030;
        }

        private static UpgradeModel GetUpgrade003() =>
            GetUpgrade(2, 3, 2000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(2, 3) }, 0, "", "");
        private static TowerModel Get003(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling003 = dontSetCommon ? Get002(true, baseTower) : GetTowerCommonSet(Get002(true, baseTower), 0, 0, 3);

            Utils.GetWeaponModel(dartling003).rate -= 0.075f;

            return dartling003;
        }

        private static TowerModel Get310() => GetTowerCommonSet(Get300(true, Get010(true)), 3, 1, 0);

        private static TowerModel Get301() => GetTowerCommonSet(Get300(true, Get001(true)), 3, 0, 1);

        private static TowerModel Get130() => GetTowerCommonSet(Get030(true, Get100(true)), 1, 3, 0);

        private static TowerModel Get031() => GetTowerCommonSet(Get030(true, Get001(true)), 0, 3, 1);

        private static TowerModel Get103() => GetTowerCommonSet(Get003(true, Get100(true)), 1, 0, 3);

        private static TowerModel Get013() => GetTowerCommonSet(Get003(true, Get010(true)), 0, 1, 3);

        private static TowerModel Get320() => GetTowerCommonSet(Get300(true, Get020(true)), 3, 2, 0);

        private static TowerModel Get302() => GetTowerCommonSet(Get300(true, Get002(true)), 3, 0, 2);

        private static TowerModel Get230() => GetTowerCommonSet(Get030(true, Get200(true)), 2, 3, 0);

        private static TowerModel Get032() => GetTowerCommonSet(Get030(true, Get002(true)), 0, 3, 2);

        private static TowerModel Get203() => GetTowerCommonSet(Get003(true, Get200(true)), 2, 0, 3);

        private static TowerModel Get023() => GetTowerCommonSet(Get003(true, Get020(true)), 0, 2, 3);

        private static UpgradeModel GetUpgrade400() =>
            GetUpgrade(0, 4, 55000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(0, 4) }, 0, "", "");
        private static TowerModel Get400(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling400 = dontSetCommon ? Get300(true, baseTower) : GetTowerCommonSet(Get300(true, baseTower), 4, 0, 0);

            AttackModel attack = Utils.GetAttackModel(dartling400);

            Utils.AddBehaviorModel(attack, new DisplayModel("DisplayModel_RayOfDoom", "Ray Of Doom Laser", 0));

            WeaponModel weapon = attack.weapons[0];
            weapon.rate = 1 / 30f;

            Utils.RemoveBehaviorModel(weapon, Utils.GetBehaviorModel<OffsetModel>(weapon, "Offset"));

            ProjectileModel projectile = weapon.projectile;
            projectile.radius = 5;
            projectile.pierce = 9999999;
            projectile.ignoreBlockers = true;

            TravelStraitModel travelStrait = Utils.GetBehaviorModel<TravelStraitModel>(projectile, "TravelStrait");
            travelStrait.speed = 1000;

            DamageModel damage = Utils.GetBehaviorModel<DamageModel>(projectile, "Damage");
            damage.damage = 1;
            damage.damageTypes = new string[] { "Normal" };

            DisplayModel display = Utils.GetBehaviorModel<DisplayModel>(projectile, "Display");
            display.display = projectile.display = null;

            return dartling400;
        }

        private static UpgradeModel GetUpgrade040() =>
            GetUpgrade(1, 4, 35000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(1, 4) }, 0, "", "");
        private static TowerModel Get040(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel bomb = Utils.GetTower(TowerType.BombShooter, 0, 4, 0);
            UpgradeModel bombu = Game.instance.model.upgradesByName["Faster Reload"];

            TowerModel dartling040 = dontSetCommon ? Get030(true, baseTower) : GetTowerCommonSet(Get030(true, baseTower), 0, 4, 0);

            WeaponModel weapon = Utils.GetWeaponModel(dartling040);
            weapon.emission.Cast<ArcEmissionModel>().count = 3;

            ProjectileModel hydra =
                Utils.GetBehaviorModel<CreateProjectileOnContactModel>(weapon.projectile, "CreateProjectile").projectile;

            DamageModel hydraDamage = Utils.GetBehaviorModel<DamageModel>(hydra, "Damage");
            hydraDamage.damageTypes = new string[] { "Normal" };
            hydraDamage.damage += 2;

            AbilityModel ability = Utils.Clone(Utils.GetAbilityModel(bomb));
            ability.cooldown = 30;
            ability.addedViaUpgrade = GetUpgradeNameFull(1, 4);
            ability.animation = 1;
            ability.description = "Rocket Storm";
            ability.name = $"AbilityModel_{BaseName}_Rocket Storm";
            ability.displayName = "Rocket Storm";
            ability.behaviors = Utils.Clone(ability.behaviors);
            ability.icon = bombu.icon;
            int abilityActivateAttackIndex = Utils.GetBehaviorModelIndex(ability, "ActivateAttack");
            ability.behaviors[abilityActivateAttackIndex] = Utils.Clone(ability.behaviors[abilityActivateAttackIndex]);

            ActivateAttackModel activateAttack = ability.behaviors[abilityActivateAttackIndex].Cast<ActivateAttackModel>();
            activateAttack.turnOffExisting = true;
            activateAttack.attacks = Utils.Clone(activateAttack.attacks);

            AttackModel abilityAttack = activateAttack.attacks[0] = Utils.Clone(Utils.GetAttackModel(dartling040));
            abilityAttack.weapons = Utils.Clone(abilityAttack.weapons);

            WeaponModel abilityWeapon = abilityAttack.weapons[0] = Utils.Clone(abilityAttack.weapons[0]);
            abilityWeapon.emission = Utils.Clone(abilityWeapon.emission);

            ArcEmissionModel abilityArcEmission = abilityWeapon.emission.Cast<ArcEmissionModel>();
            abilityArcEmission.count = 100;

            Utils.AddBehaviorModel(dartling040, ability);

            return dartling040;
        }

        private static UpgradeModel GetUpgrade004() =>
            GetUpgrade(2, 4, 5500, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(2, 4) }, 0, "", "");
        private static TowerModel Get004(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling004 = dontSetCommon ? Get003(true, baseTower) : GetTowerCommonSet(Get003(true, baseTower), 0, 0, 4);

            AttackModel attack = Utils.GetAttackModel(dartling004);

            WeaponModel weapon1 = attack.weapons[0];
            weapon1.rate -= 0.02f;
            weapon1.projectile.pierce += 3;
            WeaponModel weapon2 = Utils.Clone(weapon1);

            weapon1.name = "WeaponModel_Weapon1";
            weapon2.name = "WeaponModel_Weapon2";
            weapon1.ejectX = 5.05f;
            weapon2.ejectX = -5.05f;

            attack.weapons = Utils.AddModel(attack.weapons, weapon2, 1);

            return dartling004;
        }

        private static TowerModel Get410() => GetTowerCommonSet(Get400(true, Get010(true)), 4, 1, 0);

        private static TowerModel Get401() => GetTowerCommonSet(Get400(true, Get001(true)), 4, 0, 1);

        private static TowerModel Get140() => GetTowerCommonSet(Get040(true, Get100(true)), 1, 4, 0);

        private static TowerModel Get041() => GetTowerCommonSet(Get040(true, Get001(true)), 0, 4, 1);

        private static TowerModel Get104() => GetTowerCommonSet(Get004(true, Get100(true)), 1, 0, 4);

        private static TowerModel Get014() => GetTowerCommonSet(Get004(true, Get010(true)), 0, 1, 4);

        private static TowerModel Get420() => GetTowerCommonSet(Get400(true, Get020(true)), 4, 2, 0);

        private static TowerModel Get402() {
            TowerModel dartling402 = GetTowerCommonSet(Get400(true, Get002(true)), 4, 0, 2);

            WeaponModel weapon = Utils.GetWeaponModel(dartling402);
            weapon.rate /= 1.05f;

            return dartling402;
        }

        private static TowerModel Get240() => GetTowerCommonSet(Get040(true, Get200(true)), 2, 4, 0);

        private static TowerModel Get042() => GetTowerCommonSet(Get040(true, Get002(true)), 0, 4, 2);

        private static TowerModel Get204() => GetTowerCommonSet(Get004(true, Get200(true)), 2, 0, 4);

        private static TowerModel Get024() => GetTowerCommonSet(Get004(true, Get020(true)), 0, 2, 4);

        private static UpgradeModel GetUpgrade500() =>
            GetUpgrade(0, 5, 80000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(0, 5) }, 0, "", "");
        private static TowerModel Get500(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel adora = Game.instance.model.GetTowerWithName("Adora 7");
            AbilityModel sac = Utils.GetAbilityModel(adora, 1);
            TowerModel temple = Utils.GetTower(TowerType.SuperMonkey, 4, 0, 0);
            EffectModel esac = Utils.GetBehaviorModel<MonkeyTempleModel>(temple, "MonkeyTemple").towerEffectModel;

            TowerModel dartling500 = dontSetCommon ? Get400(true, baseTower) : GetTowerCommonSet(Get400(true, baseTower), 5, 0, 0);
            int createSoundOnUpgradeIndex = Utils.GetBehaviorModelIndex(dartling500, "CreateSoundOnUpgrade");
            dartling500.behaviors[createSoundOnUpgradeIndex] = Utils.Clone(dartling500.behaviors[createSoundOnUpgradeIndex]);

            CreateSoundOnUpgradeModel createSoundOnUpgrade = 
                dartling500.behaviors[createSoundOnUpgradeIndex].Cast<CreateSoundOnUpgradeModel>();
            createSoundOnUpgrade.sound8 = createSoundOnUpgrade.sound7 = createSoundOnUpgrade.sound6 = createSoundOnUpgrade.sound5 = 
                createSoundOnUpgrade.sound4 = createSoundOnUpgrade.sound3 = createSoundOnUpgrade.sound2 = createSoundOnUpgrade.sound1 = 
                createSoundOnUpgrade.sound; //literally bald

            WeaponModel weapon = Utils.GetWeaponModel(dartling500);
            weapon.rate = 1 / 60f;
            weapon.projectile.radius = 10;

            HeroModel xp = new HeroModel($"Xp_{BaseName}", 1, 1);
            Utils.AddBehaviorModel(dartling500, xp);

            AbilityModel ability = Utils.Clone(sac);
            ability.addedViaUpgrade = GetUpgradeNameFull(0, 5);
            ability.animation = 2;
            ability.name = $"AbilityModel_{BaseName}_SacrificeForPower";
            ability.displayName = "Sacrifice for Power";
            ability.behaviors = Utils.Clone(ability.behaviors);
            int createEffectIndex = Utils.GetBehaviorModelIndex(ability, "CreateEffect");
            int createSoundIndex = Utils.GetBehaviorModelIndex(ability, "CreateSound");
            int bloodSacrificeIndex = Utils.GetBehaviorModelIndex(ability, "BloodSacrificeModel");
            ability.behaviors[createEffectIndex] = Utils.Clone(ability.behaviors[createEffectIndex]);
            ability.behaviors[createSoundIndex] = Utils.Clone(ability.behaviors[createSoundIndex]);
            ability.behaviors[bloodSacrificeIndex] = Utils.Clone(ability.behaviors[bloodSacrificeIndex]);

            CreateEffectOnAbilityModel createEffect = ability.behaviors[createEffectIndex].Cast<CreateEffectOnAbilityModel>();
            createEffect.name = $"CreateEffectOnAbilityModel_{BaseName}_SacrificeForPower";
            createEffect.effectModel = Utils.Clone(esac);

            CreateSoundOnAbilityModel createSound = ability.behaviors[createSoundIndex].Cast<CreateSoundOnAbilityModel>();
            createSound.name = $"CreateSoundOnAbiltyModel_{BaseName}_SacrificeForPower";
            createSound.heroSound = createSound.sound;

            BloodSacrificeModel bloodSacrifice = ability.behaviors[bloodSacrificeIndex].Cast<BloodSacrificeModel>();
            bloodSacrifice.name = $"BloodSacrificeModel_{BaseName}_SacrificeForPower";
            bloodSacrifice.xpMultiplier = 1;

            Utils.AddBehaviorModel(dartling500, ability);

            return dartling500;
        }

        private static UpgradeModel GetUpgrade050() =>
            GetUpgrade(1, 5, 60000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(1, 5) }, 0, "", "");
        private static TowerModel Get050(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling050 = dontSetCommon ? Get040(true, baseTower) : GetTowerCommonSet(Get040(true, baseTower), 0, 5, 0);

            AbilityModel ability = Utils.GetAbilityModel(dartling050);
            ability.addedViaUpgrade = GetUpgradeNameFull(1, 5);
            ability.animation = 1;
            ability.description = "Mega Rocket Storm";
            ability.name = $"AbilityModel_{BaseName}_Mega Rocket Storm";
            ability.displayName = "Mega Rocket Storm";

            ActivateAttackModel activateAttack = Utils.GetBehaviorModel<ActivateAttackModel>(ability, "ActivateAttack");

            WeaponModel abilityWeapon = activateAttack.attacks[0].weapons[0];

            ArcEmissionModel abilityArcEmission = abilityWeapon.emission.Cast<ArcEmissionModel>();
            abilityArcEmission.angle = 180;
            abilityArcEmission.count = 1000;

            return dartling050;
        }

        private static UpgradeModel GetUpgrade005() =>
            GetUpgrade(2, 5, 35000, int.MaxValue, new SpriteReference() { guidRef = GetUpgradeName(2, 5) }, 0, "", "");
        private static TowerModel Get005(bool dontSetCommon = false, TowerModel baseTower = null) {
            TowerModel dartling005 = dontSetCommon ? Get004(true, baseTower) : GetTowerCommonSet(Get004(true, baseTower), 0, 0, 5);

            AttackModel attack = Utils.GetAttackModel(dartling005);

            WeaponModel weapon1 = attack.weapons[0];
            WeaponModel weapon2 = attack.weapons[1];
            weapon1.rate = weapon2.rate -= .02f;
            weapon1.projectile.pierce = weapon2.projectile.pierce += 3;
            int damage1Index = Utils.GetBehaviorModelIndex(weapon1.projectile, "DamageModel");
            int damage2Index = Utils.GetBehaviorModelIndex(weapon2.projectile, "DamageModel");
            weapon1.projectile.behaviors[damage1Index] = Utils.Clone(weapon1.projectile.behaviors[damage1Index]);
            weapon2.projectile.behaviors[damage2Index] = Utils.Clone(weapon2.projectile.behaviors[damage2Index]);

            DamageModel damage1 = weapon1.projectile.behaviors[damage1Index].Cast<DamageModel>();
            DamageModel damage2 = weapon2.projectile.behaviors[damage2Index].Cast<DamageModel>();
            damage1.damageTypes = damage2.damageTypes = new string[] { "Normal" };

            WeaponModel weapon3 = Utils.Clone(weapon1);
            WeaponModel weapon4 = Utils.Clone(weapon2);
            WeaponModel weapon5 = Utils.Clone(weapon1);
            WeaponModel weapon6 = Utils.Clone(weapon2);

            weapon3.name = "WeaponModel_Weapon3";
            weapon4.name = "WeaponModel_Weapon4";
            weapon5.name = "WeaponModel_Weapon5";
            weapon6.name = "WeaponModel_Weapon6";
            weapon3.ejectX += 10;
            weapon4.ejectX -= 10;
            weapon5.ejectX += 5;
            weapon5.ejectZ += 8.66f;
            weapon6.ejectX -= 5;
            weapon6.ejectZ += 8.66f;

            attack.weapons = Utils.AddModel(attack.weapons, weapon3, 2);
            attack.weapons = Utils.AddModel(attack.weapons, weapon4, 3);
            attack.weapons = Utils.AddModel(attack.weapons, weapon5, 4);
            attack.weapons = Utils.AddModel(attack.weapons, weapon6, 5);

            return dartling005;
        }

        private static TowerModel Get510() => GetTowerCommonSet(Get500(true, Get010(true)), 5, 1, 0);

        private static TowerModel Get501() => GetTowerCommonSet(Get500(true, Get001(true)), 5, 0, 1);

        private static TowerModel Get150() => GetTowerCommonSet(Get050(true, Get100(true)), 1, 5, 0);

        private static TowerModel Get051() => GetTowerCommonSet(Get050(true, Get001(true)), 0, 5, 1);

        private static TowerModel Get105() => GetTowerCommonSet(Get005(true, Get100(true)), 1, 0, 5);

        private static TowerModel Get015() => GetTowerCommonSet(Get005(true, Get010(true)), 0, 1, 5);

        private static TowerModel Get520() => GetTowerCommonSet(Get500(true, Get020(true)), 5, 2, 0);

        private static TowerModel Get502() {
            TowerModel dartling502 = GetTowerCommonSet(Get500(true, Get002(true)), 5, 0, 2);

            WeaponModel weapon = Utils.GetWeaponModel(dartling502);
            weapon.rate /= 1.05f;

            return dartling502;
        }

        private static TowerModel Get250() => GetTowerCommonSet(Get050(true, Get200(true)), 2, 5, 0);

        private static TowerModel Get052() => GetTowerCommonSet(Get050(true, Get002(true)), 0, 5, 2);

        private static TowerModel Get205() => GetTowerCommonSet(Get005(true, Get200(true)), 2, 0, 5);

        private static TowerModel Get025() => GetTowerCommonSet(Get005(true, Get020(true)), 0, 2, 5);
    }
}