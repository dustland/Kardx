using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<FindMissingScripts>("Find Missing Scripts");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in Scene"))
        {
            FindInScene();
        }

        if (GUILayout.Button("Find Missing Scripts in Selected GameObjects"))
        {
            FindInSelected();
        }
    }

    private void FindInScene()
    {
        int missingCount = 0;
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            missingCount += FindInGO(go);
        }

        Debug.Log($"Found {missingCount} missing script references in the scene.");
    }

    private void FindInSelected()
    {
        int missingCount = 0;
        GameObject[] selectedObjects = Selection.gameObjects;

        foreach (GameObject go in selectedObjects)
        {
            missingCount += FindInGO(go);
        }

        Debug.Log($"Found {missingCount} missing script references in selected GameObjects.");
    }

    private int FindInGO(GameObject go)
    {
        int missingCount = 0;
        Component[] components = go.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.Log($"Missing script found on GameObject: {go.name}", go);
                missingCount++;
            }
        }

        return missingCount;
    }
}
