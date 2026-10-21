// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using System.Reflection;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI;
using UBOAT.Game.UI.Periscope;

namespace Landomatic.TorpedoRoom
{
    internal static class StockUi
    {
        internal static void Install(Harmony harmony)
        {
            foreach(string name in new[]{"OnProgramLeftChanged","OnProgramRightChanged","OnProgramDistanceValueChanged","ChangeProgramDistanceValue","SetProgramAngleValue","ChangeProgramAngleValue"})
                harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),name),new HarmonyMethod(typeof(StockUi),"BeforeEdit"),new HarmonyMethod(typeof(StockUi),"AfterEdit"),finalizer:new HarmonyMethod(typeof(StockUi),"EditFinally"));
            harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),"SetProgramDisplayValues"),new HarmonyMethod(typeof(StockUi),"BeforeDisplay"),new HarmonyMethod(typeof(StockUi),"AfterDisplay"),finalizer:new HarmonyMethod(typeof(StockUi),"DisplayFinally"));
            harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),"ValidateProgramUIs"),postfix:new HarmonyMethod(typeof(StockUi),"SelectionValidated"));
            harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),"OnEnable"),postfix:new HarmonyMethod(typeof(StockUi),"PanelEnabled"));
            harmony.Patch(AccessTools.Method(typeof(TorpedoLauncherUI),"OnEnable"),postfix:new HarmonyMethod(typeof(StockUi),"TubeEnabled"));
            foreach(string name in new[]{"OnPointerClick","SwapPistolSetting"})InstallInputGuard(harmony,typeof(TorpedoLauncherUI),name);
            // Physical device handlers can receive manually dispatched clicks in
            // addition to UGUI raycasts. Suppress local input only while our
            // modal editor is open; flooding/launch commands are not patched.
            foreach(string typeName in new[]{"UBOAT.Game.Scene.Items.FPPInteractionObject","UBOAT.Game.Scene.Items.Dial","UBOAT.Game.Scene.Items.PresetDial","UBOAT.Game.Scene.Items.InterpolatedPresetDial"}){
                var type=AccessTools.TypeByName(typeName);if(type==null)continue;
                // FPPInteractionObject.OnClick is abstract in the supplied game.
                // Guard its concrete pointer entry instead of patching no IL body.
                foreach(string name in new[]{"OnPointerClick","OnPointerDown","OnDrag","OnScroll"})InstallInputGuard(harmony,type,name);
            }
        }
        internal static bool SupportsInputGuard(MethodInfo method)
        {return method!=null&&!method.IsAbstract&&!method.ContainsGenericParameters&&method.GetMethodBody()!=null;}
        static void InstallInputGuard(Harmony harmony,Type type,string name)
        {
            MethodInfo method=null;bool attempted=false;
            var prefix=AccessTools.Method(typeof(StockUi),"BeforeRoomPointer");
            try{
                method=AccessTools.Method(type,name);
                if(!SupportsInputGuard(method)||method.DeclaringType!=type)return;
                attempted=true;harmony.Patch(method,new HarmonyMethod(prefix));
            }catch(Exception e){
                // A supplementary click guard must not tear down depth/program
                // authority, the room UI, or the full-screen raycast shield.
                if(attempted)try{harmony.Unpatch(method,prefix);}catch(Exception cleanup){Debug.LogWarning("[TorpedoRoom] Input guard cleanup: "+cleanup.GetBaseException().Message);}
                Debug.LogWarning("[TorpedoRoom] Auxiliary input guard skipped: "+type.Name+"."+name+"; room UI remains active. "+e.GetBaseException().Message);
            }
        }
        static bool BeforeRoomPointer(){return !Runtime.RoomEditorOpen;}
        static ProgramValues Read(PlayerShip ship){return new ProgramValues(ship.TorpedoProgramDirection,ship.TorpedoProgramDistance,ship.TorpedoProgramAngleAbsolute);}
        static void Write(PlayerShip ship,ProgramValues value){ship.TorpedoProgramDirection=value.Right;ship.TorpedoProgramDistance=value.Distance;ship.TorpedoProgramAngleAbsolute=value.Angle;}
        internal sealed class EditState
        {internal PlayerShip Ship;internal ProgramValues Original,Before;internal bool Restored,DirectionClick;}
        static bool BeforeEdit(bool ___isValidation,out EditState __state,MethodBase __originalMethod=null,object[] __args=null)
        {
            __state=null;var ship=Runtime.Ship;
            if(!Runtime.Active||Runtime.Loading||___isValidation||ship==null)return true;
            if(Runtime.RoomEditorOpen)return false;
            if(Runtime.CurrentGroup==null&&Runtime.Viewport!=ViewportMode.Synchronized)return false;
            bool direction=__originalMethod!=null&&(__originalMethod.Name=="OnProgramLeftChanged"||__originalMethod.Name=="OnProgramRightChanged");
            if(direction&&(__args==null||__args.Length==0||!(__args[0] is bool)||!(bool)__args[0]))return true;
            var original=Read(ship);var selected=SelectedProgram();
            var before=selected.Count>0?new ProgramValues(selected.MixedRight?original.Right:selected.Values.Right,selected.MixedDistance?original.Distance:selected.Values.Distance,selected.MixedAngle?original.Angle:selected.Values.Angle):original;
            __state=new EditState{Ship=ship,Original=original,Before=before,DirectionClick=direction};Write(ship,before);return true;
        }
        static void AfterEdit(EditState __state)
        {
            var ship=Runtime.Ship;if(__state==null||ship==null||ship!=__state.Ship)return;
            var after=Read(ship);var before=__state.Before;
            // Keep only the user's changed dimensions in the native shared controls.
            var shared=new ProgramValues(__state.DirectionClick||before.Right!=after.Right?after.Right:__state.Original.Right,before.Distance!=after.Distance?after.Distance:__state.Original.Distance,before.Angle!=after.Angle?after.Angle:__state.Original.Angle);
            Write(ship,shared);__state.Restored=true;
            if(Runtime.Loading||!Runtime.Active)return;
            var edits=new List<TubeSnapshot>();var tubes=ship.TorpedoLaunchers;if(tubes==null)return;
            for(int i=0;i<tubes.Length;i++){
                var tube=tubes[i];if(tube==null||!tube.IsMarkedForLaunch||!tube.IsLoaded||!Runtime.Stable(tube))continue;
                var torpedo=Runtime.Missile(tube.Torpedo);if(torpedo==null||torpedo.ProgramType==Torpedo.TorpedoProgramType.None)continue;
                var status=Runtime.Status(tube.Torpedo,!Bridge.IsRemote);if(status==null)continue;
                var edit=status.Value.Copy();
                bool changed=ProgramValues.Merge(edit,before,after);
                if(__state.DirectionClick&&edit.Right!=after.Right){edit.Right=after.Right;changed=true;}
                if(!changed)continue;
                edits.Add(new TubeSnapshot{Index=i,Settings=edit});
            }
            if(edits.Count==0)return;
            if(Bridge.IsRemote)Bridge.RequestViewport(edits.ToArray());else Runtime.AcceptViewport(edits.ToArray(),null);
        }
        static Exception EditFinally(Exception __exception,EditState __state)
        {if(__state!=null&&!__state.Restored){Write(__state.Ship,__state.Original);__state.Restored=true;}return __exception;}
        internal sealed class DisplayState
        {internal PlayerShip Ship;internal ProgramValues Before;internal bool Restored;}
        internal sealed class Selection
        {
            internal int Count,Straight;internal bool HasLut,MixedRight,MixedDistance,MixedAngle;
            internal ProgramValues Values;
        }
        internal static Selection SelectedProgram()
        {
            var result=new Selection();var ship=Runtime.Ship;if(ship==null||ship.TorpedoLaunchers==null)return result;
            foreach(var tube in ship.TorpedoLaunchers){
                if(tube==null||!tube.IsMarkedForLaunch||!tube.IsLoaded)continue;
                var missile=Runtime.Missile(tube.Torpedo);if(missile==null||missile.ProgramType==Torpedo.TorpedoProgramType.None)continue;
                var status=Runtime.Status(tube.Torpedo,false);var p=status==null?null:status.Value;
                if(p!=null&&Runtime.LockoutEnabled&&p.StraightRun){result.Straight++;continue;}
                var values=p!=null&&p.ProgramOverride?new ProgramValues(p.Right,p.Distance,p.Angle):Read(ship);
                if(result.Count++==0)result.Values=values;
                else {result.MixedRight|=values.Right!=result.Values.Right;result.MixedDistance|=Math.Abs(values.Distance-result.Values.Distance)>.01f;}
                // Heading comparisons apply to LUT weapons only; FAT has no LUT bearing.
                if(missile.ProgramType==Torpedo.TorpedoProgramType.LUT){
                    if(result.HasLut)result.MixedAngle|=Math.Abs(values.Angle-result.Values.Angle)>.01f;
                    else result.Values.Angle=values.Angle;
                    result.HasLut=true;
                }
            }
            return result;
        }
        internal static Preparation SingleMarked()
        {
            var ship=Runtime.Ship;if(ship==null||ship.TorpedoLaunchers==null)return null;
            Preparation result=null;int count=0;
            foreach(var tube in ship.TorpedoLaunchers){
                if(tube==null||!tube.IsMarkedForLaunch||!tube.IsLoaded)continue;
                count++;var s=Runtime.Status(tube.Torpedo,false);result=s==null?null:s.Value;
            }
            return count==1?result:null;
        }
        static void BeforeDisplay(out DisplayState __state)
        {
            __state=null;if(!Runtime.Active||Runtime.Loading)return;
            var selected=SelectedProgram();var ship=Runtime.Ship;
            if(selected.Count==0||ship==null)return;
            __state=new DisplayState{Ship=ship,Before=Read(ship)};
            // Display-only projection. The native validation guard suppresses input callbacks.
            Write(ship,selected.Values);
        }
        static void Restore(DisplayState state)
        {if(state==null||state.Restored)return;state.Restored=true;if(state.Ship!=null)Write(state.Ship,state.Before);}
        static void AfterDisplay(DisplayState __state,GameObject ___programParameters,TextMeshProUGUI ___programSettingsName,TMP_InputField ___programDistance,TMP_InputField ___programAngle,TorpedoLaunchersUI __instance)
        {
            Restore(__state);if(!Runtime.Active||Runtime.Loading)return;
            var selected=SelectedProgram();
            bool locked=selected.Count==0&&selected.Straight>0;
            bool editable=Runtime.CurrentGroup!=null||Runtime.Viewport==ViewportMode.Synchronized;
            if(___programParameters!=null){
                ___programParameters.SetActive(selected.Count>0);
                var access=___programParameters.GetComponent<CanvasGroup>();if(access==null)access=___programParameters.AddComponent<CanvasGroup>();access.interactable=editable;
            }
            // Vanilla's counters exclude dry weapons and can leave this child hidden
            // after deselection. Restore only the parameter row, never firing controls.
            if(___programAngle!=null&&___programAngle.transform.parent!=null)___programAngle.transform.parent.gameObject.SetActive(selected.HasLut);
            if(selected.Count>0){
                if(___programDistance!=null&&!___programDistance.isFocused)___programDistance.SetTextWithoutNotify(selected.MixedDistance?"—":selected.Values.Distance.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" m");
                if(___programAngle!=null&&!___programAngle.isFocused)___programAngle.SetTextWithoutNotify(selected.MixedAngle?"—":selected.Values.Angle.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"°");
                SetDirection(__instance,selected);
            }
            if(___programSettingsName!=null)___programSettingsName.text=locked?"STRAIGHT RUN":selected.Count==0?"FAT/LUT SETTINGS":(selected.HasLut?"LUT":"FAT")+" — "+(editable?"synchronized":"read-only")+(selected.Straight>0?" · includes straight":"");
        }
        static readonly FieldInfo LeftField=AccessTools.Field(typeof(TorpedoLaunchersUI),"programLeft"),RightField=AccessTools.Field(typeof(TorpedoLaunchersUI),"programRight");
        static void SetDirection(TorpedoLaunchersUI owner,Selection selection)
        {
            if(owner==null)return;
            var left=LeftField==null?null:LeftField.GetValue(owner) as UnityEngine.UI.Toggle;
            var right=RightField==null?null:RightField.GetValue(owner) as UnityEngine.UI.Toggle;
            if(left!=null)left.SetIsOnWithoutNotify(!selection.MixedRight&&!selection.Values.Right);
            if(right!=null)right.SetIsOnWithoutNotify(!selection.MixedRight&&selection.Values.Right);
        }
        static void SelectionValidated(TorpedoLaunchersUI __instance){Refresh(__instance);}
        static Exception DisplayFinally(Exception __exception,DisplayState __state){Restore(__state);return __exception;}
        static readonly MethodInfo DisplayMethod=AccessTools.Method(typeof(TorpedoLaunchersUI),"SetProgramDisplayValues");
        static readonly FieldInfo DistanceField=AccessTools.Field(typeof(TorpedoLaunchersUI),"programDistance"),AngleField=AccessTools.Field(typeof(TorpedoLaunchersUI),"programAngle");
        internal static bool Refresh(TorpedoLaunchersUI owner)
        {
            if(owner==null||!owner.IsOpened||!Runtime.Active||Runtime.Loading)return false;
            var distance=DistanceField==null?null:DistanceField.GetValue(owner) as TMP_InputField;
            var angle=AngleField==null?null:AngleField.GetValue(owner) as TMP_InputField;
            if((distance!=null&&distance.isFocused)||(angle!=null&&angle.isFocused))return false;
            if(DisplayMethod==null)return false;DisplayMethod.Invoke(owner,null);return true;
        }
        static void PanelEnabled(TorpedoLaunchersUI __instance)
        {
            NativePresentation.Panel(__instance);
            var refresh=__instance.GetComponent<StockPanelRefresh>();if(refresh==null)refresh=__instance.gameObject.AddComponent<StockPanelRefresh>();refresh.Owner=__instance;refresh.Reset();
            var launchers=__instance.Launchers;if(launchers!=null)foreach(var tube in launchers)if(tube!=null)TubeEnabled(tube);
        }
        static void TubeEnabled(TorpedoLauncherUI __instance)
        {
            var badge=__instance.GetComponent<TubeProgramBadge>();if(badge==null)badge=__instance.gameObject.AddComponent<TubeProgramBadge>();badge.Owner=__instance;
        }
    }
    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class StockPanelRefresh : MonoBehaviour
    {
        internal TorpedoLaunchersUI Owner;
        float next,nextError;
        internal void Reset(){next=nextError=0;}
        void Update()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;
            try{StockUi.Refresh(Owner);if(Owner!=null)NativePresentation.Refresh(Owner);}catch(Exception e){next=Time.unscaledTime+1;if(Time.unscaledTime>=nextError){nextError=Time.unscaledTime+10;Debug.LogWarning("[TorpedoRoom] Stock field refresh will retry: "+e);}}
        }
    }
    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class TubeProgramBadge : MonoBehaviour
    {
        internal TorpedoLauncherUI Owner;
        TextMeshProUGUI label;
        float next;
        void Update()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;
            if(Owner==null||!Runtime.Active||Runtime.Loading||(Runtime.CurrentGroup==null&&Runtime.Viewport==ViewportMode.Hidden)){if(label!=null)label.gameObject.SetActive(false);return;}
            var tube=Owner.Launcher;var p=tube==null?null:Runtime.Status(tube.Torpedo,false);
            var torpedo=tube==null?null:Runtime.Missile(tube.Torpedo);
            if(p==null||torpedo==null){if(label!=null)label.gameObject.SetActive(false);return;}
            if(label==null){
                var go=new GameObject("TorpedoRoomProgram",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(transform,false);
                label=go.AddComponent<TextMeshProUGUI>();
                var source=GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach(var text in source)if(text!=label&&text.font!=null){label.font=text.font;break;}
                if(label.font==null)label.font=TMP_Settings.defaultFontAsset;
                label.fontSize=11;label.enableAutoSizing=true;label.fontSizeMin=8;label.fontSizeMax=11;
                label.color=new Color(.9f,.8f,.5f);label.raycastTarget=false;label.richText=false;label.alignment=TextAlignmentOptions.Bottom;
                var rect=label.rectTransform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,1);rect.sizeDelta=new Vector2(0,30);
            }
            label.gameObject.SetActive(true);
            var ship=Runtime.Ship;var value=p.Value;bool perTube=value.ProgramOverride;
            label.text=ReportText.Tube(torpedo.ProgramType!=Torpedo.TorpedoProgramType.None,torpedo.ProgramType==Torpedo.TorpedoProgramType.LUT,Runtime.LockoutEnabled&&value.StraightRun,
                perTube?value.Right:ship.TorpedoProgramDirection,perTube?value.Distance:ship.TorpedoProgramDistance,perTube?value.Angle:ship.TorpedoProgramAngleAbsolute,value.DepthOverride?value.Depth:ship.TorpedoAttackSettings.TorpedoDepth);
            var warning=ViewingFire.TubeDepthWarning(ship.Depth,tube.Torpedo.Data==null?null:tube.Torpedo.Data.Name);
            if(warning.Length>0)label.text+="\n"+warning;
            label.color=warning.Length>0?new Color(1,.55f,.3f):new Color(.9f,.8f,.5f);
            label.rectTransform.sizeDelta=new Vector2(0,warning.Length>0?44:30);
        }
        void OnDestroy(){if(label!=null)UnityEngine.Object.Destroy(label.gameObject);}
    }
}
