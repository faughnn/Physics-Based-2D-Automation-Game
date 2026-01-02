// Mock Unity types for testing outside Unity Editor
using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 down => new Vector2(0, -1);

        public static float Distance(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
    }

    public static class Mathf
    {
        public const float PI = 3.14159265359f;
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int i) => Math.Abs(i);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0, 1);
    }

    public static class Random
    {
        private static System.Random rng = new System.Random(42); // Seeded for reproducibility
        public static float Range(float min, float max) => (float)(rng.NextDouble() * (max - min) + min);
        public static int Range(int min, int max) => rng.Next(min, max);
        public static Vector2 insideUnitCircle
        {
            get
            {
                float angle = (float)(rng.NextDouble() * 2 * Math.PI);
                float r = (float)Math.Sqrt(rng.NextDouble());
                return new Vector2(r * (float)Math.Cos(angle), r * (float)Math.Sin(angle));
            }
        }
        public static void InitState(int seed) => rng = new System.Random(seed);
    }

    public static class Debug
    {
        public static void Log(string message) => Console.WriteLine($"[LOG] {message}");
        public static void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
        public static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }

    // Stub classes - these won't have real behavior but allow compilation
    public class MonoBehaviour { }
    public class GameObject { }
    public class Component { }
    public class Transform { }
    public class SpriteRenderer { }
    public class Rigidbody2D { }
    public class BoxCollider2D { }
    public class CircleCollider2D { }
    public class Collider2D { }
    public class Collision2D { }
    public class Camera { }
    public class Sprite { }
    public class Texture2D { }
    public class PhysicsMaterial2D
    {
        public float bounciness;
        public float friction;
        public PhysicsMaterial2D(string name) { }
    }
    public class Canvas { }
    public class Text { }
    public class Image { }
    public class Button { }
    public class RectTransform { }

    public static class Physics2D
    {
        public static Vector2 gravity = new Vector2(0, -9.81f);
        public static void IgnoreLayerCollision(int layer1, int layer2, bool ignore) { }
    }

    public class RaycastHit2D
    {
        public Collider2D collider;
    }

    public enum RigidbodyType2D { Dynamic, Static, Kinematic }
    public enum CollisionDetectionMode2D { Discrete, Continuous }
    public enum RigidbodyInterpolation2D { None, Interpolate, Extrapolate }
    public enum FilterMode { Point, Bilinear }

    public class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type type) { }
    }

    public static class Time
    {
        public static float deltaTime = 0.016f;
        public static float fixedDeltaTime = 0.02f;
        public static float time = 0f;
    }

    public static class Input
    {
        public static float GetAxisRaw(string axis) => 0f;
        public static bool GetButtonDown(string button) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static Vector3 mousePosition => Vector3.zero;
    }

    public enum KeyCode { None, Tab, Escape, Q, E, Space }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string name) where T : class => null;
    }
}
