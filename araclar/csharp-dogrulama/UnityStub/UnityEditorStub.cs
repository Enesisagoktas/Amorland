// Amorland — UnityEditor yer tutuculari (SADECE DERLEME DOGRULAMASI ICIN)
//
// Editor script'indeki bir derleme hatasi Unity'de BUTUN menuleri gorunmez yapar ve
// gelistirici sebebi anlamaz. O yuzden editor kodu da stub'a karsi derlenir.

using System;
using UnityEngine;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    public static class AssetDatabase
    {
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string path) => new UnityEngine.Object[0];
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => null;
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static string GetAssetPath(UnityEngine.Object o) => "";
        public static bool DeleteAsset(string path) => true;
    }

    public class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
        public string path { get; set; }
        public bool enabled { get; set; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; } = new EditorBuildSettingsScene[0];
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool muteOtherAudioSources { get; set; }
        public static ColorSpace colorSpace { get; set; }
    }

    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string path) => null;
        public bool ApplyModifiedProperties() => true;
        public void Update() { }
    }

    public class SerializedProperty
    {
        public int intValue { get; set; }
        public bool boolValue { get; set; }
        public float floatValue { get; set; }
        public string stringValue { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; }
        public int arraySize { get; set; }
        public SerializedProperty GetArrayElementAtIndex(int i) => null;
        public SerializedProperty FindPropertyRelative(string path) => null;
    }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;

    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => new Scene();
        public static bool SaveScene(Scene scene, string path) => true;
        public static bool SaveScene(Scene scene) => true;
        public static Scene OpenScene(string path) => new Scene();
    }
}

namespace UnityEngine
{
    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }
}
