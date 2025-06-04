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
    private bool showFloatProperties = false; // Ĭ���۵�
    private bool showColorProperties = false;
    private bool showTextureProperties = false;
    private bool showVectorProperties = false;
    private bool showBooleanProperties = false;

    // �洢ÿ�����Եı༭״̬
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
        GetWindow<MaterialBatchEditor>("���������༭��");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("���������༭��", EditorStyles.boldLabel);
        GUILayout.Label("�ϷŲ������·�������������޸�", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        Rect dropArea = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "�ϷŲ���������", EditorStyles.helpBox);
        
        HandleDragAndDrop(dropArea);
        
        if (materials.Count > 0)
        {
            GUILayout.Label($"��ѡ�� {materials.Count} ��������:", EditorStyles.boldLabel);
            
            // �����б���������
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Vector2 materialsScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(100));
            foreach (var mat in materials)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(mat, typeof(Material), false);
                
                if (GUILayout.Button("��", GUILayout.Width(25)))
                {
                    materials.Remove(mat);
                    CacheCommonProperties();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("����б�", GUILayout.Height(30)))
            {
                materials.Clear();
                properties.Clear();
                propertyEdited.Clear();
            }
            
            GUILayout.Space(20);
            
            if (properties.Count > 0)
            {
                // ���Ա༭�����������
                propertiesScroll = EditorGUILayout.BeginScrollView(propertiesScroll);
                
                DisplayPropertyEditors();
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("û�м�⵽��ͬ��Shader����", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("���ϷŲ������Ϸ�����", MessageType.Info);
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
                
                // ��ʼ���༭״̬Ϊδ�༭
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
        GUILayout.Label("Shader���Ա༭", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ֻ�޸����༭�������ԣ�����ɫ��ǣ����������Ա��ֲ��䡣", MessageType.Info);
        
        // ����ֵ���� - Ĭ���۵�
        showBooleanProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showBooleanProperties, "����ֵ����");
        if (showBooleanProperties)
        {
            DisplayBooleanProperties();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // �������ԣ��ų�����ֵ��- Ĭ���۵�
        showFloatProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFloatProperties, "��������");
        if (showFloatProperties)
        {
            DisplayFloatProperties();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // ��ɫ���� - Ĭ���۵�
        showColorProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showColorProperties, "��ɫ����");
        if (showColorProperties)
        {
            DisplayPropertiesByType(ShaderUtil.ShaderPropertyType.Color);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // �������� - Ĭ���۵�
        showVectorProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showVectorProperties, "��������");
        if (showVectorProperties)
        {
            DisplayPropertiesByType(ShaderUtil.ShaderPropertyType.Vector);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // �������� - Ĭ���۵�
        showTextureProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showTextureProperties, "��������");
        if (showTextureProperties)
        {
            DisplayPropertiesByType(ShaderUtil.ShaderPropertyType.TexEnv);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Ӧ���޸ĵ����в���", GUILayout.Height(40)))
        {
            ApplyChanges();
        }
        
        GUILayout.Space(10);
    }
    
    // ����ֵ������ʾ
    void DisplayBooleanProperties()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        foreach (var prop in properties.Values)
        {
            if (!prop.isBoolean) continue;
            
            EditorGUILayout.BeginHorizontal();
            
            // ��ʾ�༭״ָ̬ʾ��
            Color originalColor = GUI.color;
            if (propertyEdited[prop.name])
            {
                GUI.color = Color.green;
                GUILayout.Label("��", GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            GUILayout.Label(prop.name, GUILayout.Width(130));
            
            // ��ȡ��ǰ����ֵ��ʹ�õ�һ��������Ϊ�ο���
            float currentValue = (float)GetPropertyValue(materials[0], prop.name, prop.type);
            bool currentBoolValue = Mathf.Approximately(currentValue, 1.0f);
            
            bool newBoolValue = EditorGUILayout.Toggle(currentBoolValue);
            
            if (newBoolValue != currentBoolValue)
            {
                propertyEdited[prop.name] = true;
                PreviewPropertyChange(prop.name, prop.type, newBoolValue ? 1.0f : 0.0f);
            }
            
            if (GUILayout.Button("����", GUILayout.Width(60)))
            {
                propertyEdited[prop.name] = false;
                PreviewPropertyChange(prop.name, prop.type, prop.defaultValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // ����������ʾ���ų�����ֵ��
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
            
            // ��ʾ�༭״ָ̬ʾ��
            Color originalColor = GUI.color;
            if (propertyEdited[prop.name])
            {
                GUI.color = Color.green;
                GUILayout.Label("��", GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            GUILayout.Label(prop.name, GUILayout.Width(130));
            
            // ��ȡ��ǰ����ֵ��ʹ�õ�һ��������Ϊ�ο���
            float currentValue = (float)GetPropertyValue(materials[0], prop.name, prop.type);
            
            float newValue = EditorGUILayout.FloatField(currentValue);
            
            if (!Mathf.Approximately(newValue, currentValue))
            {
                propertyEdited[prop.name] = true;
                PreviewPropertyChange(prop.name, prop.type, newValue);
            }
            
            if (GUILayout.Button("����", GUILayout.Width(60)))
            {
                propertyEdited[prop.name] = false;
                PreviewPropertyChange(prop.name, prop.type, prop.defaultValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    // ͨ��������ʾ����
    void DisplayPropertiesByType(ShaderUtil.ShaderPropertyType type)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        foreach (var prop in properties.Values)
        {
            if (prop.type != type) continue;
            
            EditorGUILayout.BeginHorizontal();
            
            // ��ʾ�༭״ָ̬ʾ��
            Color originalColor = GUI.color;
            if (propertyEdited[prop.name])
            {
                GUI.color = Color.green;
                GUILayout.Label("��", GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            GUILayout.Label(prop.name, GUILayout.Width(130));
            
            // ��ȡ��ǰ����ֵ��ʹ�õ�һ��������Ϊ�ο���
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
            
            // ���ֵ�Ƿ�ı�
            if (!AreValuesEqual(newValue, currentValue))
            {
                propertyEdited[prop.name] = true;
                PreviewPropertyChange(prop.name, prop.type, newValue);
            }
            
            if (GUILayout.Button("����", GUILayout.Width(60)))
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
        
        // ֻӦ���û��༭��������
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
            EditorUtility.DisplayDialog("�������", 
                $"�ѳɹ��޸� {materials.Count} ��������� {editedCount} ������", "ȷ��");
        }
        else
        {
            EditorUtility.DisplayDialog("���޸�", 
                "û�б༭�κ����ԣ����в��ʱ��ֲ���", "ȷ��");
        }
    }
}