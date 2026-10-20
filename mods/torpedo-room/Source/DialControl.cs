// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Landomatic.TorpedoRoom
{
    // A manual dial controls the same input field as typing; Apply is still explicit.
    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class PreparationDial : MonoBehaviour,IPointerDownHandler,IDragHandler,IScrollHandler
    {
        internal InputField Input;
        internal float Minimum,Maximum,Step;
        internal float[] Choices;
        internal bool Compass;
        internal string DisplayFormat;
        internal Func<bool> CanEdit;
        internal Transform Needle;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Point(e);}
        public void OnDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Point(e);}
        void Point(PointerEventData e)
        {
            if(CanEdit==null||!CanEdit())return;Vector2 p;var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out p))return;
            // Local coordinates are relative to the RectTransform pivot, which
            // is top-left in our layout. Angles require the geometric centre.
            p-=rect.rect.center;if(p.sqrMagnitude<16)return;
            float angle=Mathf.Atan2(p.x,p.y)*Mathf.Rad2Deg;
            float value=Compass?DialValues.Compass(angle,Step):DialValues.AtAngle(angle,Minimum,Maximum,Step);Write(Choices==null?value:SpeedValues.Nearest(Choices,value));
        }
        public void OnScroll(PointerEventData e)
        {if(CanEdit==null||!CanEdit())return;float value;if(float.TryParse(Input.text,NumberStyles.Float,CultureInfo.InvariantCulture,out value))Write(Choices==null?(Compass?DialValues.Compass(value+Math.Sign(e.scrollDelta.y)*Step,Step):DialValues.Nudge(value,e.scrollDelta.y,Minimum,Maximum,Step)):SpeedValues.Nudge(Choices,value,e.scrollDelta.y));}
        internal void Write(float value){Input.text=value.ToString(DisplayFormat??(Step<1?"0.##":"0"),CultureInfo.InvariantCulture);}
        internal void Refresh()
        {float value;if(Needle!=null&&float.TryParse(Input.text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&Preparation.Finite(value))Needle.localRotation=Quaternion.Euler(0,0,Compass?-DialValues.Compass(value,Step):90-180*Mathf.InverseLerp(Minimum,Maximum,value));}
    }
}
