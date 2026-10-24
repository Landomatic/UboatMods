// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. Test doubles only; no game or upstream code.
// These shapes let offline tests execute the real mod source. They do not verify
// the game's API, Unity destroyed-object semantics, Harmony, or menu rendering.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public static int FindCalls;
        public static T FindObjectOfType<T>() where T : class { FindCalls++; return null; }
        public static void DontDestroyOnLoad(Object value) { }
    }
    public class MonoBehaviour : Object { }
    public class GameObject : Object
    {
        public static int Created;
        public GameObject(string name) { Created++; }
        public T AddComponent<T>() where T : new() { return new T(); }
    }
    public static class Application { public static string persistentDataPath = ""; }
    public static class Time { public static float realtimeSinceStartup; public static float unscaledTime; }
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) { return value < min ? min : value > max ? max : value; }
    }
    public static class Debug
    {
        public static readonly List<string> Messages = new List<string>();
        public static void Log(object value) { Messages.Add("INFO " + value); }
        public static void LogWarning(object value) { Messages.Add("WARN " + value); }
        public static void LogError(object value) { Messages.Add("ERROR " + value); }
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }
    public sealed class Harmony
    {
        public static bool ThrowPatch;
        public static bool ThrowRollback;
        public static int PatchCalls;
        public static string RolledBackId = "";
        public Harmony(string id) { }
        public void PatchAll(Assembly assembly)
        {
            PatchCalls++;
            if (ThrowPatch) throw new InvalidOperationException("simulated partial patch installation");
        }
        public void UnpatchAll(string id)
        {
            RolledBackId = id;
            if (ThrowRollback) throw new InvalidOperationException("simulated rollback failure");
        }
    }
}
namespace UBOAT.Game { public interface IUserMod { void OnLoaded(); } }
namespace UBOAT.Game.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NonSerializedInGameStateAttribute : Attribute { }
}
namespace UBOAT.Game.Scene.Audio
{
    public class DeepAudioListener
    {
        private bool inside;
        public bool IsHydrophone = true;
        public bool ThrowNextWrite;
        public int Writes;
        public bool IsInsideBoat
        {
            get { return inside; }
            set
            {
                Writes++;
                if (ThrowNextWrite) { ThrowNextWrite = false; throw new InvalidOperationException("simulated listener failure"); }
                inside = value;
            }
        }
    }
}
namespace UBOAT.Game.Scene.Entities
{
    public class PlayerShip { public string name = "Test boat"; public bool Submerged = true; public float DeckDepth = 50f; }
    public class ListeningRoom { public object ParentEntity; }
}
namespace UBOAT.Game.Scene.Camera
{
    public enum CameraMode { FPP = 1, Generic = 16 }
    public class MainCamera
    {
        public CameraMode CurrentMode = CameraMode.Generic;
        public bool IsAudioListener;
        public UBOAT.Game.Scene.Audio.DeepAudioListener AudioListener;
    }
    public class GenericCameraController
    {
        public MainCamera MainCamera;
        public object ExternalController;
    }
}
namespace UBOAT.Game.Scene.Camera.ExternalControllers
{
    public class HydrophoneController { public UBOAT.Game.Scene.Entities.ListeningRoom ListeningRoom; }
}
