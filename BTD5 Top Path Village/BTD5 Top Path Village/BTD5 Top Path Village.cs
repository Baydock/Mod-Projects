using Assets.Scripts.Models;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Models.Towers;
using Assets.Scripts.Models.Towers.Behaviors.Abilities;
using Assets.Scripts.Models.Towers.Behaviors.Attack;
using Assets.Scripts.Models.Towers.Filters;
using Assets.Scripts.Models.Towers.Projectiles;
using Assets.Scripts.Models.Towers.Projectiles.Behaviors;
using Assets.Scripts.Models.Towers.Upgrades;
using Assets.Scripts.Models.Towers.Weapons;
using Assets.Scripts.Models.TowerSets;
using Assets.Scripts.Simulation.Behaviors;
using Assets.Scripts.Simulation.Objects;
using Assets.Scripts.Simulation.Towers;
using Assets.Scripts.Simulation.Towers.Behaviors.Attack;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Localization;
using Assets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Assets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu.TowerSelectionMenuThemes;
using Assets.Scripts.Utils;
using Harmony;
using MelonLoader;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Timers;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using Vector3 = Assets.Scripts.Simulation.SMath.Vector3;

namespace BTD5TopPathVillage {

    public class BTD5TopPathVillage : MelonMod { }

    [HarmonyPatch(typeof(TSMThemeAmbidextrousRangs), "TowerInfoChanged")]
    public class TSMTheme_Patch {
        private static Sprite supportSprite = null;
        [HarmonyPostfix]
        public static void Postfix(TSMThemeAmbidextrousRangs __instance, TowerToSimulation tower) {
            if (tower.Def.name.Contains(TowerType.MonkeyVillage) && tower.Def.tiers[0] == 5) {
                if (__instance.towerBackgroundImage.sprite == null) {
                    if (supportSprite == null) {
                        foreach (BaseTSMTheme t in TowerSelectionMenu.instance.themeManager.themes) {
                            if (t.GetIl2CppType().IsAssignableFrom(Il2CppType.Of<TSMThemeDefault>())) {
                                TSMThemeDefault td = t.Cast<TSMThemeDefault>();
                                if (td.supportSprite != null)
                                    supportSprite = td.supportSprite;
                            }
                        }
                    }
                    if (supportSprite != null) {
                        __instance.supportSprite = supportSprite;
                        __instance.towerBackgroundImage.sprite = supportSprite;
                    }
                }
                __instance.rightHandButton.gameObject.SetActive(false);
                Utils.SetTexture(__instance.leftHandButton.transform.Find("Icon").GetComponent<Image>(), 
                    AttackSwitch_Patch.old ? "BTD5" : "BTD6");
            }
        }
    }
    [HarmonyPatch(typeof(TSMThemeAmbidextrousRangs), "OnButtonPress")]
    public class AttackSwitch_Patch {
        public static bool old = false;
        public static Timer waitSwitchAttack = new Timer(3000);
        private static TowerModel btd5 = null;
        private static TowerModel GetSwitch(int t2, int t3) {
            MelonLogger.Log($"{t2} {t3}");
            TowerModel original = Utils.GetTower(TowerType.MonkeyVillage, 5, t2, t3);
            if (old) {
                if (btd5 == null) {
                    WeaponModel druidLightning = Utils.GetWeaponModel(Utils.GetTower(TowerType.Druid, 4, 0, 0), 0, 3);

                    TowerModel village = Utils.Clone(original);

                    village.behaviors = Utils.Clone(village.behaviors);
                    int attackIndex = Utils.GetAttackModelIndex(village, 1);
                    village.behaviors[attackIndex] = Utils.Clone(village.behaviors[attackIndex]);

                    AttackModel attack = village.behaviors[attackIndex].Cast<AttackModel>();
                    attack.weapons = Utils.Clone(attack.weapons);

                    WeaponModel currentWeapon = attack.weapons[0];

                    WeaponModel weapon = attack.weapons[0] = Utils.Clone(druidLightning);
                    weapon.ejectX = currentWeapon.ejectX;
                    weapon.ejectY = currentWeapon.ejectY;
                    weapon.ejectZ = currentWeapon.ejectZ;
                    weapon.rate = 1 / 60f;

                    ProjectileModel projectile = weapon.projectile = Utils.Clone(weapon.projectile);
                    projectile.pierce = 1;
                    projectile.behaviors = Utils.Clone(projectile.behaviors);
                    int damageIndex = Utils.GetBehaviorModelIndex(projectile, "Damage");
                    int filterIndex = Utils.GetBehaviorModelIndex(projectile, "Filter");
                    int lightningIndex = Utils.GetBehaviorModelIndex(projectile, "LightningModel");
                    projectile.behaviors[damageIndex] = Utils.Clone(projectile.behaviors[damageIndex]);
                    projectile.behaviors[filterIndex] = Utils.Clone(projectile.behaviors[filterIndex]);
                    projectile.behaviors[lightningIndex] = Utils.Clone(projectile.behaviors[lightningIndex]);

                    DamageModel damage = projectile.behaviors[damageIndex].Cast<DamageModel>();
                    damage.damage = 2;
                    damage.damageTypes = new string[] { "Normal" };

                    ProjectileFilterModel filter = projectile.behaviors[filterIndex].Cast<ProjectileFilterModel>();
                    projectile.filters = filter.filters =
                        new FilterModel[] { new FilterInvisibleModel("FilterInvisibleModel", false, false) };

                    LightningModel lightning = projectile.behaviors[lightningIndex].Cast<LightningModel>();
                    lightning.splits = 0;

                    btd5 = village;
                }
                return btd5;
            } else return original;
        }
        [HarmonyPostfix]
        public static void Postfix(TSMThemeAmbidextrousRangs __instance, TowerToSimulation tower) {
            if (tower.Def.name.Contains(TowerType.MonkeyVillage) && tower.Def.tiers[0] == 5) {
                old = !old;
                Utils.SetTexture(__instance.leftHandButton.transform.Find("Icon").GetComponent<Image>(), old ? "BTD5" : "BTD6");
                tower.tower.UpdateRootModel(GetSwitch(tower.Def.tiers[1], tower.Def.tiers[2]));
            }
        }
    }

    [HarmonyPatch(typeof(Tower), "OnDestroy")]
    public class OnDestroy_Patch {
        [HarmonyPostfix]
        public static void Postfix(Tower __instance) {
            if(__instance.towerModel.name.Contains(TowerType.MonkeyVillage) && __instance.towerModel.tiers[0] == 5)
                AttackSwitch_Patch.old = false;
        }
    }

    [HarmonyPatch(typeof(Game), "GetVersionString")]
    public class Update_Patch {
        private static void AddAttackSwitch(TowerModel village) => village.towerSelectionMenuThemeId = "AmbidextrousRangs";
        [HarmonyPostfix]
        public static void Postfix() {
            AddAttackSwitch(Utils.GetTower(TowerType.MonkeyVillage, 5, 0, 0));
            for(int t = 1; t < 3; t++) {
                AddAttackSwitch(Utils.GetTower(TowerType.MonkeyVillage, 5, t, 0));
                AddAttackSwitch(Utils.GetTower(TowerType.MonkeyVillage, 5, 0, t));
            }
        }
    }

    public class Utils {
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
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new UnityEngine.Vector2());
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
                if(b?.model?.name == null) return false;
                return b.model.name.Contains(partialName);
                }).ElementAtOrDefault(number);

        public static bool[] GetUsedPaths(int tier1, int tier2, int tier3) => new bool[] { tier1 > 0, tier2 > 0, tier3 > 0 };
        public static int GetPathsUsed(int tier1, int tier2, int tier3) => GetUsedPaths(tier1, tier2, tier3).Count(p => p);
        public static int GetTier(int tier1, int tier2, int tier3) => new int[] { tier1, tier2, tier3 }.Max();
    }
}