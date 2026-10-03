#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class IAPSymbolEditor
{

    [MenuItem("Tools/IAP/Toggle USE_IAP")]
    public static void ToggleIAP() => ToggleSymbol("USE_IAP");

    private static void ToggleSymbol(string symbol)
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').ToList();

        if (defines.Contains(symbol)) defines.Remove(symbol);
        else defines.Add(symbol);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        Debug.Log($"Toggled {symbol}, new define symbols: {string.Join(";", defines)}");
    }
}
#endif