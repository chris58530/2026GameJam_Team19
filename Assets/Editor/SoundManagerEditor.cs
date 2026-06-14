using UnityEngine;
using UnityEditor;

/// <summary>
/// SoundManager 的自訂 Inspector。
/// 提供清晰的 BGM/SFX 音效清單管理介面，
/// 支援預覽播放、快速新增/移除音效片段。
/// </summary>
[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    private SerializedProperty bgmVolume;
    private SerializedProperty bgmFadeDuration;
    private SerializedProperty bgmClips;
    private SerializedProperty sceneBGMBindings;
    private SerializedProperty sfxVolume;
    private SerializedProperty sfxChannelCount;
    private SerializedProperty sfxClips;

    private bool showBGMSection = true;
    private bool showSFXSection = true;
    private bool showSceneBGMSection = true;

    private AudioSource previewSource;

    private void OnEnable()
    {
        bgmVolume = serializedObject.FindProperty("bgmVolume");
        bgmFadeDuration = serializedObject.FindProperty("bgmFadeDuration");
        bgmClips = serializedObject.FindProperty("bgmClips");
        sceneBGMBindings = serializedObject.FindProperty("sceneBGMBindings");
        sfxVolume = serializedObject.FindProperty("sfxVolume");
        sfxChannelCount = serializedObject.FindProperty("sfxChannelCount");
        sfxClips = serializedObject.FindProperty("sfxClips");
    }

    private void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 標題
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🔊 Sound Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // ─── BGM 區塊 ─────────────────────────────────
        showBGMSection = EditorGUILayout.BeginFoldoutHeaderGroup(showBGMSection, "🎵 BGM 設定");
        if (showBGMSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(bgmVolume, new GUIContent("音量"));
            EditorGUILayout.PropertyField(bgmFadeDuration, new GUIContent("淡入淡出時間 (秒)"));
            EditorGUILayout.Space(3);
            DrawClipArray(bgmClips, "BGM");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(10);

        // ─── 場景 BGM 綁定 ─────────────────────────────
        showSceneBGMSection = EditorGUILayout.BeginFoldoutHeaderGroup(showSceneBGMSection, "🎬 場景 BGM 自動綁定");
        if (showSceneBGMSection)
        {
            EditorGUI.indentLevel++;
            DrawSceneBGMBindings();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(10);

        // ─── SFX 區塊 ─────────────────────────────────
        showSFXSection = EditorGUILayout.BeginFoldoutHeaderGroup(showSFXSection, "🔫 SFX 設定");
        if (showSFXSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sfxVolume, new GUIContent("音量"));
            EditorGUILayout.PropertyField(sfxChannelCount, new GUIContent("同時播放聲道數"));

            if (sfxChannelCount.intValue < 1)
                sfxChannelCount.intValue = 1;

            EditorGUILayout.Space(3);
            DrawClipArray(sfxClips, "SFX");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(10);

        // ─── 統計資訊 ─────────────────────────────────
        EditorGUILayout.HelpBox(
            $"BGM 音效數量: {bgmClips.arraySize}\n" +
            $"SFX 音效數量: {sfxClips.arraySize}\n" +
            $"SFX 聲道數量: {sfxChannelCount.intValue}",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSceneBGMBindings()
    {
        EditorGUILayout.HelpBox(
            "設定場景切換時自動播放的 BGM。\n" +
            "場景名稱必須與 Build Settings 中一致，BGM 名稱必須在上方 BGM 清單中存在。",
            MessageType.None);

        EditorGUILayout.Space(3);

        for (int i = 0; i < sceneBGMBindings.arraySize; i++)
        {
            SerializedProperty element = sceneBGMBindings.GetArrayElementAtIndex(i);
            SerializedProperty sceneNameProp = element.FindPropertyRelative("sceneName");
            SerializedProperty bgmNameProp = element.FindPropertyRelative("bgmName");

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField("場景:", GUILayout.Width(35));
            EditorGUILayout.PropertyField(sceneNameProp, GUIContent.none, GUILayout.MinWidth(120));

            EditorGUILayout.LabelField("→ BGM:", GUILayout.Width(50));
            EditorGUILayout.PropertyField(bgmNameProp, GUIContent.none, GUILayout.MinWidth(100));

            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                sceneBGMBindings.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ 新增場景綁定", GUILayout.Width(150)))
        {
            sceneBGMBindings.InsertArrayElementAtIndex(sceneBGMBindings.arraySize);
            var newElement = sceneBGMBindings.GetArrayElementAtIndex(sceneBGMBindings.arraySize - 1);
            newElement.FindPropertyRelative("sceneName").stringValue = "";
            newElement.FindPropertyRelative("bgmName").stringValue = "";
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawClipArray(SerializedProperty arrayProp, string label)
    {
        EditorGUILayout.LabelField($"{label} 音效清單 ({arrayProp.arraySize})", EditorStyles.miniBoldLabel);

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("name");
            SerializedProperty clipProp = element.FindPropertyRelative("clip");

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 索引標籤
            EditorGUILayout.LabelField($"#{i}", GUILayout.Width(25));

            // 名稱
            EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.MinWidth(80), GUILayout.MaxWidth(150));

            // AudioClip
            EditorGUILayout.PropertyField(clipProp, GUIContent.none, GUILayout.MinWidth(100));

            // 自動填入名稱按鈕
            if (clipProp.objectReferenceValue != null && string.IsNullOrEmpty(nameProp.stringValue))
            {
                if (GUILayout.Button("自動命名", GUILayout.Width(65)))
                {
                    nameProp.stringValue = clipProp.objectReferenceValue.name;
                }
            }

            // 預覽按鈕
            if (clipProp.objectReferenceValue != null)
            {
                if (GUILayout.Button("▶", GUILayout.Width(25)))
                {
                    PlayPreview((AudioClip)clipProp.objectReferenceValue);
                }
            }

            // 停止預覽
            if (GUILayout.Button("■", GUILayout.Width(25)))
            {
                StopPreview();
            }

            // 刪除按鈕
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button($"+ 新增 {label} 音效", GUILayout.Width(150)))
        {
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
            var newElement = arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1);
            newElement.FindPropertyRelative("name").stringValue = "";
            newElement.FindPropertyRelative("clip").objectReferenceValue = null;
        }

        if (arrayProp.arraySize > 0)
        {
            if (GUILayout.Button("停止預覽", GUILayout.Width(80)))
            {
                StopPreview();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void PlayPreview(AudioClip clip)
    {
        StopPreview();

        // 使用 Unity Editor 內建的音效預覽工具
        var unityEditorAssembly = typeof(AudioImporter).Assembly;
        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

        if (audioUtilClass != null)
        {
            var playMethod = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            if (playMethod != null)
            {
                playMethod.Invoke(null, new object[] { clip, 0, false });
                return;
            }
        }

        // Fallback: 嘗試無參數版本
        Debug.Log($"[SoundManager Editor] 預覽: {clip.name} ({clip.length:F1}s)");
    }

    private void StopPreview()
    {
        var unityEditorAssembly = typeof(AudioImporter).Assembly;
        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

        if (audioUtilClass != null)
        {
            var stopMethod = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );

            stopMethod?.Invoke(null, null);
        }
    }
}
