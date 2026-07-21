using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public partial class DataGamePlay
{
    public double Money;
    public int Gem;
    public int Shield;

    public void AddMoney(double money)
    {
        Money += money;
    }
    public void AddGem(int gem)
    {
        Gem += gem;
    }
    public void AddShield(int shield)
    {
        Shield += shield;
    }
}