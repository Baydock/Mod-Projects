using Assets.Scripts.Models;
using Assets.Scripts.Models.Profile;
using Assets.Scripts.Models.Towers;
using Assets.Scripts.Models.Towers.Behaviors.Abilities;
using Assets.Scripts.Models.Towers.Behaviors.Attack;
using Assets.Scripts.Models.Towers.Projectiles;
using Assets.Scripts.Models.Towers.Upgrades;
using Assets.Scripts.Models.Towers.Weapons;
using Assets.Scripts.Models.TowerSets;
using Assets.Scripts.Simulation.Objects;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Localization;
using Assets.Scripts.Unity.UI_New.Main.HeroSelect;
using Assets.Scripts.Unity.UI_New.Transitions;
using Assets.Scripts.Utils;
using Harmony;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System.Linq;
using UnhollowerBaseLib;
using TowerDictionary = System.Collections.Generic.Dictionary<int, Assets.Scripts.Models.Towers.TowerModel>;
using UpgradeDictionary = System.Collections.Generic.Dictionary<int, Assets.Scripts.Models.Towers.Upgrades.UpgradeModel>;

namespace LaserHero {

    public class LaserHeroMod : MelonMod { }

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

            added = added || AddIfNotContained(unlockedTowers, LaserHero.BaseName);

            for (int l = 1; l < 1; l++) added = added || AddIfNotContained(aquiredUpgrades, LaserHero.GetUpgradeName(l));

            if (added)
                MelonLogger.Log($"Unlocked {LaserHero.DisplayName}");
        }
    }

    [HarmonyPatch(typeof(Game), "GetVersionString")] // this method is called soon after the game is done initializing the models, hence why it's used to modify said models
    public class GameModel_Patch {
        [HarmonyPostfix]
        public static void Postfix() {
            if (Game.instance.model.towers.ToArray().Any(tower => tower.name.Contains(LaserHero.BaseName)))
                return;

            //for (int l = 1; l < 21; l++) LaserHero.Add(l);

            LaserHero.Add(1);

            MelonLogger.Log($"Made {LaserHero.DisplayName}");
        }
    }

    [HarmonyPatch(typeof(HeroSelectTransition), "OnEnable")]
    public class HeroSelect_Patch {
        [HarmonyPostfix]
        public static void Postfix(HeroSelectTransition __instance) {
            MelonLogger.Log(__instance.sortedTowers.Count);
            HeroDetailsModel details = new HeroDetailsModel(LaserHero.BaseName, __instance.heroButtons.Count, 1, 1, 0, 0, null, false);
            __instance.sortedTowers.Add(new HeroSelectTransition.TowerDetailsModelSort() { heroDetails = details, unlockLevel = 0 });
            HeroButton button = UnityEngine.Object.Instantiate(__instance.heroButtonPrefab);
            button.transform.parent = __instance.transform;
            button.Init(details.towerId, 0, false, __instance);
            button.gameObject.SetActive(true);
            __instance.heroButtons.Add(button);
        }
    }
    [HarmonyPatch(typeof(HeroUpgradeDetails), "SetDetails")]
    public class HeroShowDetails_Patch {
        [HarmonyPostfix]
        public static void Postfix(HeroUpgradeDetails __instance, string heroIdToUse, bool showSelect = true, bool inGame = false, TowerToSimulation selectedHero = null) {
            if (heroIdToUse.Contains(LaserHero.BaseName)) {
                if (__instance.buyHeroButton.active) {
                    __instance.buyHeroButton.SetActive(false);
                    __instance.selectButton.gameObject.SetActive(true);
                }
                __instance.UpdateAbilities(LaserHero.GetTower(1));
                __instance.Update
            }
        }
    }

    public static class Utils {
        /*private static AssetBundleDictionary AssetBundleCache { get; } = new AssetBundleDictionary() {
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
        }*/

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
            bs.GetBackingList().ToArray().Where(b => b.model.name.Contains(partialName)).ElementAtOrDefault(number);

        public static bool[] GetUsedPaths(int tier1, int tier2, int tier3) => new bool[] { tier1 > 0, tier2 > 0, tier3 > 0 };
        public static bool[] GetUsedPaths(int[] tiers) => GetUsedPaths(tiers[0], tiers[1], tiers[2]);
        public static int GetPathsUsed(int tier1, int tier2, int tier3) => GetUsedPaths(tier1, tier2, tier3).Count(p => p);
        public static int GetPathsUsed(int[] tiers) => GetPathsUsed(tiers[0], tiers[1], tiers[2]);
        public static int GetTier(int tier1, int tier2, int tier3) => new int[] { tier1, tier2, tier3 }.Max();
        public static int GetTier(int[] tiers) => GetTier(tiers[0], tiers[1], tiers[2]);
    }

    public class LaserHero {
        public static string DisplayName => "Laser Hero";
        public static string BaseName => "LaserHero";
        public static string InsertAfter => TowerType.MortarMonkey;
        public static string InsertBefore => TowerType.WizardMonkey;
        public static string GetName(int level) => $"{BaseName}-{level}";
        public static string GetUpgradeName(int level) => $"{BaseName}-Upgrade-{level}";
        public static string GetLevelDescription(int level) => level switch {
            1 => "1",
            2 => "2",
            3 => "3",
            4 => "4",
            5 => "5",
            6 => "6",
            7 => "7",
            8 => "8",
            9 => "9",
            10 => "10",
            11 => "11",
            12 => "12",
            13 => "13",
            14 => "14",
            15 => "15",
            16 => "16",
            17 => "17",
            18 => "18",
            19 => "19",
            20 => "20",
            _ => default
        };
        private static TowerDictionary TowerCache { get; } = new TowerDictionary() {
            { 1, Get1() }
        };
        private static UpgradeDictionary UpgradeCache { get; } = new UpgradeDictionary() {
        };

        public static TowerModel GetTower(int level) => TowerCache[level];
        public static UpgradeModel GetUpgrade(int level) => UpgradeCache[level];
        private static UpgradeModel GetUpgrade(int level, int cost, int xpCost, SpriteReference icon, int locked,
                                               string confirmation, string localizedNameOverride) =>
            new UpgradeModel(GetUpgradeName(level), cost, xpCost, icon, 0, level - 1, locked, confirmation, localizedNameOverride);
        private static UpgradePathModel GetUpgradePath(int level) =>
            new UpgradePathModel(GetUpgradeName(level), GetName(level), 1, level);
        private static TowerModel GetTowerCommonSet(TowerModel tower, int level) {
            tower.name = GetName(level);
            tower.tier = level;
            tower.tiers = new int[] { level, 0, 0 };

            string[] appliedUpgrades = new string[level - 1];
            for (int t = 2, i = 0; t <= level; t++, i++)
                appliedUpgrades[i] = GetUpgradeName(t);
            tower.appliedUpgrades = appliedUpgrades;

            tower.upgrades = new UpgradePathModel[] { GetUpgradePath(level) };

            return tower;
        }

        public static void Add(int level) {
            Utils.AddTowerModel(GetTower(level));
            Utils.AddTextChange(GetUpgradeName(level), GetUpgradeName(level));
            Utils.AddTextChange($"{GetUpgradeName(level)} Description", GetLevelDescription(level));
            if(level > 1) {
                Utils.AddUpgradeModel(GetUpgrade(level));
            }
        }

        private static TowerModel Get1() {
            TowerModel quin = Utils.GetTower(TowerType.Quincy, 0, 0, 0);

            TowerModel laserHero1 = Utils.Clone(quin);
            laserHero1.baseId = laserHero1.name = BaseName;

            return laserHero1;
        }
    }
}