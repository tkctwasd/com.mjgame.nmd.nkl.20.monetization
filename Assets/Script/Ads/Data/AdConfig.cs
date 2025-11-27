using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class AdsConfig
{
    public bool ShowInfo = true;
    public bool ShowAppOpen = true;
    public bool ShowInterstitial = true;
    public bool ShowBanner = true;
    public bool ShowReward = true;
    public int InterstitialCappingTime = 45;
    public int AppOpenCappingTime = 60;
    public int InterstitialIndexStart = 2;
    public int TimestampInterReward = 2;
}

[Serializable]
public class AdsPopupSetting
{
    public bool btnClose = false;
    public bool btnReset = false;
}

public static class RemoteKey
{
    public static string AdsConfig = "AdsConfig";
    public static string AdsPopupSetting = "AdsPopupSetting";
    public static string ReviewUI = "ReviewUI";
    public static string ReviewMap = "ReviewMap";
    public static string Menu = "Menu";
    public static string PopupRate = "PopupRate";
    public static string ReviewSkill = "ReviewSkill";
    public static string MapLocked = "MapLocked";
    public static string ButtonUI = "ButtonUI";
    public static string AdBonus = "AdBonus";
    public static string BotAI = "BotAI";
    public static string EnemySetting = "EnemySetting";
}

[Serializable]
public class MenuSelect
{
    public List<string> NameMap = new List<string>();
}

[Serializable]
public class PopupRate
{
    public int IndexShowRate = 4;
    public int MaxShowCount = 5;
    public bool CanShowRate = false;
}

[Serializable]
public class MapLocked
{
    public List<bool> Lock = new List<bool>();
}

[Serializable]
public class ButtonUI
{
    public bool Daily = true;

}
[Serializable]
public class AdBonus
{
    public int TimeDelay = 90;
    public int CappingTime = 60;
    public bool CanShow = true;
    public bool CheckShowInter = true;
}


[Serializable]
public class BotAISetting
{
    public float ventPickupChance = 0.3f;

    public float ventDetectRange = 5f;

    public int checkVentEvery = 3;

    public int maxVentBeforePlacing = 2;

    public bool prioritizePlacingVents = true;

    public float hideChance = 0.7f;

    public float hideTimeMin = 10f;

    public float hideTimeMax = 30f;

    public float postHideVentSearchChance = 0.7f;

    public float policeProneChance = 0.6f;

    public float randomProneChance = 0.4f;
    public Vector2 randomProneInterval = new Vector2(30f, 45f);

    public float reactionTime = 1f;

    public int intelligenceLevel = 50;
}

[Serializable]
public class BotAI
{
    public BotAISetting VeryEasy;
    public BotAISetting Medium;
    public BotAISetting VeryHard;
}





[Serializable]
public class DifficultySetting
{
    public float timeStart;
    public float playerChance = 0.5f;
    public float speedMultiplier = 1.0f;
    public float timeEnemySleep = 30f;
    public float timeEnemyWake = 30f;
}


[Serializable]
public class Difficulty
{
    public List<DifficultySetting> difficulties;
    public DifficultySetting GetDifficultyStage(int index)
    {
        index = Math.Clamp(index, 0, difficulties.Count - 1);
        return difficulties[index];
    }
}

[Serializable]
public class EnemySetting
{
    public Difficulty VeryEasy;
    public Difficulty Medium;
    public Difficulty VeryHard;
}