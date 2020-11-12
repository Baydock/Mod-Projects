// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
// Custom injected code entry point

#include "pch.hpp"
#include <iostream>

using namespace app;

void MakeRedsReallyFast(GameModel* gmdl, const std::string& where) {
    std::cout << "Making reds really fast! (" << where << ")" << std::endl;

    BloonModel__Array* bloonModelArr = gmdl->fields.bloons;
    BloonModel** bloonModels = bloonModelArr->vector;

    for (int i = 0; i < bloonModelArr->max_length; ++i)
        if (bloonModels[i] != NULL && bloonModels[i]->fields.display != NULL)
            if (BTD6API::StringUtils::toString(bloonModels[i]->fields.baseId) == "Red") {
                std::cout << "Before: " << bloonModels[i]->fields.speed << ", ";
                bloonModels[i]->fields.speed += 100;
                std::cout << "After: " << bloonModels[i]->fields.speed << std::endl;
            }

    std::cout << "Reds are really fast!" << std::endl;
}

// Injected code entry point
void Run()
{
    AllocConsole();
    freopen_s((FILE**)stdout, "CONOUT$", "w", stdout);

    std::cout << "Initializing..." << std::endl;

    size_t size = 0;
    const Il2CppAssembly** assemblies = il2cpp_domain_get_assemblies(nullptr, &size);

    const Il2CppAssembly* assembly = BTD6API::Assembly::get(assemblies, "Assembly-CSharp", size);

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
        MakeRedsReallyFast(inGameInstance->fields.bridge->fields.simulation->fields.model, "in-game");
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
    MakeRedsReallyFast(gameInstance->fields.model, "game");
}