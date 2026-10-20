// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using UnityEngine;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    // A full instrument face behind the native rotating handle. No raster enlargement.
    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class InstrumentFace : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=rectTransform.rect;float radius=Mathf.Min(rect.width,rect.height)*.46f;
            Disc(mesh,rect.center,radius,new Color(.27f,.27f,.23f,.98f));
            Disc(mesh,rect.center,radius-2,new Color(.055f,.065f,.06f,.98f));
        }
        static void Disc(VertexHelper mesh,Vector2 center,float radius,Color color)
        {
            int start=mesh.currentVertCount;mesh.AddVert(new Vector3(center.x,center.y,0),color,Vector2.zero);
            for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;mesh.AddVert(new Vector3(center.x+Mathf.Cos(a)*radius,center.y+Mathf.Sin(a)*radius,0),color,Vector2.zero);if(i>0)mesh.AddTriangle(start,start+i,start+i+1);}
        }
    }
}
