using System;
using System.Collections.Generic;
using UnityEngine;

// Config remote rieng cua game, doc qua RemoteConfigManager.GetConfigValue<T>(GameRemoteKey.X).
public static class GameRemoteKey
{
    public static string BotAISetting = "BotAISetting";
    public static string MenuConfig = "MenuConfig";
    public static string Treadmills = "Treadmills";
    public static string Tutorial = "Tutorial";
}

[Serializable]
public class BotAISetting
{
    public float SpeedMin = 3f;
    public float SpeedMax = 3f;
    public float PatrolWaitMin = 0.5f;
    public float PatrolWaitMax = 2f;
    [Min(1)]
    public int PatrolCountToDecide = 5;
    [Range(0f, 1f)]
    public float ForageChance = 0.8f;
    public float BrainrotSearchRadius = 3f;
    [Range(0f, 1f)]
    public float BrainrotFindChance = 0.65f;
    [Min(1)]
    public int MaxSearchFailCount = 5;
    public float WaitAtHomeMin = 1f;
    public float WaitAtHomeMax = 2f;
}

[Serializable]
public class MenuConfig
{
    public bool OpenStart = false;
    public List<ItemMenuConfig> NameMap = new List<ItemMenuConfig>();
}

[Serializable]
public class ItemMenuConfig
{
    public string name;
    public bool reward;
}


[Serializable]
public class Treadmills
{
    public List<int> Data = new();
}


[Serializable]
public class Tutorial
{
    public bool ChargeEnergy = false;
}