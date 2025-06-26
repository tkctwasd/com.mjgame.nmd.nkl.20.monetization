namespace A
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public static class LogMessage
    {
        public static void Log(string content, string location = "")
        {
            Debug.Log($"<color=yellow>[WASD]</color> <color=green>[{location}]</color>: " + content);
        }
    }

}