// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
// Custom injected code entry point
// Edited by Baydock for the Bloon Speed Adjuster Mod

#include "pch.hpp"

using namespace app;

const Il2CppAssembly* assembly;

struct bloonStruct {
    std::string name;
    bool isMoab = false;
    bloonStruct(std::string name, bool isMoab = false) : name(name), isMoab(isMoab) {}
};

bloonStruct bloonList[17] = {
    bloonStruct("Red"),
    bloonStruct("Blue"),
    bloonStruct("Green"),
    bloonStruct("Yellow"),
    bloonStruct("Pink"),
    bloonStruct("Black"),
    bloonStruct("White"),
    bloonStruct("Purple"),
    bloonStruct("Lead"),
    bloonStruct("Zebra"),
    bloonStruct("Rainbow"),
    bloonStruct("Ceramic"),
    bloonStruct("Moab", true),
    bloonStruct("Bfb", true),
    bloonStruct("Zomg", true),
    bloonStruct("Ddt", true),
    bloonStruct("Bad", true)
};

void AdjustBloonSpeeds(GameModel* gmdl, const std::string& where) {
    //"Adjusting Bloon Speeds (" << where << ")"

    BloonModel__Array* bloonModelArr = gmdl->fields.bloons;
    BloonModel** bloonModels = bloonModelArr->vector;

    for (int i = 0; i < bloonModelArr->max_length; ++i)
        if (bloonModels[i] != NULL && bloonModels[i]->fields.display != NULL)
            if (bloonModels[i]->fields.layerNumber == 1)
                bloonModels[i]->fields.speed += 100;



    //"Bloon Speeds Adjusted"
}

bool Start() {
    //"Initializing..."

    size_t size = 0;
    const Il2CppAssembly** assemblies = il2cpp_domain_get_assemblies(nullptr, &size);

    assembly = BTD6API::Assembly::get(assemblies, "Assembly-CSharp", size);

    if (assembly == nullptr) {
        //Error: Assembly-CSharp not found.
        return false;
    }

    return true;
}

// Injected code entry point
bool Run() {
    // do in-game patches (will need to patch InGame if user is currently in-game, just patching Game will do nothing in that case)
    Il2CppClass* inGameClass = il2cpp_class_from_name(assembly->image, "Assets.Scripts.Unity.UI_New.InGame", "InGame");
    FieldInfo* inGameInstanceInfo = il2cpp_class_get_field_from_name(inGameClass, "instance");
    InGame* inGameInstAddr = 0;
    il2cpp_field_static_get_value(inGameInstanceInfo, &inGameInstAddr);

    if (inGameInstAddr != NULL) {
        InGame* inGameInstance = (InGame*)inGameInstAddr;
        AdjustBloonSpeeds(inGameInstance->fields.bridge->fields.simulation->fields.model, "in-game");
    }
    // game patches
    Il2CppClass* gameClass = il2cpp_class_from_name(assembly->image, "Assets.Scripts.Unity", "Game");
    FieldInfo* gameInstanceInfo = il2cpp_class_get_field_from_name(gameClass, "instance");
    Game* gameInstAddr = 0;
    il2cpp_field_static_get_value(gameInstanceInfo, &gameInstAddr);

    if (gameInstAddr == NULL) {
        //Some error occurred when trying to access the game model.
        return false;
    }

    Game* gameInstance = (Game*)gameInstAddr;
    AdjustBloonSpeeds(gameInstance->fields.model, "game");

    return true;
}