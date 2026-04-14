using UnityEditor;
using UnityEngine;

/// <summary>
/// Cleans "Missing (Script)" slots that Unity keeps on GameObjects (e.g. after rename/delete).
/// </summary>
public static class RemoveMissingScriptsHelper
{
    [MenuItem("Tools/StreetSyndicate/Remove Missing Scripts on GameManager")]
    private static void RemoveOnGameManager()
    {
        GameObject go = GameObject.Find("GameManager");
        if (go == null)
        {
            Debug.LogWarning("RemoveMissingScriptsHelper: No GameObject named GameManager in the loaded scene.");
            return;
        }

        int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (before == 0)
        {
            Debug.Log("GameManager: no missing script components.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts on GameManager");
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        Debug.Log("GameManager: removed " + removed + " missing script slot(s). Save the scene (Ctrl+S).");
    }
}
