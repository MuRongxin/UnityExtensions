using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using MorphImportEditor.MMD;
using System.IO;
using Codice.Client.BaseCommands;
using System.Linq;
using Unity.VisualScripting;
using System.Text.RegularExpressions;
using MorphImportEditor.SharedObject;


namespace MorphImportEditor {
    public class MmdMorphGenerate : EditorWindow {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public VmdObject vmdObject = new();
        public Object vmd_file {
            set {
                vmdObject.vmd_file = value;
            }
            get { return vmdObject.vmd_file; }
        }
        public string vmd_path { 
            get { return vmdObject.vmd_path; }
        }
        public string output_file_name = "out";
        public float frame_rate = 30;
        private string inpt_frame_rate = "30";

        public static MmdMorphGenerate Instance { get; private set; }
        [MenuItem("Tools/MorphImportEditor/MMD Morph Animation Clip Generate Tool")]
        public static void ShowWindow() {
            Instance = GetWindow<MmdMorphGenerate>();
            Instance.Show();
        }

        private void OnEnable() {


        }

        private void domain() {
            if (vmd_file == null || skinnedMeshRenderer == null) { Debug.LogError("Empty File Input | vmd_file is null or skinnedMeshRender is null"); return; }
            Debug.Log("vmd path: " + vmd_path);
            using var st = File.Open(vmd_path, FileMode.Open);
            var data = ConvertVMD.Read(st);
            st.Dispose();

            AnimationClip clip = new AnimationClip();
            clip.legacy = false;
            clip.frameRate = frame_rate;
            float delta = 1 / frame_rate;

            var list_morph = data.MorphFrames;
            Dictionary<string, List<Keyframe>> morph_keyframe = new();

            foreach (var item in list_morph) {
                List<Keyframe> currentKeyframes;
                if (!morph_keyframe.ContainsKey(item.MorphName)) {
                    currentKeyframes = new List<Keyframe>();
                    morph_keyframe.Add(item.MorphName, currentKeyframes);
                } else {
                    morph_keyframe.TryGetValue(item.MorphName, out currentKeyframes);
                }
                currentKeyframes.Add(new(item.FrameTime * delta, item.Weight * 100));
            }

            Debug.Log($"This Vmd File Contains {morph_keyframe.Count} Blend Shapes.Totoal Length {list_morph.Count}.The Vmd File Model Owner {data.ModelName}.");

            HashSet<string> skinnedMRkeys = new();
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                skinnedMRkeys.Add(skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i));
            }

            var out_path = Path.Combine(Path.GetDirectoryName(vmd_path), output_file_name + ".anim");
            foreach (var item in morph_keyframe) {
                var referenceName = skinnedMRkeys.Where(x => x.Contains(item.Key)).FirstOrDefault();
                if (referenceName == null) {
                    Debug.Log($"This Reference Skinned Mesh Render DO NOT Contain The Same Name,Not Create Cure For this {item.Key} curve");
                    continue;
                }
                AnimationCurve curve = new AnimationCurve(item.Value.ToArray());
                clip.SetCurve("", typeof(SkinnedMeshRenderer), $"blendShape.{referenceName}", curve);
            }

            AssetDatabase.CreateAsset(clip, out_path);
        }

        private void OnGUI() {

            GUILayout.Label("Morph Generate AnimationClip");
            GUILayout.Label("MorphImport Editor");
            GUILayout.Space(20);

            GUILayout.Label("Skinned Mesh Render");
            skinnedMeshRenderer = EditorGUILayout.ObjectField(skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
            GUILayout.TextArea("放置Hierarchy面板中的面部组件");

            GUILayout.Space(20);
            GUILayout.Label("VMD File");
            vmd_file = EditorGUILayout.ObjectField(vmd_file, typeof(Object), false) as Object;
            GUILayout.TextArea("选择包含Morph数据的.VMD文件，*.vmd");

            GUILayout.Space(20);
            GUILayout.Label("Custom Output Animation Clip Asset Name");
            output_file_name = GUILayout.TextField(output_file_name);
            GUILayout.Label($"输出目录预览：{Path.Combine(Path.GetDirectoryName(vmd_path), output_file_name + ".anim")}");
            if (GUILayout.Button("Read VMD File And Generate Morph Animation Clip")) {
                domain();
            }
            GUILayout.Label($"输出动画帧率");
            inpt_frame_rate = frame_rate.ToString();
            inpt_frame_rate = GUILayout.TextField(inpt_frame_rate);
            frame_rate = float.TryParse(inpt_frame_rate, out frame_rate) ? frame_rate : 30;
            GUILayout.Space(20);
            About.AboutGUI();
        }
    }
}
