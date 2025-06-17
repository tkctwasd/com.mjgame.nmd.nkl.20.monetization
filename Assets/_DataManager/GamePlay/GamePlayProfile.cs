using System;
using System.Collections.Generic;

[Serializable]
public class GamePlayProfile
{
    public int Map;
    public int TimePlayGame;
    public bool Tutorials;
    public List<int> ListCheckPointEachMap = new List<int>();

    public void OnChangedCheckPointInMap(int idCheckPoint, Action acOnChanged = null)
    {
        if (idCheckPoint > ListCheckPointEachMap[Map])
        {
            ListCheckPointEachMap[Map] = idCheckPoint;
            acOnChanged?.Invoke();
        }
    }
    public void IncreseCheckPointInMap()
    {
        ListCheckPointEachMap[Map]++;
    }
    public int CheckPointCurrent() { return ListCheckPointEachMap[Map]; }
    public void SetTutorials(bool isTutorial)
    {
        Tutorials = isTutorial;
    }
    public void ResetMap()
    {
        TimePlayGame = 0;
        Tutorials = false;
        for (int i = 0; i < ListCheckPointEachMap.Count; i++)
        {
            ListCheckPointEachMap[i] = -1;
        }
    }
    public GamePlayProfile()
    {
        Map = 1;
        TimePlayGame = 0;
        Tutorials = false;
    }
}