using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MaterialBatchEditor : EditorWindow
{
    private List<Material> materials = new List<Material>();
    private Vector2 scrollPosition;
    private Vector2 propertiesScroll;
    private Dictionary<string, ShaderProperty> properties = new Dictionary<string, ShaderProperty>();
    private bool showFloatProperties = false; // 默认折叠
    private bool showColorProperties = false;
    private bool showTextureProperties = false;
    private bool showVectorProperties = false;
    private bool showBooleanProperties = false;

    // 存储每个属性的编辑状态
    private Dictionary<string, bool> propertyEdited = new Dictionary<string, bool>();

    private class ShaderProperty
    {
        public string name;
        public ShaderUtil.ShaderPropertyType type;
        public object defaultValue;
        public bool isBoolean = false;
    }

    [MenuItem("Tools/Material Batch Editor")]
    public static void ShowWindow()
    {
        GetWindow<MaterialBatchEditor>("材质批量编辑器");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("材质批量编辑器", EditorStyles.boldLabel);
        GUILayout.Label("拖放材质球到下方区域进行批量修改", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        Rect dropArea = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖放材质球到这里", EditorStyles.helpBox);
        
        HandleDragAndDrop(dropArea);
        
        if (materials.Count > 0)
        {
            GUILayout.Label($"已选择 {materials.Count} 个材质球:", EditorStyles.boldLabel);
            
            // 材质列表滚动区域
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Vector2 materialsScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(100));
            foreach (var mat in materials)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(mat, typeof(Material), false);
                
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    materials.Remove(mat);
                    CacheCommonProperties();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("清空列表", GUILayout.Height(30)))
            {
                materials.Clear();
                properties.Clear();
                propertyEdited.Clear();
            }
            
            GUILayout.Space(20);
            
            if (properties.Count > 0)
            {
                // 属性编辑区域整体滚动
                propertiesScroll = EditorGUILayout.BeginScrollView(propertiesScroll);
                
                DisplayPropertyEditors();
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("没有检测到共同的Shader属性", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请拖放材质球到上方区域", MessageType.Info);
        }
    }

    void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        Material mat = obj as Material;
                        if (mat != null && !materials.Contains(mat))
                        {
                            materials.Add(mat);
                        }
                    }
                    
                    CacheCommonProperties();
                }
                break;
        }
    }

    void CacheCommonProperties()
    {
        properties.Clear();
        propertyEdited.Clear();
        
        if (materials.Count == 0)
            return;
        
        var firstMat = materials[0];
        int propertyCount = ShaderUtil.GetPropertyCount(firstMat.shader);
        
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = ShaderUtil.GetPropertyName(firstMat.shader, i);
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(firstMat.shader, i);
            
            bool allHaveProperty = true;
            foreach (var mat in materials)
            {
                if (!mat.HasProperty(propName))
                {
                    allHaveProperty = false;
                    break;
                }
            }
            
            if (allHaveProperty)
            {
                ShaderProperty prop = new ShaderProperty
                {
                    name = propName,
                    type = propType,
                    defaultValue = GetPropertyValue(firstMat, propName, propType)
                };
                
                prop.isBoolean = IsBooleanProperty(propName, propType);
                properties[propName] = prop;
                
                // 初始化编辑状态为未编辑
                propertyEdited[propName] = false;
            }
        }
    }
    
    bool IsBooleanProperty(string propName, ShaderUtil.ShaderPropertyType type)
    {
        if (type != ShaderUtil.ShaderPropertyType.Float && 
            type != ShaderUtil.ShaderPropertyType.Range)
            return false;
        
        string lowerName = propName.ToLower();
        return lowerName.Contains("enable") || 
               lowerName.Contains("toggle") || 
               lowerName.Contains("use") || 
               lowerName.Contains("on") || 
               lowerName.Contains("is");
    }

    object GetPropertyValue(Material mat, string propName, ShaderUtil.ShaderPropertyType type)
    {
        switch (type)
        {
            case ShaderUtil.ShaderPropertyType.Float:
            case ShaderUtil.ShaderPropertyType.Range:
                return mat.GetFloat(propName);
            case ShaderUtil.ShaderPropertyType.Color:
                return mat.GetColor(propName);
            case ShaderUtil.ShaderPropertyType.Vector:
                return mat.GetVector(propName);
            case ShaderUtil.ShaderPropertyType.TexEnv:
                return mat.GetTexture(propName);
            default:
                return null;
        }
    }

    void DisplayPropertyEditors()
    {
        GUILayout.Label("Shader属性编辑", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("只修改您编辑过的属性（带绿色标记），其他属性保持不变。", MessageType.Info);
        
        // 布尔值属性 - 默认折叠
        showBooleanProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showBooleanProperties, "布尔值属性");
        if (showBooleanProperties)
        {
            DisplayBooleanProperties();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 浮点属性（排除布尔值）- 默认折叠
        showFloatProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFloatProperties, "浮点属性");
        if (showFloatProperties)
        {
            DisplayFloatProperties();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 颜色属性 - 默认折叠
        showColorProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showColorProperties, "颜色属性");
        if (showColorProperties)
        {
            DisplayPropertiesByType(ShaderUtil.ShaderPropertyType.Color);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 向量属性 - 默认折叠
        showVectorProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showVectorProperties, "向量属性");
        if (showVectorProperties)
        {
            DisplayPropertiesByType(ShaderUtil.ShaderPropertyType.Vector);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // 纹理属性 - 默认折叠
        showTextureProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showTextureProperties, "纹理属性");
        if (showTextureProperties)
        {
            DisplayPropertiesByType(ShaderUtil.ShaderPropertyType.TexEnv);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("应用修改到所有材质", GUILayout.Height(40)))
        {
            ApplyChanges();
        }
        
        GUILayout.Space(10);
    }
    
    // 布尔值属性显示
    void DisplayBooleanProperties()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        foreach (var prop in properties.Values)
        {
            if (!prop.isBoolean) continue;
            
            EditorGUILayout.BeginHorizontal();
            
            // 显示编辑状态指示器
            Color originalColor = GUI.color;
            if (propertyEdited[prop.name])
            {
                GUI.color = Color.green;
                GUILayout.Label("●", GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            GUILayout.Label(prop.name, GUILayout.Width(130));
            
            // 获取当前材质值（使用第一个材质作为参考）
            float currentValue = (float)GetPropertyValue(materials[0], prop.name, prop.type);
            bool currentBoolValue = Mathf.Approximately(currentValue, 1.0f);
            
            bool newBoolValue = EditorGUILayout.Toggle(currentBoolValue);
            
            if (newBoolValue != currentBoolValue)
            {
                propertyEdited[prop.name] = true;
                PreviewPropertyChange(prop.name, prop.type, newBoolValue ? 1.0f : 0.0f);
            }
            
            if (GUILayout.Button("重置", GUILayout.Width(60)))
            {
                propertyEdited[prop.name] = false;
                PreviewPropertyChange(prop.name, prop.type, prop.defaultValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // 浮点属性显示（排除布尔值）
    void DisplayFloatProperties()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        foreach (var prop in properties.Values)
        {
            if (prop.type != ShaderUtil.ShaderPropertyType.Float && 
                prop.type != ShaderUtil.ShaderPropertyType.Range)
                continue;
            
            if (prop.isBoolean) continue;
            
            EditorGUILayout.BeginHorizontal();
            
            // 显示编辑状态指示器
            Color originalColor = GUI.color;
            if (propertyEdited[prop.name])
            {
                GUI.color = Color.green;
                GUILayout.Label("●", GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            GUILayout.Label(prop.name, GUILayout.Width(130));
            
            // 获取当前材质值（使用第一个材质作为参考）
            float currentValue = (float)GetPropertyValue(materials[0], prop.name, prop.type);
            
            float newValue = EditorGUILayout.FloatField(currentValue);
            
            if (!Mathf.Approximately(newValue, currentValue))
            {
                propertyEdited[prop.name] = true;
                PreviewPropertyChange(prop.name, prop.type, newValue);
            }
            
            if (GUILayout.Button("重置", GUILayout.Width(60)))
            {
                propertyEdited[prop.name] = false;
                PreviewPropertyChange(prop.name, prop.type, prop.defaultValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    // 通用属性显示方法
    void DisplayPropertiesByType(ShaderUtil.ShaderPropertyType type)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        foreach (var prop in properties.Values)
        {
            if (prop.type != type) continue;
            
            EditorGUILayout.BeginHorizontal();
            
            // 显示编辑状态指示器
            Color originalColor = GUI.color;
            if (propertyEdited[prop.name])
            {
                GUI.color = Color.green;
                GUILayout.Label("●", GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            GUILayout.Label(prop.name, GUILayout.Width(130));
            
            // 获取当前材质值（使用第一个材质作为参考）
            object currentValue = GetPropertyValue(materials[0], prop.name, prop.type);
            object newValue = currentValue;
            
            switch (prop.type)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    newValue = EditorGUILayout.ColorField((Color)currentValue);
                    break;
                    
                case ShaderUtil.ShaderPropertyType.Vector:
                    newValue = EditorGUILayout.Vector4Field("", (Vector4)currentValue);
                    break;
                    
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    newValue = EditorGUILayout.ObjectField((Texture)currentValue, typeof(Texture), false);
                    break;
            }
            
            // 检查值是否改变
            if (!AreValuesEqual(newValue, currentValue))
            {
                propertyEdited[prop.name] = true;
                PreviewPropertyChange(prop.name, prop.type, newValue);
            }
            
            if (GUILayout.Button("重置", GUILayout.Width(60)))
            {
                propertyEdited[prop.name] = false;
                PreviewPropertyChange(prop.name, prop.type, prop.defaultValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    bool AreValuesEqual(object a, object b)
    {
        if (a == null || b == null) 
            return a == b;
        
        if (a is Color colorA && b is Color colorB)
        {
            return colorA.Equals(colorB);
        }
        if (a is Vector4 vectorA && b is Vector4 vectorB)
        {
            return vectorA == vectorB;
        }
        if (a is Texture texA && b is Texture texB)
        {
            return texA == texB;
        }
        
        return a.Equals(b);
    }

    void PreviewPropertyChange(string propName, ShaderUtil.ShaderPropertyType type, object value)
    {
        foreach (var mat in materials)
        {
            Undo.RecordObject(mat, "Preview Property Change");
            
            switch (type)
            {
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    mat.SetFloat(propName, (float)value);
                    break;
                case ShaderUtil.ShaderPropertyType.Color:
                    mat.SetColor(propName, (Color)value);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    mat.SetVector(propName, (Vector4)value);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    mat.SetTexture(propName, (Texture)value);
                    break;
            }
            
            EditorUtility.SetDirty(mat);
        }
    }

    void ApplyChanges()
    {
        if (materials.Count == 0 || properties.Count == 0)
            return;
        
        // 只应用用户编辑过的属性
        int editedCount = 0;
        
        foreach (var prop in properties.Values)
        {
            if (!propertyEdited[prop.name]) continue;
            
            foreach (var mat in materials)
            {
                Undo.RecordObject(mat, $"Modify {prop.name}");
                
                switch (prop.type)
                {
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        mat.SetFloat(prop.name, (float)GetPropertyValue(mat, prop.name, prop.type));
                        break;
                    case ShaderUtil.ShaderPropertyType.Color:
                        mat.SetColor(prop.name, (Color)GetPropertyValue(mat, prop.name, prop.type));
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        mat.SetVector(prop.name, (Vector4)GetPropertyValue(mat, prop.name, prop.type));
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        mat.SetTexture(prop.name, (Texture)GetPropertyValue(mat, prop.name, prop.type));
                        break;
                }
                
                EditorUtility.SetDirty(mat);
            }
            
            editedCount++;
        }
        
        AssetDatabase.SaveAssets();
        
        if (editedCount > 0)
        {
            EditorUtility.DisplayDialog("操作完成", 
                $"已成功修改 {materials.Count} 个材质球的 {editedCount} 个属性", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("无修改", 
                "没有编辑任何属性，所有材质保持不变", "确定");
        }
    }
}