// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI;
namespace Landomatic.TorpedoRoom
{
    internal static class UiScale
    {
        static readonly FieldInfo MainCanvas=AccessTools.Field(typeof(GameUI),"mainCanvas");
        static GameUI owner;
        static Canvas native;
        internal static bool Match(Canvas canvas,CanvasScaler scaler)
        {
            if(canvas==null||scaler==null)return false;
            var ui=Runtime.Get<GameUI>();
            if(owner!=ui||native==null){
                owner=ui;var value=ui==null||MainCanvas==null?null:MainCanvas.GetValue(ui);
                var component=value as Component;var go=value as GameObject;
                native=component!=null?component.GetComponent<Canvas>():go==null?null:go.GetComponent<Canvas>();
            }
            if(native==null||native==canvas||!Valid(native.scaleFactor))return false;
            // Native CanvasUserSettings drives the main CanvasScaler from uiScale.
            // Copy the resulting pixel scale, without a second resolution multiplier.
            if(scaler.uiScaleMode!=CanvasScaler.ScaleMode.ConstantPixelSize)scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            if(scaler.scaleFactor!=native.scaleFactor)scaler.scaleFactor=native.scaleFactor;
            if(scaler.referencePixelsPerUnit!=native.referencePixelsPerUnit)scaler.referencePixelsPerUnit=native.referencePixelsPerUnit;
            if(canvas.scaleFactor!=native.scaleFactor)canvas.scaleFactor=native.scaleFactor;
            if(canvas.referencePixelsPerUnit!=native.referencePixelsPerUnit)canvas.referencePixelsPerUnit=native.referencePixelsPerUnit;
            if(canvas.pixelPerfect!=native.pixelPerfect)canvas.pixelPerfect=native.pixelPerfect;
            return true;
        }
        static bool Valid(float value){return !float.IsNaN(value)&&!float.IsInfinity(value)&&value>0;}
        // Pixels per authored local unit, including canvas, ancestor and local scales.
        // Screen conversion also works for inactive native template hierarchies.
        internal static Vector2 Pixels(Transform transform)
        {
            if(transform==null)return new Vector2(1,1);
            var canvas=transform.GetComponentInParent<Canvas>();
            var camera=canvas==null||canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var origin=RectTransformUtility.WorldToScreenPoint(camera,transform.TransformPoint(Vector3.zero));
            var x=RectTransformUtility.WorldToScreenPoint(camera,transform.TransformPoint(new Vector3(1,0,0)));
            var y=RectTransformUtility.WorldToScreenPoint(camera,transform.TransformPoint(new Vector3(0,1,0)));
            float width=Vector2.Distance(origin,x),height=Vector2.Distance(origin,y);
            return new Vector2(Valid(width)?width:1,Valid(height)?height:1);
        }
        internal static void SetScale(Transform target,Vector3 scale)
        {var old=target.localScale;if(System.Math.Abs(old.x-scale.x)>.0001f||System.Math.Abs(old.y-scale.y)>.0001f||System.Math.Abs(old.z-scale.z)>.0001f)target.localScale=scale;}
        internal static Vector3 Between(Transform source,Transform target)
        {var a=Pixels(source);var b=Pixels(target);return new Vector3(a.x/b.x,a.y/b.y,1);}
    }
}
