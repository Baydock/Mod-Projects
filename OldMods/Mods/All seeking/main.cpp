// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
// Custom injected code entry point

#include "pch.hpp"

using namespace app;

const Il2CppAssembly* assembly;

TrackTargetModel* makeSeekingBehavior(bool noOverrideRotation) {
    Il2CppClass* TrackTargetModelClass = il2cpp_class_from_name(assembly->image,
        "Assets.Scripts.Models.Towers.Projectiles.Behaviors", "TrackTargetModel");
    const MethodInfo* TrackTargetModelCtor = il2cpp_class_get_method_from_name(TrackTargetModelClass, ".ctor", 9);

    TrackTargetModel* ttm = (TrackTargetModel*)il2cpp_object_new(TrackTargetModelClass);
    BTD6API::Assembly::callFunction<void*>(TrackTargetModelCtor, ttm, (String*)il2cpp_string_new("TrackTargetModel_"),
        9999999.0f, true, false, 360.0f, true, 9999999.0f, noOverrideRotation, false);

    return ttm;
}

// because boomerang monkeys are stupid
TravelStraitModel* makeTravelStraightBehavior(FollowPathModel* fpModel) {
    Il2CppClass* TravelStraitModelClass = il2cpp_class_from_name(assembly->image,
        "Assets.Scripts.Models.Towers.Projectiles.Behaviors", "TravelStraitModel");
    const MethodInfo* TravelStraitModelCtor = il2cpp_class_get_method_from_name(TravelStraitModelClass, ".ctor", 3);

    TravelStraitModel* ttm = (TravelStraitModel*)il2cpp_object_new(TravelStraitModelClass);
    BTD6API::Assembly::callFunction<void*>(TravelStraitModelCtor, ttm, (String*)il2cpp_string_new("TravelStraitModel_"),
        fpModel->fields.speed, 2.0f);
    return ttm;
}

void makeProjectileSeeking(ProjectileModel*, bool);

void makeRecursiveProjectileSeeking(Model* model, bool noOverrideRotation) {
    std::string className = BTD6API::StringUtils::toString(model->fields.name);
    className = className.substr(0, className.find('_'));
    Il2CppClass* ResursiveModelClass = il2cpp_class_from_name(assembly->image,
        "Assets.Scripts.Models.Towers.Projectiles.Behaviors", className.c_str());
    FieldInfo* RecursiveProjectilefi = il2cpp_class_get_field_from_name(ResursiveModelClass, "projectile");
    ProjectileModel* RecursiveProjectile;
    il2cpp_field_get_value((Il2CppObject*)model, RecursiveProjectilefi, &RecursiveProjectile);
    makeProjectileSeeking(RecursiveProjectile, noOverrideRotation);
}

void makeTowerSeeking(TowerModel*);

void makeRecursiveTowerSeeking(Model* model) {
    std::string className = BTD6API::StringUtils::toString(model->fields.name);
    className = className.substr(0, className.find('_'));
    Il2CppClass* ResursiveModelClass = il2cpp_class_from_name(assembly->image,
        "Assets.Scripts.Models.Towers.Projectiles.Behaviors", className.c_str());
    FieldInfo* RecursiveTowerfi = il2cpp_class_get_field_from_name(ResursiveModelClass, "tower");
    TowerModel* RecursiveTower;
    il2cpp_field_get_value((Il2CppObject*)model, RecursiveTowerfi, &RecursiveTower);
    makeTowerSeeking(RecursiveTower);
}

void makeProjectileSeeking(ProjectileModel* projectile, bool noOverrideRotation) {
    Model__Array* modelsArr = projectile->fields.behaviors;
    Model** models = modelsArr->vector;
    int seekingBehaviorIndex = -1;
    if (models != NULL) {
        for (int i = 0; i < modelsArr->max_length; ++i) {
            Model* model = models[i];
            if (model != NULL && model->fields.name != NULL) {
                std::wstring name = BTD6API::StringUtils::toWideString(model->fields.name);
                if (name.find(L"TrackTarget") != std::wstring::npos)
                    seekingBehaviorIndex = i;

                if (name.find(L"FollowPath") != std::wstring::npos)
                    models[i] = (Model*)makeTravelStraightBehavior((FollowPathModel*)model);

                if (name.find(L"Rotate") != std::wstring::npos)
                    noOverrideRotation = true;

                if (name.find(L"CreateProjectile") != std::wstring::npos ||
                    name.find(L"Emit") != std::wstring::npos)
                    makeRecursiveProjectileSeeking(model, noOverrideRotation);
                
                if (name.find(L"CreateTower") != std::wstring::npos) {
                    makeRecursiveTowerSeeking(model);
                    return;
                }

                if (name.find(L"TravelAlongPath") != std::wstring::npos ||
                    name.find(L"Pickup") != std::wstring::npos ||
                    name.find(L"MoabTakedownModel") != std::wstring::npos)
                    return;
            }
        }
    }
    if (seekingBehaviorIndex == -1) {
        BTD6API::Array::resize(modelsArr, modelsArr->max_length + 1);
        projectile->fields.behaviors = modelsArr;
        models = modelsArr->vector;
        seekingBehaviorIndex = modelsArr->max_length - 1;
    }
    models[seekingBehaviorIndex] = (Model*)makeSeekingBehavior(noOverrideRotation);
}

void makeWeaponSeeking(WeaponModel* weaponModel) {
    Model__Array* modelsArr = (Model__Array*)weaponModel->fields.behaviors;
    Model** models = modelsArr->vector;
    bool noOverrideRotation = false;
    if (modelsArr != NULL) {
        for (int i = 0; i < modelsArr->max_length; i++) {
            std::wstring name = BTD6API::StringUtils::toWideString(models[i]->fields.name);
            if (name.find(L"ZeroRotation") != std::wstring::npos)
                noOverrideRotation = true;
        }
    }
    makeProjectileSeeking(weaponModel->fields.projectile, noOverrideRotation);
}

void makeAttackSeeking(AttackModel* attackModel) {
    Model__Array* modelsArr = attackModel->fields.behaviors;
    Model** models = modelsArr->vector;
    bool notBloonTargeting = false;
    if (modelsArr != NULL) {
        for (int i = 0; i < modelsArr->max_length && !notBloonTargeting; i++) {
            std::wstring name = BTD6API::StringUtils::toWideString(models[i]->fields.name);
            if (name.find(L"TargetTrack") != std::wstring::npos ||
                name.find(L"TargetFriendly") != std::wstring::npos ||
                name.find(L"BrewTargetting") != std::wstring::npos ||
                name.find(L"RandomPosition") != std::wstring::npos)
                notBloonTargeting = true;
        }
    }
    if (!notBloonTargeting) {
        WeaponModel__Array* weaponsArr = attackModel->fields.weapons;
        WeaponModel** weapons = weaponsArr->vector;
        for (int i = 0; i < weaponsArr->max_length; ++i) {
            WeaponModel* weapon = weapons[i];
            if (weapon != NULL)
                makeWeaponSeeking(weapon);
        }
    }
}

void makeAbilitySeeking(AbilityModel* abilityModel) {
    Model__Array* modelsArr = abilityModel->fields.behaviors;
    Model** models = modelsArr->vector;
    for (int i = 0; i < modelsArr->max_length; ++i) {
        Model* model = models[i];
        std::wstring name = BTD6API::StringUtils::toWideString(model->fields.name);

        if (name.find(L"UCAVModel") != std::wstring::npos) {
            UCAVModel* mdl = (UCAVModel*)(model);
            makeTowerSeeking(mdl->fields.uavTowerModel);
            makeTowerSeeking(mdl->fields.ucavTowerModel);
        }

        if (name.find(L"DroneSwarmModel") != std::wstring::npos) {
            DroneSwarmModel* dsm = (DroneSwarmModel*)(model);
            makeTowerSeeking(dsm->fields.droneSupportModel->fields.droneModel);
        }

        if (name.find(L"ActivateAttackModel") != std::wstring::npos) {
            ActivateAttackModel* aaModel = (ActivateAttackModel*)model;

            AttackModel__Array* aaModelsArr = aaModel->fields.attacks;
            AttackModel** attacks = aaModelsArr->vector;
            for (int j = 0; j < aaModelsArr->max_length; j++)
                makeAttackSeeking(attacks[j]);
        }
    }
}

void makeTempleTowerMutatorGroupSeeking(TowerMutatorGroupModel* tmgModel) {
    TowerMutatorModel__Array* tmModelsArr = tmgModel->fields.mutators;
    TowerMutatorModel** tmModels = tmModelsArr->vector;
    if (tmModelsArr != NULL) {
        for (int i = 0; i < tmModelsArr->max_length; i++) {
            std::wstring name = BTD6API::StringUtils::toWideString(((Model*)tmModels[i])->fields.name);
            if (name.find(L"AddAttackTowerMutatorModel") != std::wstring::npos)
                makeAttackSeeking((AttackModel*)((AddAttackTowerMutatorModel*)tmModels[i])->fields.attackModel);
        }
    }
}

void makeTowerSeeking(TowerModel* tmdl) {
    Model__Array* modelsArr = tmdl->fields.behaviors;
    Model** models = modelsArr->vector;
    if (models != NULL) {
        for (int i = 0; i < modelsArr->max_length; ++i) {
            Model* model = models[i];
            if (model != NULL && model->fields.name != NULL) {

                std::wstring name = BTD6API::StringUtils::toWideString(model->fields.name);

                if (name.find(L"AbilityModel") != std::wstring::npos)
                    makeAbilitySeeking((AbilityModel*)model);

                if (name.find(L"DroneSupportModel") != std::wstring::npos)
                    makeTowerSeeking(((DroneSupportModel*)model)->fields.droneModel);

                if (name.find(L"ComancheDefenceModel") != std::wstring::npos)
                    makeTowerSeeking(((ComancheDefenceModel*)model)->fields.towerModel);

                if (name.find(L"TempleTowerMutatorGroup") != std::wstring::npos)
                    makeTempleTowerMutatorGroupSeeking((TowerMutatorGroupModel*)model);

                if (name.find(L"AttackModel") != std::wstring::npos || name.find(L"AttackAirUnitModel") != std::wstring::npos)
                    makeAttackSeeking((AttackModel*)model);
            }
        }
    }
}


void makeSeeking(GameModel* gmdl, const std::string& where)
{
    TowerModel__Array* towersArr = gmdl->fields.towers;
    TowerModel** towers = towersArr->vector;

    for (int i = 0; i < towersArr->max_length; ++i)
        if (towers[i]->fields.display != NULL && towers[i]->fields.baseId != NULL)
            makeTowerSeeking(towers[i]);

    std::cout << "Made projectiles seeking (" << where << ")" << std::endl;
}

// Injected code entry point
void Run()
{
    AllocConsole();
    freopen_s((FILE**)stdout, "CONOUT$", "w", stdout);

    std::cout << "Initializing..." << std::endl;

    size_t size = 0;
    const Il2CppAssembly** assemblies = il2cpp_domain_get_assemblies(nullptr, &size);

    assembly = BTD6API::Assembly::get(assemblies, "Assembly-CSharp", size);

    if (assembly == nullptr)
    {
        std::cout << "Error: Assembly-CSharp not found." << std::endl;
        return;
    }

    // do in-game patches (will need to patch InGame if user is currently in-game, just patching Game will do nothing in that case)
    Il2CppClass* inGameClass = il2cpp_class_from_name(assembly->image, "Assets.Scripts.Unity.UI_New.InGame", "InGame");
    FieldInfo* inGameInstanceInfo = il2cpp_class_get_field_from_name(inGameClass, "instance");
    InGame* inGameInstAddr = 0;
    il2cpp_field_static_get_value(inGameInstanceInfo, &inGameInstAddr);

    if (inGameInstAddr != NULL)
    {
        InGame* inGameInstance = (InGame*)inGameInstAddr;
        makeSeeking(inGameInstance->fields.bridge->fields.simulation->fields.model, "in-game");
    }
    // game patches
    Il2CppClass* gameClass = il2cpp_class_from_name(assembly->image, "Assets.Scripts.Unity", "Game");
    FieldInfo* gameInstanceInfo = il2cpp_class_get_field_from_name(gameClass, "instance");
    Game* gameInstAddr = 0;
    il2cpp_field_static_get_value(gameInstanceInfo, &gameInstAddr);

    if (gameInstAddr == NULL)
    {
        std::cout << "Some error occurred when trying to access the game model." << std::endl;
        return;
    }

    Game* gameInstance = (Game*)gameInstAddr;
    makeSeeking(gameInstance->fields.model, "game");
}