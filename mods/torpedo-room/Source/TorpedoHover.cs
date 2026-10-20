// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using UBOAT.Game.Scene.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class TorpedoHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        internal Action Enter,Exit;
        public void OnPointerEnter(PointerEventData data){if(Enter!=null)Enter();}
        public void OnPointerExit(PointerEventData data){if(Exit!=null)Exit();}
        void OnDisable(){if(Exit!=null)Exit();}
    }
    public sealed partial class Runtime
    {
        int hoverTube=-1;StoredTorpedo hoverItem;Image hoverPanel;Text hoverText;
        void HideTubeHover(){hoverTube=-1;hoverItem=null;if(hoverPanel!=null)hoverPanel.gameObject.SetActive(false);}
        void ShowTubeHover(int index)
        {
            HideTubeHover();if(Ship==null||index<0||index>=Ship.TorpedoLaunchers.Length)return;
            var tube=Ship.TorpedoLaunchers[index];if(tube==null||tube.Torpedo==null)return;
            hoverTube=index;hoverItem=tube.Torpedo;RefreshTubeHover();
        }
        void RefreshTubeHover()
        {
            if(hoverTube<0)return;
            if(!Active||Loading||CurrentGroup==null||Ship==null||hoverTube>=Ship.TorpedoLaunchers.Length){HideTubeHover();return;}
            var tube=Ship.TorpedoLaunchers[hoverTube];
            if(tube==null||tube.Torpedo!=hoverItem||(Access==RoomAccess.CurrentCompartment&&tube.Group!=CurrentGroup)){HideTubeHover();return;}
            if(hoverPanel==null){
                hoverPanel=GridPanel(gridRoot.transform,"Torpedo information",Dark);hoverPanel.raycastTarget=false;
                hoverText=GridLabel(hoverPanel.transform,"Type and maintenance","",12,8,296,64,16);
            }
            hoverPanel.gameObject.SetActive(true);hoverPanel.transform.SetAsLastSibling();hoverText.text=TorpedoInfo.Read(hoverItem);
            float scale=roomCanvas==null?1:Mathf.Max(.01f,roomCanvas.scaleFactor);
            float width=Screen.width/scale,height=Screen.height/scale;
            var mouse=Input.mousePosition;
            Place(hoverPanel.rectTransform,Mathf.Clamp(mouse.x/scale+16,0,Mathf.Max(0,width-320)),Mathf.Clamp(height-mouse.y/scale-92,0,Mathf.Max(0,height-80)),320,80);
        }
    }
}
