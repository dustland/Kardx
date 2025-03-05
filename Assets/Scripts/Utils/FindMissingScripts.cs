using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Kardx.Utils
{
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
                FindMissingScriptsInScene();
            }

            if (GUILayout.Button("Find Missing Scripts in Prefabs"))
            {
                FindMissingScriptsInPrefabs();
            }
        }

        private void FindMissingScriptsInScene()
        {
            int missingCount = 0;
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            
            foreach (GameObject go in rootObjects)
            {
                missingCount += FindMissingScriptsInGameObject(go);
            }
            
            Debug.Log($"Found {missingCount} GameObjects with missing script references in the current scene");
        }

        private void FindMissingScriptsInPrefabs()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
            int missingCount = 0;
            
            foreach (string guid in prefabPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                int count = FindMissingScriptsInGameObject(prefab);
                if (count > 0)
                {
                    Debug.Log($"Prefab at {path} has {count} missing script references");
                    missingCount += count;
                }
            }
            
            Debug.Log($"Found {missingCount} prefabs with missing script references");
        }

        private int FindMissingScriptsInGameObject(GameObject go)
        {
            int missingCount = 0;
            Component[] components = go.GetComponents<Component>();
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.Log($"GameObject {go.name} has a missing script at index {i}", go);
                    missingCount++;
                }
            }
            
            foreach (Transform child in go.transform)
            {
                missingCount += FindMissingScriptsInGameObject(child.gameObject);
            }
            
            return missingCount;
        }
    }
}
