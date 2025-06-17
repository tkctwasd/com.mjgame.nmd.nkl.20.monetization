using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    public bool Sound;
    public bool Light;
    public PlayerProfile()
    {
        Sound = true;
        Light = true;
    }
}

[Serializable]
public class ItemInGameData
{
    public string Id;
    public Vector3 Position;
}
