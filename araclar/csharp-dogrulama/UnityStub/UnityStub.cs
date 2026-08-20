// Amorland — Unity API yer tutuculari (SADECE DERLEME DOGRULAMASI ICIN)
//
// Bu dosya Unity DEGILDIR ve oyuna girmez. Amaci tek: Unity kurulu olmayan bir
// makinede istemci C# kodunu GERCEKTEN derleyip sozdizimi ve tip hatalarini
// yakalamak. Imzalar Unity'nin gercek imzalariyla ayni tutulmustur; bir imza
// burada yanlissa derleme yesil gorunur ama Unity'de kirmizi olur — o yuzden
// yeni bir API kullanildiginda buraya da gercek imzasiyla eklenmelidir.
//
// Kapsam: yalnizca istemci kodunun kullandigi uyeler.

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
    }

    [Flags]
    public enum HideFlags { None = 0, HideAndDontSave = 61 }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 up => new Vector2(0, 1);
        public static Vector2 right => new Vector2(1, 0);
        public float magnitude => Mathf.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized => magnitude > 0 ? new Vector2(x / magnitude, y / magnitude) : zero;
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) =>
            new Vector2(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t));
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float f) => new Vector2(a.x * f, a.y * f);
        public static Vector2 operator *(float f, Vector2 a) => new Vector2(a.x * f, a.y * f);
        public static Vector2 operator /(Vector2 a, float f) => new Vector2(a.x / f, a.y / f);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object o) => o is Vector2 v && this == v;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0; }
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized => magnitude > 0 ? new Vector3(x / magnitude, y / magnitude, z / magnitude) : zero;
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t));
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) => b;
        public static Vector3 Cross(Vector3 a, Vector3 b) =>
            new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
        public static Vector3 operator *(float f, Vector3 a) => a * f;
        public static Vector3 operator /(Vector3 a, float f) => new Vector3(a.x / f, a.y / f, a.z / f);
        public static bool operator ==(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public override bool Equals(object o) => o is Vector3 v && this == v;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        public override string ToString() => $"({x}, {y}, {z})";
    }

    public struct Vector2Int
    {
        public int x { get; set; }
        public int y { get; set; }
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int one => new Vector2Int(1, 1);
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public override bool Equals(object o) => o is Vector2Int v && this == v;
        public override int GetHashCode() => x * 397 ^ y;
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3Int
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 v) => identity;
        public static Quaternion LookRotation(Vector3 f) => identity;
        public static Quaternion LookRotation(Vector3 f, Vector3 u) => identity;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => b;
        public static Quaternion AngleAxis(float a, Vector3 axis) => identity;
        public Vector3 eulerAngles { get; set; }
        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color red => new Color(1, 0, 0);
        public static Color green => new Color(0, 1, 0);
        public static Color blue => new Color(0, 0, 1);
        public static Color yellow => new Color(1, 0.92f, 0.016f);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f);
        public static Color grey => gray;
        public static Color Lerp(Color a, Color b, float t) =>
            new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t), Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
        public static Color operator *(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a * f);
        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static implicit operator Color32(Color c) => new Color32();
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string html, out Color color) { color = Color.white; return true; }
        public static string ToHtmlStringRGB(Color c) => "FFFFFF";
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public Vector2 position => new Vector2(x, y);
        public Vector2 size => new Vector2(width, height);
        public Vector2 center => new Vector2(x + width / 2f, y + height / 2f);
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
        public bool Contains(Vector2 p) => true;
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Infinity = float.PositiveInfinity;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Epsilon = 1.401298E-45f;
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int f) => Math.Abs(f);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Exp(float f) => (float)Math.Exp(f);
        public static float Log(float f) => (float)Math.Log(f);
        public static float Log10(float f) => (float)Math.Log10(f);
        public static float Sign(float f) => Math.Sign(f);
        public static float Tan(float f) => (float)Math.Tan(f);
        public static float Asin(float f) => (float)Math.Asin(f);
        public static float Acos(float f) => (float)Math.Acos(f);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp(float v, float a, float b) => Math.Min(Math.Max(v, a), b);
        public static int Clamp(int v, int a, int b) => Math.Min(Math.Max(v, a), b);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float MoveTowards(float a, float b, float d) => b;
        public static float SmoothStep(float a, float b, float t) => Lerp(a, b, t);
        public static float SmoothDamp(float cur, float target, ref float vel, float time) => target;
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Ceil(float f) => (float)Math.Ceiling(f);
        public static float Round(float f) => (float)Math.Round(f);
        public static float Repeat(float t, float len) => t % len;
        public static float PingPong(float t, float len) => t % len;
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-5f;
        public static float PerlinNoise(float x, float y) => 0.5f;
    }

    public static class Time
    {
        public static float time => 0f;
        public static float deltaTime => 0.016f;
        public static float unscaledTime => 0f;
        public static float unscaledDeltaTime => 0.016f;
        public static float fixedDeltaTime => 0.02f;
        public static float timeScale { get; set; }
        public static int frameCount => 0;
        public static float realtimeSinceStartup => 0f;
    }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { }
        public static void LogException(Exception e) { }
        public static void DrawLine(Vector3 a, Vector3 b, Color c) { }
    }

    public static class Application
    {
        public static bool isPlaying => true;
        public static bool isEditor => false;
        public static string persistentDataPath => "";
        public static string version => "0.1.0";
        public static RuntimePlatform platform => RuntimePlatform.WindowsPlayer;
        public static event Action<bool> focusChanged;
        public static void Quit() { }
        public static bool isMobilePlatform => false;
        public static int targetFrameRate { get; set; }
    }

    public enum RuntimePlatform { WindowsPlayer, Android, IPhonePlayer, OSXEditor, WindowsEditor }

    public static class SystemInfo
    {
        public static string deviceUniqueIdentifier => "stub-device";
        public static string unsupportedIdentifier => "n/a";
        public static string deviceModel => "stub";
        public static int systemMemorySize => 4096;
        public static bool supportsVibration => true;
    }

    public static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
        public static float dpi => 96f;
        public static ScreenOrientation orientation { get; set; }
        public static bool autorotateToLandscapeLeft { get; set; }
        public static bool autorotateToLandscapeRight { get; set; }
        public static bool autorotateToPortrait { get; set; }
        public static bool autorotateToPortraitUpsideDown { get; set; }
        public static int sleepTimeout { get; set; }
    }

    public enum ScreenOrientation { Portrait, LandscapeLeft, LandscapeRight, AutoRotation }

    public static class SleepTimeout { public const int NeverSleep = -1; public const int SystemSetting = -2; }

    public static class PlayerPrefs
    {
        public static string GetString(string k, string d = "") => d;
        public static void SetString(string k, string v) { }
        public static int GetInt(string k, int d = 0) => d;
        public static void SetInt(string k, int v) { }
        public static float GetFloat(string k, float d = 0f) => d;
        public static void SetFloat(string k, float v) { }
        public static bool HasKey(string k) => false;
        public static void DeleteKey(string k) { }
        public static void DeleteAll() { }
        public static void Save() { }
    }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static ShadowQuality shadows { get; set; }
        public static int shadowCascades { get; set; }
        public static int antiAliasing { get; set; }
        public static string[] names => new string[] { "Default" };
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) { }
        public static int GetQualityLevel() => 0;
    }

    public enum ShadowQuality { Disable, HardOnly, All }

    public class Component : Object
    {
        public Transform transform { get; set; }
        public GameObject gameObject { get; set; }
        public T GetComponent<T>() where T : class => null;
        public T GetComponentInChildren<T>() where T : class => null;
        public T GetComponentInParent<T>() where T : class => null;
        public T[] GetComponentsInChildren<T>() where T : class => new T[0];
        public Component GetComponent(Type t) => null;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled => enabled;
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale => localScale;
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 forward { get; set; }
        public Vector3 up { get; set; }
        public Vector3 right { get; set; }
        public Transform parent { get; set; }
        public int childCount => 0;
        public Transform GetChild(int i) => null;
        public Transform Find(string n) => null;
        public void SetParent(Transform p) { }
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void SetAsLastSibling() { }
        public void SetAsFirstSibling() { }
        public void SetSiblingIndex(int i) { }
        public void LookAt(Vector3 t) { }
        public void Translate(Vector3 v) { }
        public void Rotate(Vector3 v) { }
        public void DetachChildren() { }
        public IEnumerator GetEnumerator() => new List<Transform>().GetEnumerator();
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => new Rect(0, 0, 100, 100);
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public GameObject(string name, params Type[] components) { this.name = name; }
        public Transform transform { get; set; }
        public bool activeSelf => true;
        public bool activeInHierarchy => true;
        public int layer { get; set; }
        public string tag { get; set; }
        public void SetActive(bool a) { }
        public T AddComponent<T>() where T : Component => null;
        public Component AddComponent(Type t) => null;
        public T GetComponent<T>() where T : class => null;
        public T GetComponentInChildren<T>() where T : class => null;
        public T[] GetComponentsInChildren<T>() where T : class => new T[0];
        public static GameObject Find(string n) => null;
        public static GameObject CreatePrimitive(PrimitiveType t) => null;
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator r) => null;
        public Coroutine StartCoroutine(string m) => null;
        public void StopCoroutine(Coroutine c) { }
        public void StopCoroutine(IEnumerator c) { }
        public void StopAllCoroutines() { }
        public void Invoke(string m, float t) { }
        public void CancelInvoke() { }
        public bool IsInvoking(string m) => false;
        public static void print(object o) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => null;
        public static ScriptableObject CreateInstance(Type t) => null;
    }

    public class Coroutine : Object { }

    public class YieldInstruction { }

    public class CustomYieldInstruction : IEnumerator
    {
        public virtual bool keepWaiting => false;
        public object Current => null;
        public bool MoveNext() => keepWaiting;
        public void Reset() { }
    }

    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } }
    public class WaitForEndOfFrame : YieldInstruction { }
    public class WaitForFixedUpdate : YieldInstruction { }
    public class WaitUntil : CustomYieldInstruction { public WaitUntil(Func<bool> p) { } }
    public class WaitWhile : CustomYieldInstruction { public WaitWhile(Func<bool> p) { } }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public int cullingMask { get; set; }
        public int depth { get; set; }
        public Ray ScreenPointToRay(Vector3 p) => new Ray();
        public Vector3 ScreenToWorldPoint(Vector3 p) => Vector3.zero;
        public Vector3 WorldToScreenPoint(Vector3 p) => Vector3.zero;
    }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    public struct Ray
    {
        public Vector3 origin { get; set; }
        public Vector3 direction { get; set; }
        public Ray(Vector3 o, Vector3 d) { origin = o; direction = d; }
        public Vector3 GetPoint(float d) => origin + direction * d;
    }

    public struct Plane
    {
        public Plane(Vector3 normal, Vector3 point) { }
        public Plane(Vector3 normal, float d) { }
        public bool Raycast(Ray r, out float enter) { enter = 0f; return true; }
    }

    public struct RaycastHit
    {
        public Vector3 point { get; set; }
        public Vector3 normal { get; set; }
        public float distance { get; set; }
        public Collider collider { get; set; }
        public Transform transform { get; set; }
    }

    public static class Physics
    {
        public static bool Raycast(Ray r, out RaycastHit hit, float maxDistance = float.PositiveInfinity)
        { hit = new RaycastHit(); return false; }
        public static bool Raycast(Ray r, out RaycastHit hit, float maxDistance, int layerMask)
        { hit = new RaycastHit(); return false; }
    }

    public class Collider : Component { public bool enabled { get; set; } }
    public class BoxCollider : Collider { public Vector3 size { get; set; } public Vector3 center { get; set; } }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }

    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }

    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public int[] triangles { get; set; }
        public Vector3[] normals { get; set; }
        public Vector2[] uv { get; set; }
        public Color[] colors { get; set; }
        public Color32[] colors32 { get; set; }
        public void Clear() { }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void SetVertices(List<Vector3> v) { }
        public void SetTriangles(List<int> t, int sub) { }
    }

    public class MeshFilter : Component { public Mesh mesh { get; set; } public Mesh sharedMesh { get; set; } }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] materials { get; set; }
        public bool enabled { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }

    public class MeshRenderer : Renderer { }
    public class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public bool useWorldSpace { get; set; }
        public void SetPosition(int i, Vector3 p) { }
    }

    public class Material : Object
    {
        public Material(Shader s) { }
        public Material(Material m) { }
        public Color color { get; set; }
        public Shader shader { get; set; }
        public int renderQueue { get; set; }
        public void SetColor(string n, Color c) { }
        public void SetFloat(string n, float f) { }
        public void SetTexture(string n, Texture t) { }
        public bool HasProperty(string n) => true;
        public void EnableKeyword(string k) { }
        public void DisableKeyword(string k) { }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
    }

    public class Texture : Object { public int width { get; set; } public int height { get; set; } }

    public class Texture2D : Texture
    {
        public Texture2D(int w, int h) { }
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public void SetPixel(int x, int y, Color c) { }
        public void SetPixels(Color[] c) { }
        public Color GetPixel(int x, int y) => Color.white;
        public void Apply() { }
        public static Texture2D whiteTexture => null;
    }

    public enum TextureFormat { RGBA32, ARGB32, RGB24 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }

    public class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot) => null;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) => null;
        public Rect rect => new Rect();
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public struct Touch
    {
        public int fingerId { get; set; }
        public Vector2 position { get; set; }
        public Vector2 deltaPosition { get; set; }
        public float deltaTime { get; set; }
        public int tapCount { get; set; }
        public TouchPhase phase { get; set; }
        public float pressure { get; set; }
    }

    public static class Input
    {
        public static int touchCount => 0;
        public static Touch GetTouch(int i) => new Touch();
        public static Touch[] touches => new Touch[0];
        public static bool touchSupported => false;
        public static Vector3 mousePosition => Vector3.zero;
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static bool GetMouseButtonUp(int b) => false;
        public static bool GetKey(KeyCode k) => false;
        public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetKeyUp(KeyCode k) => false;
        public static float GetAxis(string a) => 0f;
        public static float GetAxisRaw(string a) => 0f;
        public static bool multiTouchEnabled { get; set; }
    }

    public enum KeyCode { None = 0, Escape = 27, Space = 32, Return = 13, Tab = 9, A = 97, D = 100, S = 115, W = 119 }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string name, int size) => null;
        public static Font CreateDynamicFontFromOSFont(string[] names, int size) => null;
        public static string[] GetOSInstalledFontNames() => new string[0];
        public int fontSize { get; set; }
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
        public int sortingOrder { get; set; }
        public bool pixelPerfect { get; set; }
        public static bool ForceUpdateCanvases() => true;
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class AudioClip : Object
    {
        public float length => 1f;
        public int frequency => 44100;
        public int channels => 1;
        public int samples => 44100;
        public static AudioClip Create(string name, int samples, int channels, int freq, bool stream) => null;
        public bool SetData(float[] data, int offset) => true;
        public bool GetData(float[] data, int offset) => true;
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public bool isPlaying => false;
        public float time { get; set; }
        public float spatialBlend { get; set; }
        public int priority { get; set; }
        public UnityEngine.Audio.AudioMixerGroup outputAudioMixerGroup { get; set; }
        public void Play() { }
        public void PlayOneShot(AudioClip c) { }
        public void PlayOneShot(AudioClip c, float v) { }
        public void Stop() { }
        public void Pause() { }
        public void UnPause() { }
    }

    public static class AudioListener
    {
        public static float volume { get; set; }
        public static bool pause { get; set; }
    }

    public static class AudioSettings
    {
        public static int outputSampleRate => 44100;
    }

    public static class Handheld
    {
        public static void Vibrate() { }
    }

    public class AndroidJavaObject : IDisposable
    {
        public AndroidJavaObject(string className, params object[] args) { }
        public T Call<T>(string method, params object[] args) => default;
        public void Call(string method, params object[] args) { }
        public T GetStatic<T>(string field) => default;
        public T Get<T>(string field) => default;
        public T CallStatic<T>(string method, params object[] args) => default;
        public void CallStatic(string method, params object[] args) { }
        public void Dispose() { }
    }

    public class AndroidJavaClass : AndroidJavaObject
    {
        public AndroidJavaClass(string className) : base(className) { }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => null;
        public static Object Load(string path) => null;
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad, BeforeSceneLoad, BeforeSplashScreen, SubsystemRegistration, AfterAssembliesLoaded
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class RequireComponent : Attribute { public RequireComponent(Type t) { } }

    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleComponent : Attribute { }

    public static class Random
    {
        public static float value => 0.5f;
        public static float Range(float min, float max) => min;
        public static int Range(int min, int max) => min;
        public static Vector2 insideUnitCircle => Vector2.zero;
        public static void InitState(int seed) { }
    }

    public class Gradient : Object { }
    public class AnimationCurve : Object
    {
        public static AnimationCurve EaseInOut(float t0, float v0, float t1, float v1) => null;
        public static AnimationCurve Linear(float t0, float v0, float t1, float v1) => null;
        public float Evaluate(float t) => 0f;
    }
}

namespace UnityEngine.Audio
{
    public class AudioMixer : Object
    {
        public AudioMixerGroup[] FindMatchingGroups(string path) => new AudioMixerGroup[0];
        public bool SetFloat(string name, float value) => true;
        public bool GetFloat(string name, out float value) { value = 0f; return true; }
    }

    public class AudioMixerGroup : Object { public AudioMixer audioMixer { get; set; } }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name { get; set; }
        public bool isLoaded => true;
        public int buildIndex => 0;
    }

    public static class SceneManager
    {
        public static Scene GetActiveScene() => new Scene();
        public static void LoadScene(string name) { }
        public static void LoadScene(int index) { }
        public static Scene CreateScene(string name) => new Scene();
        public static int sceneCount => 1;
    }
}

namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }

        public UnityWebRequest(string url, string method) { }
        public string url { get; set; }
        public string method { get; set; }
        public string error => null;
        public long responseCode { get; set; }
        public int timeout { get; set; }
        public Result result => Result.Success;
        public bool isDone => true;
        public float downloadProgress => 1f;
        public UploadHandler uploadHandler { get; set; }
        public DownloadHandler downloadHandler { get; set; }
        public void SetRequestHeader(string name, string value) { }
        public string GetResponseHeader(string name) => null;
        public UnityWebRequestAsyncOperation SendWebRequest() => new UnityWebRequestAsyncOperation();
        public void Abort() { }
        public void Dispose() { }
        public static UnityWebRequest Get(string url) => new UnityWebRequest(url, "GET");
        public static UnityWebRequest Post(string url, string body, string type) => new UnityWebRequest(url, "POST");
    }

    public class UnityWebRequestAsyncOperation : AsyncOperation { }

    public class AsyncOperation : YieldInstruction
    {
        public bool isDone => true;
        public float progress => 1f;
        public event Action<AsyncOperation> completed;
    }

    public class UploadHandler : IDisposable { public void Dispose() { } }
    public class UploadHandlerRaw : UploadHandler { public UploadHandlerRaw(byte[] data) { } }

    public class DownloadHandler : IDisposable
    {
        public string text => "";
        public byte[] data => new byte[0];
        public void Dispose() { }
    }

    public class DownloadHandlerBuffer : DownloadHandler { }
}

namespace UnityEngine.UI
{
    using UnityEngine;

    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get; set; }
        public Material material { get; set; }
        public void SetAllDirty() { }
    }

    public class MaskableGraphic : Graphic { public bool maskable { get; set; } }

    public class Image : MaskableGraphic
    {
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public float fillAmount { get; set; }
        public bool preserveAspect { get; set; }
        public enum Type { Simple, Sliced, Tiled, Filled }
    }

    public class RawImage : MaskableGraphic { public Texture texture { get; set; } public Rect uvRect { get; set; } }

    public class Text : MaskableGraphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public float lineSpacing { get; set; }
        public bool supportRichText { get; set; }
    }

    public class Selectable : Behaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public Navigation navigation { get; set; }
        public ColorBlock colors { get; set; }
        public Transition transition { get; set; }
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
    }

    public struct Navigation
    {
        public Mode mode { get; set; }
        public enum Mode { None, Horizontal, Vertical, Automatic, Explicit }
    }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
        public static ColorBlock defaultColorBlock => new ColorBlock();
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick { get; set; }
        // GERCEK Unity imzasi: UnityAction, System.Action DEGIL.
        // Stub burada Action kabul etseydi Unity'de CS1503 alirdik.
        public class ButtonClickedEvent
        {
            public void AddListener(UnityEngine.Events.UnityAction a) { }
            public void RemoveAllListeners() { }
            public void Invoke() { }
        }
    }

    public class Slider : Selectable
    {
        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
    }

    public class InputField : Selectable
    {
        public string text { get; set; }
        public Text textComponent { get; set; }
        public Text placeholder { get; set; }
        public int characterLimit { get; set; }
        public ContentType contentType { get; set; }
        public OnChangeEvent onValueChanged { get; set; }
        public SubmitEvent onEndEdit { get; set; }
        public enum ContentType { Standard, Alphanumeric, IntegerNumber, Name, EmailAddress }
        public class OnChangeEvent { public void AddListener(UnityEngine.Events.UnityAction<string> a) { } }
        public class SubmitEvent { public void AddListener(UnityEngine.Events.UnityAction<string> a) { } }
        public void ActivateInputField() { }
    }

    public class ScrollRect : Behaviour
    {
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public MovementType movementType { get; set; }
        public float scrollSensitivity { get; set; }
        public enum MovementType { Unrestricted, Elastic, Clamped }
    }

    public class Mask : Behaviour { public bool showMaskGraphic { get; set; } }
    public class RectMask2D : Behaviour { }

    public class CanvasScaler : Behaviour
    {
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float referencePixelsPerUnit { get; set; }
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
    }

    public class GraphicRaycaster : Behaviour { public bool ignoreReversedGraphics { get; set; } }

    public class LayoutElement : Behaviour
    {
        public float minWidth { get; set; }
        public float minHeight { get; set; }
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public float flexibleWidth { get; set; }
        public float flexibleHeight { get; set; }
    }

    public class LayoutGroup : Behaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public Vector2 cellSize { get; set; }
        public Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; }
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
    }

    public class ContentSizeFitter : Behaviour
    {
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform r) { }
    }
}

namespace UnityEngine
{
    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }
}

namespace UnityEngine.EventSystems
{
    using UnityEngine;

    public class EventSystem : Behaviour
    {
        public static EventSystem current { get; set; }
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
    }

    public class BaseInputModule : Behaviour { }
    public class StandaloneInputModule : BaseInputModule { }

    public class PointerEventData
    {
        public PointerEventData(EventSystem e) { }
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public int pointerId { get; set; }
        public GameObject pointerCurrentRaycast { get; set; }
    }

    public interface IPointerDownHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData e); }
    public interface IDragHandler { void OnDrag(PointerEventData e); }
    public interface IBeginDragHandler { void OnBeginDrag(PointerEventData e); }
    public interface IEndDragHandler { void OnEndDrag(PointerEventData e); }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
}

namespace UnityEngine.Events
{
    /// <summary>
    /// ⚠ Gercek Unity'de UnityEvent.AddListener bunu ister, System.Action'i DEGIL.
    /// Stub bunu Action olarak taklit ederse derleme yesil gorunur ama Unity'de kirilir.
    /// </summary>
    public delegate void UnityAction();

    public delegate void UnityAction<T0>(T0 arg0);

    public class UnityEvent
    {
        public void AddListener(UnityAction a) { }
        public void RemoveAllListeners() { }
        public void Invoke() { }
    }
}
