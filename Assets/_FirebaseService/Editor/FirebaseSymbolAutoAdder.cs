using UnityEditor;

[InitializeOnLoad]
public static class FirebaseSymbolAutoAdder
{
    private const string Symbol = "FIREBASE_ANALYTICS";

    static FirebaseSymbolAutoAdder()
    {
        AddSymbolIfMissing(BuildTargetGroup.Android);
        AddSymbolIfMissing(BuildTargetGroup.iOS);
    }

    static void AddSymbolIfMissing(BuildTargetGroup targetGroup)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
        if (!defines.Contains(Symbol))
        {
            defines = string.IsNullOrEmpty(defines) ? Symbol : defines + ";" + Symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
            UnityEngine.Debug.Log($"[Firebase] Added define symbol '{Symbol}' for {targetGroup}");
        }
    }
}
