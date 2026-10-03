// UnityEngine mínimo para compilar y EJECUTAR los generadores de sprites fuera de Unity (vista previa).
// Solo lo que usan: Mathf, Color, Color32, Vector2, Rect, Texture2D (guarda los píxeles) y Sprite.
using System;
namespace UnityEngine {
public static class Mathf {
    public static int RoundToInt(float v) => (int)System.Math.Round(v, System.MidpointRounding.ToEven);
  public const float PI = (float)Math.PI;
  public const float Deg2Rad = PI / 180f;
  public const float Rad2Deg = 180f / PI;
  public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
  public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
  public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
  public static float Sqrt(float v) => (float)Math.Sqrt(v);
  public static float Abs(float v) => Math.Abs(v);
  public static int Abs(int v) => Math.Abs(v);
  public static float Min(float a, float b) => a < b ? a : b;
  public static float Max(float a, float b) => a > b ? a : b;
  public static int Min(int a, int b) => a < b ? a : b;
  public static int Max(int a, int b) => a > b ? a : b;
  public static float Sin(float v) => (float)Math.Sin(v);
  public static float Cos(float v) => (float)Math.Cos(v);
  public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
  public static float Pow(float a, float b) => (float)Math.Pow(a, b);
  public static float Exp(float a) => (float)Math.Exp(a);
  public static float Log(float a) => (float)Math.Log(a);
  public static int CeilToInt(float a) => (int)Math.Ceiling(a);
  public static float Sign(float v) => v >= 0f ? 1f : -1f;
  public static float Floor(float v) => (float)Math.Floor(v);
  public static float Round(float v) => (float)Math.Round(v);
  public static float Repeat(float t, float l) => Clamp(t - Floor(t / l) * l, 0f, l);
  public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
  public static float SmoothStep(float from, float to, float t) { t = Clamp01(t); t = -2f*t*t*t + 3f*t*t; return to*t + from*(1f-t); }
}
public struct Color { public float r,g,b,a;
  public Color(float r,float g,float b,float a=1f){this.r=r;this.g=g;this.b=b;this.a=a;}
  public static Color white => new Color(1,1,1,1);
  public static Color Lerp(Color x, Color y, float t){ t=Mathf.Clamp01(t); return new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);} }
public struct Color32 { public byte r,g,b,a;
  public static implicit operator Color(Color32 c) => new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f); public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}
  public static implicit operator Color32(Color c) => new Color32((byte)(Mathf.Clamp01(c.r)*255f+0.5f),(byte)(Mathf.Clamp01(c.g)*255f+0.5f),(byte)(Mathf.Clamp01(c.b)*255f+0.5f),(byte)(Mathf.Clamp01(c.a)*255f+0.5f)); }
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
  public float sqrMagnitude => x*x+y*y; public float magnitude => Mathf.Sqrt(sqrMagnitude);
  public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x+b.x,a.y+b.y);
  public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
  public static Vector2 operator *(Vector2 a, float k) => new Vector2(a.x*k,a.y*k);
  public static Vector2 operator *(float k, Vector2 a) => new Vector2(a.x*k,a.y*k);
  public static float Distance(Vector2 a, Vector2 b) => (a-b).magnitude;
  public static float Dot(Vector2 a, Vector2 b) => a.x*b.x+a.y*b.y; }
public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} }
public struct Rect { public Rect(float x,float y,float w,float h){} }
public enum TextureFormat { RGBA32 } public enum TextureWrapMode { Clamp, Repeat } public enum FilterMode { Bilinear, Point }
public class Object {}
public class Texture2D : Object { public int width; public Color32[] pixels; public Texture2D(int w,int h,TextureFormat f,bool m){width=w;} public TextureWrapMode wrapMode{get;set;} public FilterMode filterMode{get;set;} public void SetPixels32(Color32[] p){pixels=p;} public void Apply(){} }
public class Sprite : Object { public Texture2D texture; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => new Sprite{texture=t}; }
public class AudioClip : Object { public float[] data; public int frequency; public static AudioClip Create(string n, int len, int ch, int freq, bool stream) => new AudioClip{data=new float[len], frequency=freq}; public bool SetData(float[] d, int off){ Array.Copy(d,0,data,off,d.Length); return true; } public int samples => data.Length; public bool GetData(float[] d, int off){ Array.Copy(data,off,d,0,d.Length); return true; } }
}
