using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LogW
{
    public static void Log(string content, string location = "")
    {
        Debug.Log($"[WASD][{location}]: " + content);
    }
}
