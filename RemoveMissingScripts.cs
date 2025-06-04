using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MissingScriptRemover : EditorWindow
{
    private bool includeChildren = true;
    private bool includeInactive = true;
    private Vector2 scrollPos;
    private List<GameObject> objectsWithMissingScripts = new List<GameObject>();
    private int totalMissingScriptsCount;

    [MenuItem("Tools/Missing Script Remover")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptRemover>("Script Remover");
    }

    private void OnGUI()
    {
        GUILayout.Label("Missing Script Removal Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 设置选项
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);
        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);
        
        EditorGUILayout.Space(15);
        
        // 预览部分
        GUILayout.Label("Preview", EditorStyles.boldLabel);
        if (GUILayout.Button("Find Missing Scripts in Selection"))
        {
            FindMissingScripts();
        }
        
        if (objectsWithMissingScripts.Count > 0)
        {
            EditorGUILayout.HelpBox($"Found {totalMissingScriptsCount} missing scripts in {objectsWithMissingScripts.Count} objects", MessageType.Info);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            foreach (var obj in objectsWithMissingScripts)
            {
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No missing scripts found in selection", MessageType.Info);
        }
        
        EditorGUILayout.Space(20);
        
        // 操作按钮
        EditorGUI.BeginDisabledGroup(objectsWithMissingScripts.Count == 0);
        if (GUILayout.Button("Remove Missing Scripts", GUILayout.Height(30)))
        {
            RemoveMissingScripts();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Process Prefabs in Selection", GUILayout.Height(30)))
        {
            ProcessPrefabs();
        }
        
        EditorGUILayout.HelpBox("Note: Always backup your project before performing batch operations", MessageType.Warning);
    }

    private void FindMissingScripts()
    {
        objectsWithMissingScripts.Clear();
        totalMissingScriptsCount = 0;
        
        foreach (GameObject go in Selection.gameObjects)
        {
            FindMissingScriptsInObject(go);
        }
        
        if (objectsWithMissingScripts.Count > 0)
        {
            Debug.Log($"Found {totalMissingScriptsCount} missing scripts in {objectsWithMissingScripts.Count} objects");
        }
        else
        {
            Debug.Log("No missing scripts found in selected objects");
        }
    }

    private void FindMissingScriptsInObject(GameObject go)
    {
        if (!includeInactive && !go.activeInHierarchy) return;
        
        // 检查当前物体
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (count > 0)
        {
            objectsWithMissingScripts.Add(go);
            totalMissingScriptsCount += count;
        }
        
        // 递归检查子物体
        if (includeChildren)
        {
            foreach (Transform child in go.transform)
            {
                FindMissingScriptsInObject(child.gameObject);
            }
        }
    }

    private void RemoveMissingScripts()
    {
        if (objectsWithMissingScripts.Count == 0)
        {
            Debug.LogWarning("No missing scripts to remove");
            return;
        }
        
        int removedCount = 0;
        int processedObjects = 0;
        
        foreach (GameObject go in objectsWithMissingScripts)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                removedCount += count;
                processedObjects++;
                Debug.Log($"Removed {count} missing scripts from {go.name}");
            }
        }
        
        objectsWithMissingScripts.Clear();
        totalMissingScriptsCount = 0;
        
        Debug.Log($"Successfully removed {removedCount} missing scripts from {processedObjects} objects");
        EditorUtility.DisplayDialog("Operation Complete", 
            $"Removed {removedCount} missing scripts from {processedObjects} objects", "OK");
    }

    private void ProcessPrefabs()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected");
            return;
        }
        
        int processedPrefabs = 0;
        int removedScripts = 0;
        
        foreach (GameObject go in Selection.gameObjects)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(go))
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (!string.IsNullOrEmpty(path))
                {
                    // 在预制件编辑模式下打开
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    
                    // 移除缺失脚本
                    int count = RemoveMissingInPrefab(prefabRoot);
                    removedScripts += count;
                    
                    if (count > 0)
                    {
                        // 保存预制件
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                        processedPrefabs++;
                        Debug.Log($"Removed {count} missing scripts from prefab: {path}");
                    }
                    
                    // 卸载预制件内容
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }
        
        if (processedPrefabs > 0)
        {
            Debug.Log($"Processed {processedPrefabs} prefabs, removed {removedScripts} missing scripts");
            EditorUtility.DisplayDialog("Prefab Processing Complete", 
                $"Processed {processedPrefabs} prefabs\nRemoved {removedScripts} missing scripts", "OK");
        }
        else
        {
            Debug.Log("No prefabs found in selection");
        }
    }

    private int RemoveMissingInPrefab(GameObject prefabRoot)
    {
        int count = 0;
        Stack<GameObject> stack = new Stack<GameObject>();
        stack.Push(prefabRoot);
        
        while (stack.Count > 0)
        {
            GameObject current = stack.Pop();
            
            // 移除当前物体上的缺失脚本
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(current);
            count += removed;
            
            // 添加子物体到堆栈
            foreach (Transform child in current.transform)
            {
                stack.Push(child.gameObject);
            }
        }
        
        return count;
    }
}