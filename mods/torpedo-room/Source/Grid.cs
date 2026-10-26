// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Globalization;
using System.Collections.Generic;
using UBOAT.Game.Core.Input;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Landomatic.TorpedoRoom
{
    public sealed partial class Runtime
    {
        bool open,awaiting,dirty,settingFields,gridReady;
        Snapshot awaitingSnapshot;float awaitingUntil;
        string gridStage="idle";
        float nextGridRender,nextRoomMask,nextBuildAttempt,speedFactor;
        bool roomTakeover;
        TorpedoLauncherGroup gridGroup;
        int selected=-1;
        Preparation draft;
        StoredTorpedo selectedItem;
        readonly Dictionary<int,TubeUiState> tubeUiStates=new Dictionary<int,TubeUiState>();
        GameInput blocked;
        CursorManager cursor;
        static readonly object CursorFlag=new object();
        const string InputOwner="Landomatic.TorpedoRoom.Grid";
        Canvas roomCanvas;CanvasScaler roomScaler;
        GameObject gridRoot,gridPanel,editor,programRow,programFields,tubeViewport,dockBackdrop,clickBlocker;
        internal static bool RoomOverviewActive {get{return Instance!=null&&Instance.roomTakeover&&Instance.gridRoot!=null&&Instance.gridRoot.activeSelf&&Config.Interface&&Active&&!Loading&&!Instance.OrdnanceOpen&&CurrentGroup!=null;}}
        internal static bool RoomEditorOpen {get{return Instance!=null&&Instance.open&&Config.Interface&&Active&&!Loading&&CurrentGroup!=null;}}
        Text banner,notice,selectionLabel,policyLabel,pistolLabel,programLabel,turnLabel,pinLabel,closeLabel,applyLabel,applyState;
        Button pistolButton,programButton,turnButton,pinButton,applyButton,ordnanceButton;
        InputField distanceInput,angleInput,depthInput,speedInput;
        Text angleLabel,runLabel;
        PreparationDial depthDial,runDial,headingDial,speedDial;
        GameObject speedDialRoot;Text speedTitle;Button slowerSpeed,fasterSpeed;
        GameObject runDialRoot,headingDialRoot;
        readonly List<NativeSkin.Visual> tubeVisuals=new List<NativeSkin.Visual>(),dialVisuals=new List<NativeSkin.Visual>();
        RectTransform cardsContent;
        CanvasGroup editorAccess;
        Font gridFont;
        readonly List<int> cardIndices=new List<int>();
        readonly List<Button> cards=new List<Button>(),cardPistols=new List<Button>();
        readonly List<Text> cardPistolLabels=new List<Text>();
        readonly List<Image> cardIcons=new List<Image>();
        readonly List<PrepSelection.Frame> cardOutlines=new List<PrepSelection.Frame>();
        readonly List<Text> cardLabels=new List<Text>();
        static readonly Color Brass=new Color(.86f,.74f,.46f),Paper=new Color(.91f,.91f,.83f),Dark=new Color(.08f,.13f,.14f,.98f);
        void UpdateGrid()
        {
            var group=CurrentGroup;
            if(OrdnanceOpen){
                if(roomTakeover||open||group!=null)ObserveOrdnance();
                if(!roomHandoff.Dismissed&&(roomTakeover||open||group!=null))DismissRoom();
                roomHandoff.CanShow(group,RoomStationToken,true,false);
                if(roomHandoff.Dismissed&&Config.Interface&&Active&&Time.unscaledTime>=nextRoomMask){nextRoomMask=Time.unscaledTime+.2f;NativeSkin.Capture();NativePresentation.Room(true);}
                return;
            }
            bool pressed=Shortcut.Pressed(true);
            bool atStation=Config.Interface&&Active&&!Loading&&Ship!=null&&group!=null;
            bool takeover=roomHandoff.CanShow(atStation?group:null,RoomStationToken,false,atStation&&pressed)&&atStation;
            if(takeover!=roomTakeover||Time.unscaledTime>=nextRoomMask){roomTakeover=takeover;nextRoomMask=Time.unscaledTime+.2f;if(atStation)NativeSkin.Capture();NativePresentation.Room(atStation);}
            if(!takeover){NativeSkin.RestoreStations();CloseGrid();if(gridRoot!=null)gridRoot.SetActive(false);return;}
            if(!gridReady&&!TryBuildGrid(pressed))return;
            if(!gridRoot.activeSelf)gridRoot.SetActive(true);
            UiScale.Match(roomCanvas,roomScaler);
            NativeSkin.RoomStations(gridRoot.transform);NativeSkin.RoomExit(gridRoot.transform);
            if(open&&gridGroup!=CurrentGroup)CloseGrid();
            if(pressed){if(open)CloseGrid();else TryOpenGrid();}
            if(open&&Input.GetKeyDown(KeyCode.Escape))CloseGrid();
            if(Time.unscaledTime>=nextGridRender)banner.text=Shortcut.Display+"  TORPEDO ROOM";
            banner.gameObject.SetActive(!open);
            if(!open){if(Time.unscaledTime>=nextGridRender){nextGridRender=Time.unscaledTime+.2f;RenderGrid();}return;}
            RefreshSelectedWeapon();
            if(awaiting&&((Bridge.Last!=awaitingSnapshot&&Bridge.Last!=null&&!string.IsNullOrEmpty(Bridge.Last.Notice))||Time.unscaledTime>=awaitingUntil)){
                var reply=selected>=0&&selected<Ship.TorpedoLaunchers.Length?Status(Ship.TorpedoLaunchers[selected].Torpedo,false):null;
                if(reply!=null&&draft!=null&&reply.Value.Id==draft.Id&&reply.Value.Revision>draft.Revision)Select(selected);
                else {awaiting=false;dirty=true;if(Time.unscaledTime>=awaitingUntil)Say("No host confirmation yet. Refresh before retrying Apply.");}
            }
            if((awaiting||!dirty)&&draft!=null&&selected>=0&&selected<Ship.TorpedoLaunchers.Length){
                var s=Status(Ship.TorpedoLaunchers[selected].Torpedo,false);
                if(s!=null&&s.Value.Id==draft.Id&&s.Value.Revision>draft.Revision&&!distanceInput.isFocused&&!angleInput.isFocused&&!depthInput.isFocused)Select(selected);
            }
            if(Time.unscaledTime>=nextGridRender){nextGridRender=Time.unscaledTime+.2f;RenderGrid();NativePresentation.Room(true);}
        }
        bool TryBuildGrid(bool force=false)
        {
            if(gridReady)return true;
            if(!force&&Time.unscaledTime<nextBuildAttempt)return false;
            nextBuildAttempt=Time.unscaledTime+2;
            try{gridStage="native templates";NativeSkin.Capture();ResetGrid();BuildGrid();gridReady=true;SetGridLayout(false);return true;}
            catch(Exception e){string stage=gridStage;ResetGrid();Say("Room interface construction failed ("+stage+"). See output_log.txt; disabling the room interface restores native graphics.");Debug.LogError("[TorpedoRoom] Room interface stage="+stage+": "+e);return false;}
        }
        void TryOpenGrid()
        {
            try{
                gridStage="native templates";NativeSkin.Capture();
                if(!gridReady&&!TryBuildGrid(true))return;
                gridStage="station input";OpenGrid();
                if(!open)return;
                gridStage="tube graphics";RenderGrid();banner.gameObject.SetActive(false);
                gridStage="open";Debug.Log("[TorpedoRoom] Room interface opened; tubes="+cardIndices.Count+" shortcut="+Shortcut.Display);
            }catch(Exception e){
                string stage=gridStage;ResetGrid();
                Say("Room interface could not open ("+stage+"). See output_log.txt; disabling the room interface restores native graphics.");
                Debug.LogError("[TorpedoRoom] Room interface stage="+stage+": "+e);
            }
        }
        void OpenGrid()
        {
            NativeSkin.Capture();
            selected=-1;selectedItem=null;draft=null;awaiting=false;dirty=false;nextGridRender=0;gridGroup=CurrentGroup;
            blocked=Get<GameInput>();cursor=Get<CursorManager>();
            if(blocked==null||cursor==null){CloseGrid();Say("Preparation input is not ready; retry at the tube station.");return;}
            blocked.AddBlockade(InputOwner);cursor.AddCursorVisibilityFlag(CursorFlag);
            open=true;gridRoot.SetActive(true);SetGridLayout(true);
        }
        void CloseGrid()
        {
            HideTubeHover();
            foreach(var border in cardOutlines)border.Select(false);
            if(blocked!=null)blocked.RemoveBlockade(InputOwner);
            if(cursor!=null)cursor.RemoveCursorVisibilityFlag(CursorFlag);
            blocked=null;cursor=null;open=false;selected=-1;selectedItem=null;draft=null;awaiting=false;
            if(gridReady)SetGridLayout(false);
            else if(gridRoot!=null)gridRoot.SetActive(false);
        }
        bool CompactOverview {get{return Ship!=null&&Ship.TorpedoLaunchers!=null&&Ship.TorpedoLaunchers.Length==3;}}
        void SetGridLayout(bool expanded)
        {
            if(gridPanel==null||gridRoot==null||tubeViewport==null)return;
            gridPanel.SetActive(expanded);
            if(clickBlocker!=null)clickBlocker.SetActive(expanded);
            dockBackdrop.SetActive(!expanded);
            if(ordnanceButton!=null)ordnanceButton.gameObject.SetActive(!expanded);
            var rect=(RectTransform)tubeViewport.transform;
            rect.SetParent(expanded?gridPanel.transform:gridRoot.transform,false);
            rect.localScale=expanded?Vector3.one:new Vector3(1.1f,1.1f,1);
            if(expanded)Place(rect,16,46,918,240);
            else {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(CompactOverview?1:.5f,0);rect.anchoredPosition=new Vector2(CompactOverview?-24:0,84);rect.sizeDelta=new Vector2(CompactOverview?450:918,240);}
            var backing=(RectTransform)dockBackdrop.transform;backing.anchorMin=backing.anchorMax=backing.pivot=new Vector2(CompactOverview?1:.5f,0);backing.anchoredPosition=new Vector2(CompactOverview?-18:0,28);backing.sizeDelta=new Vector2(CompactOverview?470:930,300);
            if(ordnanceButton!=null){var o=(RectTransform)ordnanceButton.transform;o.anchorMin=o.anchorMax=o.pivot=new Vector2(CompactOverview?1:0.5f,0);o.anchoredPosition=new Vector2(CompactOverview?-42:-355,38);}
            cardIndices.Clear();
            tubeViewport.SetActive(true);
        }
        void ChooseTube(int index)
        {if(!open){TryOpenGrid();if(!open)return;}Select(index);}
        void Select(int i)
        {
            var tubes=Ship.TorpedoLaunchers;if(i<0||i>=tubes.Length)return;
            var item=tubes[i]==null?null:tubes[i].Torpedo;var s=Status(item,!Bridge.IsRemote);selectedItem=item;
            selected=i;draft=s==null?null:s.Value.Copy();awaiting=false;dirty=false;nextGridRender=0;settingFields=true;
            if(draft!=null){depthInput.text=(draft.DepthOverride?draft.Depth:Ship.TorpedoAttackSettings.TorpedoDepth).ToString("0.##",CultureInfo.InvariantCulture);distanceInput.text=draft.Distance.ToString("0",CultureInfo.InvariantCulture);angleInput.text=draft.Angle.ToString("0.#",CultureInfo.InvariantCulture);}
            if(draft!=null){
                RefreshSpeedUnits(true);
            }
            settingFields=false;
        }
        void RefreshSelectedWeapon()
        {
            if(selected<0||Ship==null||selected>=Ship.TorpedoLaunchers.Length)return;
            var tube=Ship.TorpedoLaunchers[selected];var item=tube==null?null:tube.Torpedo;
            var status=Status(item,!Bridge.IsRemote);
            if(TubeUiState.NeedsRebind(selectedItem==item,draft,status==null?null:status.Value)){
                HideTubeHover();Select(selected);
            }
        }
        void RefreshSpeedUnits(bool force=false)
        {
            float factor=SpeedUnits.Factor;if(!force&&factor==speedFactor)return;
            if(draft==null||Ship==null||selected<0||selected>=Ship.TorpedoLaunchers.Length)return;
            bool previous=settingFields;settingFields=true;
            try{
                speedFactor=factor;var tube=Ship.TorpedoLaunchers[selected];var options=SpeedControl.Options(tube.Torpedo);
                speedDial.Choices=new float[options.Length];for(int n=0;n<options.Length;n++)speedDial.Choices[n]=options[n]*factor;
                speedDial.Minimum=options.Length==0?0:speedDial.Choices[0];speedDial.Maximum=options.Length==0?1:speedDial.Choices[options.Length-1];
                speedInput.text=(SpeedControl.Effective(tube.Torpedo,draft,Ship.TorpedoAttackSettings.TorpedoSpeed)*factor).ToString("0.0",CultureInfo.InvariantCulture);
            }finally{settingFields=previous;}
        }
        void ApplyDraft()
        {
            if(draft==null||!Editable())return;
            float d=draft.Distance,a=draft.Angle,depth;
            if(!float.TryParse(depthInput.text,NumberStyles.Float,CultureInfo.InvariantCulture,out depth)||!Preparation.Finite(depth)||depth<1||depth>14){Say("Enter launch depth 1–14 m.");return;}
            if(draft.SpeedOverride){float speed;if(!float.TryParse(speedInput.text,NumberStyles.Float,CultureInfo.InvariantCulture,out speed)||!Preparation.Finite(speed)){Say("Choose a supported torpedo speed.");return;}draft.Speed=SpeedValues.Nearest(SpeedControl.Options(Ship.TorpedoLaunchers[selected].Torpedo),speed/speedFactor);}
            bool parse=ProgramValues.ShowFields(draft,LockoutAllowed)&&draft.ProgramOverride;
            if(parse&&(!float.TryParse(distanceInput.text,NumberStyles.Float,CultureInfo.InvariantCulture,out d)||!float.TryParse(angleInput.text,NumberStyles.Float,CultureInfo.InvariantCulture,out a)
                ||!Preparation.Finite(d)||!Preparation.Finite(a)||d<500||d>15000||a<0||a>360)){Say("Enter run 500–15000 m and bearing 0–360 degrees (360 = north).");return;}
            if(parse)a=DialValues.Compass(a,.1f);
            draft.Depth=DialValues.Snap(depth,1,14,.25f);draft.DepthOverride=true;draft.Distance=d;draft.Angle=a;
            if(Request(selected,draft.Copy())){if(Bridge.IsRemote){awaiting=true;awaitingSnapshot=Bridge.Last;awaitingUntil=Time.unscaledTime+5;}else Select(selected);}
        }
        void StepSpeed(float direction)
        {
            if(!Editable()||speedDial.Choices==null||speedDial.Choices.Length<=1)return;
            float current;if(float.TryParse(speedInput.text,NumberStyles.Float,CultureInfo.InvariantCulture,out current))speedDial.Write(SpeedValues.Nudge(speedDial.Choices,current,direction));
        }
        bool Editable()
        {
            if(draft==null||Ship==null||selected<0||selected>=Ship.TorpedoLaunchers.Length)return false;
            var tube=Ship.TorpedoLaunchers[selected];if(tube==null)return false;
            var s=Status(tube.Torpedo,false);
            return Active&&!awaiting&&PistolSeconds(selected)<=0&&s!=null&&s.Value.Id==draft.Id&&RoomEditable(tube.Group)&&Stable(tube)&&(!Bridge.IsRemote||Bridge.Synced);
        }
        void RenderGrid()
        {
            RefreshSelectedWeapon();
            closeLabel.text="Back · "+Shortcut.Display;notice.text=Notice;var speedWarning=TdcSpeed.Warning();if(speedWarning.Length>0)notice.text=speedWarning;
            foreach(var dial in dialVisuals)NativeSkin.SizeDial(dial);
            var tubes=Ship.TorpedoLaunchers;
            var visible=new List<int>();for(int i=0;i<tubes.Length;i++)if(tubes[i]!=null&&(Access!=RoomAccess.CurrentCompartment||tubes[i].Group==CurrentGroup))visible.Add(i);
            bool changed=visible.Count!=cardIndices.Count;
            if(!changed)for(int n=0;n<visible.Count;n++)if(visible[n]!=cardIndices[n]){changed=true;break;}
            if(changed){
                HideTubeHover();
                foreach(var b in cards){b.gameObject.SetActive(false);UnityEngine.Object.Destroy(b.gameObject);}
                cards.Clear();cardPistols.Clear();cardPistolLabels.Clear();cardIcons.Clear();cardOutlines.Clear();tubeVisuals.Clear();cardLabels.Clear();cardIndices.Clear();cardIndices.AddRange(visible);
                float rowWidth=!open&&CompactOverview?450:918;
                cardsContent.sizeDelta=new Vector2(Mathf.Max(rowWidth,visible.Count*150),236);
                for(int n=0;n<visible.Count;n++){
                    int index=visible[n];Text label;
                    var b=GridButton(cardsContent,"Tube "+index,"",Mathf.Max(0,(rowWidth-visible.Count*150)/2)+n*150,0,144,236,()=>ChooseTube(index),out label);
                    label.alignment=TextAnchor.UpperCenter;label.fontSize=12;Place(label.rectTransform,2,156,140,80);
                    GridLabel(b.transform,"Tube number","TUBE "+(index+1),4,0,82,20,12).alignment=TextAnchor.MiddleCenter;
                    Text pistol;var switchPistol=GridButton(b.transform,"Pistol mode","",90,0,50,20,()=>{if(!open||selected!=index)ChooseTube(index);if(Editable()){draft.Pistol=1-draft.Pistol;DraftChanged();}},out pistol);
                    pistol.fontSize=13;cardPistols.Add(switchPistol);cardPistolLabels.Add(pistol);
                    var icon=GridPanel(b.transform,"Native torpedo icon",new Color(1,1,1,1));Place(icon.rectTransform,28,25,88,100);icon.preserveAspect=true;icon.raycastTarget=false;cardIcons.Add(icon);
                    var holder=new GameObject("Native tube frame",typeof(RectTransform));holder.transform.SetParent(b.transform,false);Place((RectTransform)holder.transform,0,24,144,126);
                    var visual=NativeSkin.Tube(NativeSkin.TubeView(index),holder.transform,144,126);tubeVisuals.Add(visual);
                    if(visual!=null&&visual.Sources.Count>0){b.GetComponent<Image>().color=Color.clear;icon.gameObject.SetActive(false);}else b.GetComponent<Image>().color=Color.clear;
                    var hover=b.gameObject.AddComponent<TorpedoHover>();hover.Enter=()=>ShowTubeHover(index);hover.Exit=HideTubeHover;
                    cardOutlines.Add(PrepSelection.Create(b.transform,Brass));
                    cards.Add(b);cardLabels.Add(label);
                }
            }
            for(int n=0;n<cardIndices.Count;n++){
                int i=cardIndices[n];var tube=tubes[i];var item=tube.Torpedo;var missile=Missile(item);var s=Status(item,!Bridge.IsRemote);
                TubeUiState observed;if(!tubeUiStates.TryGetValue(i,out observed)){observed=new TubeUiState();tubeUiStates[i]=observed;}
                bool stateChanged=observed.Observe((int)tube.CurrentState,s==null?null:s.Value.Id,item!=null,Stable(tube));
                if(stateChanged)NativeSkin.RefreshTubeIcon(i);
                
                if(tubeVisuals[n]!=null){var v=tubeVisuals[n];v.Sync();bool closed=ProgramValues.DoorClosed(tube.IsFlooded,(int)tube.CurrentState);if(v.OpenCover!=null)v.OpenCover.enabled=!closed;if(v.ClosedCover!=null)v.ClosedCover.enabled=closed;}
                var sprite=item==null?null:NativePresentation.SpriteFor(i)??NativeSkin.TubeSprite(i);if(tubeVisuals[n]!=null){var v=tubeVisuals[n];if(v.Icon!=null){v.Icon.enabled=item!=null&&sprite!=null;v.Icon.sprite=sprite;}}cardIcons[n].sprite=sprite;cardIcons[n].enabled=item!=null&&sprite!=null;
                cardPistols[n].interactable=!awaiting&&s!=null&&RoomEditable(tube.Group)&&Stable(tube)&&(!Bridge.IsRemote||Bridge.Synced);
                cardPistolLabels[n].text=s==null?"—":s.Value.Pistol==0?"AZ":"MZ";
                string info=tube.Group==CurrentGroup?"":"OPPOSITE "+(Access==RoomAccess.AllEditable?"":"· READ ONLY")+"\n";
                if(item==null)info+="EMPTY";
                else {
                    
                    if(s==null)info+="Awaiting host\n";
                    if(missile!=null&&missile.ProgramType!=UBOAT.Game.Scene.Entities.Torpedo.TorpedoProgramType.None&&s!=null)
                        info+=missile.ProgramType+" "+(s.Value.StraightRun&&LockoutAllowed?"STRAIGHT RUN":s.Value.ProgramOverride?s.Value.Distance.ToString("0")+" m "+(s.Value.Right?"R":"L"):"Last station")+"\n";
                    info+=TubeWaterText(tube)+"\n"+(s!=null&&s.Value.DepthOverride?s.Value.Depth:Ship.TorpedoAttackSettings.TorpedoDepth).ToString("0.##")+" m depth\n"+SpeedUnits.Format(SpeedControl.Effective(item,s==null?null:s.Value,Ship.TorpedoAttackSettings.TorpedoSpeed));
                }
                cardLabels[n].text=info;cardLabels[n].color=i==selected?Brass:Paper;
                cardOutlines[n].Select(open&&i==selected&&draft!=null&&selectedItem==item);
            }
            RefreshTubeHover();
            editor.SetActive(draft!=null);
            if(draft==null){selectionLabel.text="Choose a loaded tube above. Empty tubes and mines have no preparation controls.";return;}
            if(selected<0||selected>=tubes.Length||tubes[selected]==null){draft=null;editor.SetActive(false);return;}
            var current=tubes[selected];var status=Status(current.Torpedo,false);var torpedo=Missile(current.Torpedo);
            float preparing=PistolSeconds(selected);
            bool valid=status!=null&&status.Value.Id==draft.Id,editable=Editable()&&preparing<=0;
            selectionLabel.text="▼ TUBE "+(selected+1)+" · "+(current.Torpedo==null?"EMPTY":current.Torpedo.TypeName)+(!valid?" · Contents changed — select again":status.Value.Revision!=draft.Revision?" · Refresh required":"");
            editorAccess.interactable=editable;
            pistolLabel.text=draft.Pistol==0?"AZ · Contact":"MZ · Magnetic";
            policyLabel.text=TubeUiState.ReadOnlyReason(editable,awaiting,valid,Stable(current),RoomEditable(current.Group),!Bridge.IsRemote||Bridge.Synced,current.CurrentState.ToString());
            applyLabel.text=preparing>0?"PREPARING PISTOL":awaiting?"WAITING FOR HOST":dirty?"APPLY CHANGES":"APPLY";
            applyState.text=preparing>0?"Pistol preparation: "+Mathf.CeilToInt(preparing)+" game seconds remaining":awaiting?"Awaiting confirmation":dirty?"Unapplied changes":"Settings applied";
            applyButton.interactable=editable&&!awaiting;
            var applyImage=applyButton.targetGraphic as Image;
            if(applyImage!=null)applyImage.color=dirty?new Color(.7f,.5f,.16f,1):new Color(.25f,.35f,.32f,1);
            applyLabel.color=dirty?Color.white:Paper;

            RefreshSpeedUnits();depthDial.Refresh();speedDial.Refresh();runDial.Refresh();headingDial.Refresh();
            int speeds=speedDial.Choices==null?0:speedDial.Choices.Length;speedTitle.text="Speed ("+SpeedUnits.Suffix+")"+(speeds==1?" · fixed":"");speedInput.readOnly=true;slowerSpeed.interactable=fasterSpeed.interactable=editable&&speeds>1;
            bool programmed=torpedo!=null&&torpedo.ProgramType!=UBOAT.Game.Scene.Entities.Torpedo.TorpedoProgramType.None;
            programRow.SetActive(programmed);
            if(programmed){
                bool show=ProgramValues.ShowFields(draft,LockoutAllowed);programFields.SetActive(show&&draft.ProgramOverride);
                programLabel.text=draft.ProgramOverride?"Source: Per-tube":"Source: Last station";
                turnLabel.text=draft.Right?"Right turn":"Left turn";
                bool lut=torpedo.ProgramType==UBOAT.Game.Scene.Entities.Torpedo.TorpedoProgramType.LUT;
                bool fields=show&&draft.ProgramOverride;turnButton.gameObject.SetActive(fields);runLabel.gameObject.SetActive(fields);distanceInput.gameObject.SetActive(fields);
                angleInput.gameObject.SetActive(lut&&fields);angleLabel.gameObject.SetActive(lut&&fields);runDialRoot.SetActive(fields);headingDialRoot.SetActive(lut&&fields);
                pinButton.interactable=editable&&LockoutAllowed;
                pinLabel.text=LockoutAllowed?(draft.StraightRun?"Run mode: STRAIGHT":"Run mode: "+torpedo.ProgramType):"Straight mode unavailable";
            }
        }
        void BuildGrid()
        {
            gridStage="canvas and font";NativeSkin.Capture();gridFont=Resources.GetBuiltinResource<Font>("Arial.ttf");
            // Keep the overlay a root canvas; its coordinates must not inherit a
            // non-UI runtime object's transform or a station's UI scaling.
            gridRoot=new GameObject("TorpedoPreparationCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));UnityEngine.Object.DontDestroyOnLoad(gridRoot);
            var canvas=roomCanvas=gridRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=29000;
            var scaler=roomScaler=gridRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;UiScale.Match(canvas,scaler);
            var shield=GridPanel(gridRoot.transform,"Modal input shield",Color.clear);clickBlocker=shield.gameObject;
            var sr=shield.rectTransform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=sr.offsetMax=Vector2.zero;clickBlocker.SetActive(false);
            var dock=GridPanel(gridRoot.transform,"Dock backing",new Color(.035f,.05f,.045f,.88f));dockBackdrop=dock.gameObject;dock.raycastTarget=false;
            var dr=dock.rectTransform;dr.anchorMin=dr.anchorMax=dr.pivot=new Vector2(.5f,0);dr.anchoredPosition=new Vector2(0,28);dr.sizeDelta=new Vector2(930,300);dr.localScale=new Vector3(1.1f,1.1f,1);
            banner=GridLabel(gridRoot.transform,"Hint","",0,0,950,30,13);var br=(RectTransform)banner.transform;br.anchorMin=br.anchorMax=new Vector2(.5f,0);br.pivot=new Vector2(.5f,0);br.anchoredPosition=new Vector2(0,10);
            banner.alignment=TextAnchor.MiddleCenter;
            Text ordnanceLabel;
            ordnanceButton=GridButton(gridRoot.transform,"Manage Ordnance","Manage Ordnance",0,0,280,48,OpenOrdnance,out ordnanceLabel);ordnanceLabel.fontSize=20;
            var ordnanceRect=(RectTransform)ordnanceButton.transform;ordnanceRect.anchorMin=ordnanceRect.anchorMax=new Vector2(.5f,0);ordnanceRect.pivot=Vector2.zero;ordnanceRect.anchoredPosition=new Vector2(-495,38);
            var bg=GridPanel(gridRoot.transform,"Preparation",new Color(.035f,.05f,.045f,.94f));gridPanel=bg.gameObject;
            var rect=bg.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(950,604);rect.localScale=new Vector3(1.1f,1.1f,1);
            if(NativeSkin.Board!=null){bg.sprite=NativeSkin.Board;bg.color=new Color(1,1,1,.94f);bg.type=Image.Type.Sliced;}
            GridLabel(rect,"Title","TORPEDO ROOM",16,8,600,28,18).color=Brass;
            Text unused;GridButton(rect,"Back","Back",760,8,174,26,CloseGrid,out closeLabel);
            var viewport=GridPanel(rect,"Tube viewport",Color.clear);Place(viewport.rectTransform,16,46,918,240);viewport.gameObject.AddComponent<RectMask2D>();
            tubeViewport=viewport.gameObject;
            cardsContent=(RectTransform)new GameObject("Tube graphics",typeof(RectTransform)).transform;cardsContent.SetParent(viewport.transform,false);Place(cardsContent,0,0,918,236);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=cardsContent;scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            selectionLabel=GridLabel(rect,"Selection","Choose a loaded tube",16,290,800,24,14);GridButton(rect,"Refresh","Refresh",834,290,100,24,()=>{if(selected>=0)Select(selected);},out unused);
            editor=GridPanel(rect,"Instruments",Color.clear).gameObject;Place((RectTransform)editor.transform,16,320,918,218);editor.GetComponent<Image>().raycastTarget=false;editorAccess=editor.AddComponent<CanvasGroup>();
            pistolButton=GridButton(editor.transform,"Pistol","",0,0,140,26,()=>{if(Editable()){draft.Pistol=1-draft.Pistol;DraftChanged();}},out pistolLabel);
            policyLabel=GridLabel(editor.transform,"Access","",720,0,190,26,12);
            gridStage="depth instrument";depthDial=MakeDial(editor.transform,"Depth (m)",0,1,14,.25f,()=>Editable(),out depthInput,out depthDialRoot,out depthTitle);
            speedDial=MakeDial(editor.transform,"Speed (kn)",230,0,100,.1f,()=>Editable()&&speedDial.Choices!=null&&speedDial.Choices.Length>1,out speedInput,out speedDialRoot,out speedTitle);
            speedDial.DisplayFormat="0.0";speedInput.readOnly=true;speedInput.transition=Selectable.Transition.None;speedInput.enabled=false;
            speedInput.targetGraphic.raycastTarget=false;var speedBackground=speedInput.targetGraphic as Image;if(speedBackground!=null){speedBackground.sprite=null;speedBackground.color=Color.clear;speedBackground.enabled=false;}
            speedInput.textComponent.raycastTarget=false;speedInput.textComponent.alignment=TextAnchor.MiddleCenter;
            slowerSpeed=GridButton(speedDialRoot.transform,"Slower speed","<",132,86,40,30,()=>StepSpeed(-1),out unused);
            fasterSpeed=GridButton(speedDialRoot.transform,"Faster speed",">",178,86,40,30,()=>StepSpeed(1),out unused);
            speedInput.onValueChanged.AddListener(value=>speedInput.textComponent.text=value);
            speedInput.onValueChanged.AddListener(value=>{if(!settingFields&&draft!=null){float knots;if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out knots)&&Preparation.Finite(knots)){draft.SpeedOverride=true;draft.Speed=SpeedValues.Nearest(SpeedControl.Options(Ship.TorpedoLaunchers[selected].Torpedo),knots/speedFactor);}}});
            programRow=GridPanel(editor.transform,"Program",Color.clear).gameObject;Place((RectTransform)programRow.transform,0,0,918,134);programRow.GetComponent<Image>().raycastTarget=false;
            programButton=GridButton(programRow.transform,"Program source","",155,0,210,26,()=>{if(Editable()){draft.ProgramOverride=!draft.ProgramOverride;DraftChanged();}},out programLabel);
            pinButton=GridButton(programRow.transform,"Run mode","",380,0,180,26,()=>{if(Editable()&&LockoutAllowed){draft.StraightRun=!draft.StraightRun;DraftChanged();}},out pinLabel);
            turnButton=GridButton(programRow.transform,"Turn","",575,0,130,26,()=>{if(Editable()){draft.Right=!draft.Right;DraftChanged();}},out turnLabel);
            programFields=GridPanel(programRow.transform,"Program instruments",Color.clear).gameObject;Place((RectTransform)programFields.transform,0,0,918,134);programFields.GetComponent<Image>().raycastTarget=false;
            gridStage="program instruments";runDial=MakeDial(programFields.transform,"Initial run (m)",460,500,15000,100,()=>Editable()&&draft.ProgramOverride&&ProgramValues.ShowFields(draft,LockoutAllowed),out distanceInput,out runDialRoot,out runLabel);
            headingDial=MakeDial(programFields.transform,"LUT heading °T",690,0,359.9f,.1f,()=>Editable()&&draft.ProgramOverride&&ProgramValues.ShowFields(draft,LockoutAllowed),out angleInput,out headingDialRoot,out angleLabel);headingDial.Compass=true;
            applyButton=GridButton(editor.transform,"Apply","APPLY CHANGES",0,164,340,48,ApplyDraft,out applyLabel);
            applyLabel.fontSize=21;applyLabel.fontStyle=FontStyle.Bold;
            applyState=GridLabel(editor.transform,"Apply state","Settings applied",358,164,540,22,15);applyState.color=Brass;
            GridLabel(editor.transform,"Help","Apply confirms changes to the selected tube.",358,188,540,24,14);
            notice=GridLabel(rect,"Notice","",16,572,918,28,14);depthInput.text="4.0";distanceInput.text="1500";angleInput.text="0.0";gridPanel.SetActive(false);Debug.Log("[TorpedoRoom] UI native dials="+nativeDialCount+"/4 board="+(NativeSkin.Board!=null));
        }
        int nativeDialCount;
        GameObject depthDialRoot;Text depthTitle;
        PreparationDial MakeDial(Transform parent,string title,float x,float min,float max,float step,Func<bool> allowed,out InputField input,out GameObject root,out Text heading)
        {
            var group=GridPanel(parent,title,Color.clear);group.raycastTarget=false;Place(group.rectTransform,x,32,220,124);root=group.gameObject;
            heading=GridLabel(group.transform,"Title",title,132,8,86,34,14);
            input=GridInput(group.transform,title+" value",132,47,86);
            var touch=GridPanel(group.transform,"Dial",new Color(.05f,.05f,.05f,.05f));Place(touch.rectTransform,0,0,124,124);
            var instrumentFace=new GameObject("Instrument face",typeof(RectTransform),typeof(CanvasRenderer),typeof(InstrumentFace));instrumentFace.transform.SetParent(touch.transform,false);Place((RectTransform)instrumentFace.transform,0,0,124,124);instrumentFace.GetComponent<InstrumentFace>().raycastTarget=false;
            var visual=NativeSkin.Dial(touch.transform,124);if(visual!=null&&visual.Needle!=null){nativeDialCount++;dialVisuals.Add(visual);}Transform needle=visual==null?null:visual.Needle;
            if(needle==null){
                // Visible fallback if the native template is unavailable; no firing controls.
                var centre=new GameObject("Needle pivot",typeof(RectTransform));centre.transform.SetParent(touch.transform,false);var pivot=(RectTransform)centre.transform;pivot.anchorMin=pivot.anchorMax=pivot.pivot=new Vector2(.5f,.5f);pivot.anchoredPosition=Vector2.zero;pivot.sizeDelta=Vector2.zero;
                var hand=GridPanel(pivot,"Needle",Brass);hand.raycastTarget=false;hand.rectTransform.anchorMin=hand.rectTransform.anchorMax=new Vector2(.5f,.5f);hand.rectTransform.pivot=new Vector2(.5f,0);hand.rectTransform.anchoredPosition=Vector2.zero;hand.rectTransform.sizeDelta=new Vector2(3,45);needle=pivot;
            }
            var dial=touch.gameObject.AddComponent<PreparationDial>();dial.Input=input;dial.Minimum=min;dial.Maximum=max;dial.Step=step;dial.CanEdit=allowed;dial.Needle=needle;
            var field=input;
            if(visual!=null&&visual.ValueText!=null){var readout=visual.ValueText;readout.color=Paper;field.onValueChanged.AddListener(v=>readout.text=v+(title.Contains("°")?"°T":" m"));NativeSkin.ClickArea(readout.rectTransform,()=>{if(allowed()){field.Select();field.ActivateInputField();}});}
            input.onValueChanged.AddListener(v=>dial.Refresh());return dial;
        }
        void ResetGrid(){NativeSkin.RestoreStations();CloseGrid();if(gridRoot!=null)UnityEngine.Object.Destroy(gridRoot);gridRoot=null;hoverPanel=null;hoverText=null;gridPanel=null;roomCanvas=null;roomScaler=null;gridReady=false;tubeUiStates.Clear();dialVisuals.Clear();cards.Clear();cardPistols.Clear();cardPistolLabels.Clear();cardLabels.Clear();cardIcons.Clear();cardOutlines.Clear();cardIndices.Clear();tubeVisuals.Clear();nativeDialCount=0;}
        void DraftChanged(){if(!settingFields){dirty=true;nextGridRender=0;}}
        static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Image GridPanel(Transform parent,string name,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;return image;}
        Text GridLabel(Transform parent,string name,string value,float x,float y,float w,float h,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
            var text=go.GetComponent<Text>();text.font=gridFont;text.text=value;text.fontSize=size;text.color=Paper;text.raycastTarget=false;text.supportRichText=false;text.alignment=TextAnchor.MiddleLeft;
            Place(text.rectTransform,x,y,w,h);return text;
        }
        Button GridButton(Transform parent,string name,string value,float x,float y,float w,float h,Action action,out Text label)
        {
            var image=GridPanel(parent,name,new Color(.19f,.27f,.28f));if(NativeSkin.ButtonSprite!=null){image.sprite=NativeSkin.ButtonSprite;image.type=Image.Type.Sliced;image.color=Color.white;}Place(image.rectTransform,x,y,w,h);
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());
            label=GridLabel(image.transform,name+" label",value,7,2,w-14,h-4,14);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        InputField GridInput(Transform parent,string name,float x,float y,float w)
        {
            var image=GridPanel(parent,name,new Color(.13f,.19f,.2f));if(NativeSkin.FieldSprite!=null){image.sprite=NativeSkin.FieldSprite;image.type=Image.Type.Sliced;image.color=Color.white;}Place(image.rectTransform,x,y,w,28);
            var text=GridLabel(image.transform,name+" text","",6,2,w-12,24,17);
            var input=image.gameObject.AddComponent<InputField>();input.targetGraphic=image;input.textComponent=text;input.contentType=InputField.ContentType.DecimalNumber;input.characterLimit=7;input.onValueChanged.AddListener(v=>DraftChanged());return input;
        }
        static bool LockoutAllowed {get{return Bridge.IsRemote?Bridge.Last!=null&&Bridge.Last.AllowLockout:Config.AllowLockout;}}
    }
}
