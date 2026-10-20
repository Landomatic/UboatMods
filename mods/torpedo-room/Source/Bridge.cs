// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
// Independently authored reflection adapter for Uboats: Coop Multiplayer 1.11.5.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace Landomatic.TorpedoRoom
{
    internal static class Bridge
    {
        const string Prefix="PinguUboatMultiplayer.";
        // Unassigned in the supplied 1.11.5 protocol. Never enabled for other builds.
        const byte Message=240;
        static Type session, writerType, messageType;
        static Harmony harmony;
        static bool installed;
        static float nextInstall,nextHello,nextState,lastState;
        static int lastRole;
        static readonly HashSet<object> ready=new HashSet<object>();
        static readonly Dictionary<object,float> lastRequest=new Dictionary<object,float>();
        public static Snapshot Last;
        public static string Status="Single-player";
        static readonly Dictionary<string,Type> types=new Dictionary<string,Type>();
        static readonly Dictionary<Type,Dictionary<string,FieldInfo>> fields=new Dictionary<Type,Dictionary<string,FieldInfo>>();
        static readonly Dictionary<Type,Dictionary<string,PropertyInfo>> properties=new Dictionary<Type,Dictionary<string,PropertyInfo>>();
        static readonly Dictionary<Type,Dictionary<string,MethodInfo>> methods=new Dictionary<Type,Dictionary<string,MethodInfo>>();
        public static object Field(object obj,string name)
        {
            if(obj==null)return null;Type type=obj is Type?(Type)obj:obj.GetType();
            Dictionary<string,FieldInfo> members;if(!fields.TryGetValue(type,out members))fields[type]=members=new Dictionary<string,FieldInfo>();
            FieldInfo f;if(!members.TryGetValue(name,out f)){f=AccessTools.Field(type,name);members[name]=f;}
            return f==null?null:f.GetValue(obj is Type?null:obj);
        }
        public static object Prop(Type type,string name)
        {
            if(type==null)return null;Dictionary<string,PropertyInfo> members;
            if(!properties.TryGetValue(type,out members))properties[type]=members=new Dictionary<string,PropertyInfo>();
            PropertyInfo p;if(!members.TryGetValue(name,out p)){p=AccessTools.Property(type,name);members[name]=p;}
            return p==null?Field(type,name):p.GetValue(null,null);
        }
        static Type T(string name)
        {Type t;if(types.TryGetValue(name,out t))return t;t=AccessTools.TypeByName(Prefix+name);if(t!=null)types[name]=t;return t;}
        static MethodInfo Method(Type t,string name)
        {
            if(t==null)return null;Dictionary<string,MethodInfo> members;
            if(!methods.TryGetValue(t,out members))methods[t]=members=new Dictionary<string,MethodInfo>();
            MethodInfo m;if(!members.TryGetValue(name,out m)){m=AccessTools.Method(t,name);members[name]=m;}return m;
        }
        static object Call(Type t,string method,params object[] args)
        { var m=Method(t,method);return m==null?null:m.Invoke(null,args); }
        public static int Role {get{return session==null?0:Convert.ToInt32(Prop(session,"Role"));}}
        public static bool IsRemote {get{return Role>=2 && Convert.ToBoolean(Prop(T("MpMode"),"SharedBoat")??false);}}
        public static bool Synced {get{return installed&&Last!=null&&Time.unscaledTime-lastState<8;}}
        public static void Install(Harmony h){harmony=h;TryInstall();}
        static void TryInstall()
        {
            if(installed)return;
            session=T("MpSession");if(session==null)return;
            string build=Convert.ToString(Field(T("MpBuild"),"Version"));
            if(build!="1.11.5"){Status="Multiplayer "+build+" is not supported by this test adapter";return;}
            writerType=T("MpWriter");messageType=T("MpMsg");
            if(writerType==null||messageType==null||Enum.IsDefined(messageType,Message))return;
            MethodInfo host=AccessTools.Method(session,"OnHostPacket"),client=AccessTools.Method(session,"OnJoinerPacket");
            if(host==null||client==null)return;
            harmony.Patch(host,new HarmonyMethod(typeof(Bridge),"HostPacket"));
            harmony.Patch(client,new HarmonyMethod(typeof(Bridge),"ClientPacket"));
            installed=true;Status="Multiplayer adapter 1.11.5 ready";
            Debug.Log("[TorpedoRoom] Version-gated multiplayer adapter installed; no upstream files changed.");
        }
        public static void ResetWorld(){Last=null;ready.Clear();lastRequest.Clear();nextHello=nextState=0;}
        public static void Tick()
        {
            if(!installed&&Time.unscaledTime>=nextInstall){nextInstall=Time.unscaledTime+2;TryInstall();}
            if(!installed)return;
            if(Role!=lastRole){lastRole=Role;ResetWorld();}
            if(Role==0){if(Last!=null||ready.Count>0)ResetWorld();return;}
            if(IsRemote){
                if(!Synced)Last=null;
                if(!Synced&&Time.unscaledTime>=nextHello&&!Runtime.Loading){
                    nextHello=Time.unscaledTime+3;
                    Send(null,Wire.Pack(Wire.Hello,w=>{}));
                }
            }else if(Role==1&&Time.unscaledTime>=nextState&&!Runtime.Loading){
                nextState=Time.unscaledTime+1;
                var gone=new List<object>();
                foreach(var peer in ready){if(PeerValid(peer))SendState(peer,"");else gone.Add(peer);}
                foreach(var peer in gone){ready.Remove(peer);lastRequest.Remove(peer);}
            }
        }
        static bool PeerValid(object peer)
        {
            if(peer==null||Convert.ToInt32(Field(peer,"State")??0)!=3||Convert.ToString(Field(peer,"Mode"))!="SharedBoat")return false;
            var peers=Prop(session,"Peers") as IEnumerable;
            if(peers!=null)foreach(var p in peers)if(ReferenceEquals(p,peer))return true;
            return false;
        }
        public static bool Allowed(object peer,TorpedoLauncherGroup group)
        {
            if(!PeerValid(peer)||group==null)return false;
            string claim=Convert.ToString(Call(T("MpOfficers"),"ClaimOf",peer));if(string.IsNullOrEmpty(claim))return false;
            // The multiplayer station table is the existing authority for occupied devices.
            var users=Field(T("MpStations"),"Users") as IEnumerable;if(users==null)return false;
            foreach(var user in users){
                if(!Equals(Field(user,"Peer"),Field(peer,"PeerId")))continue;
                string station=Convert.ToString(Field(user,"Station"));
                double seen=Convert.ToDouble(Field(user,"Seen")??0d);
                if(Time.realtimeSinceStartupAsDouble-seen>6)return false;
                if(station=="TUBES:"+group.TypeName)return true;
                var ship=Runtime.Ship;if(Config.Access!=RoomAccess.AllEditable||ship==null)return false;
                foreach(var tube in ship.TorpedoLaunchers)if(tube!=null&&tube.Group!=null&&station=="TUBES:"+tube.Group.TypeName)return true;
                return false;
            }
            return false;
        }
        public static bool AllowedViewport(object peer)
        {
            if(Config.Viewport!=ViewportMode.Synchronized||!PeerValid(peer)||string.IsNullOrEmpty(Convert.ToString(Call(T("MpOfficers"),"ClaimOf",peer))))return false;
            var users=Field(T("MpStations"),"Users") as IEnumerable;if(users==null)return false;
            foreach(var user in users){
                if(!Equals(Field(user,"Peer"),Field(peer,"PeerId")))continue;
                string station=Convert.ToString(Field(user,"Station"));double seen=Convert.ToDouble(Field(user,"Seen")??0d);
                if(station.StartsWith("SCOPE:",StringComparison.Ordinal)&&Convert.ToBoolean(Prop(T("MpStations"),"TubesHiddenAtScopes")??false))return false;
                return Time.realtimeSinceStartupAsDouble-seen<=6&&(station.StartsWith("SCOPE:",StringComparison.Ordinal)||station=="TDC");
            }
            return false;
        }
        public static bool RequestViewport(TubeSnapshot[] edits)
        {
            if(!Synced||edits==null||edits.Length==0||edits.Length>16){Runtime.Say("Waiting for host confirmation before viewport edits.");return false;}
            bool sent=Send(null,Wire.Pack(Wire.Viewport,w=>{w.Write(Last.Epoch);w.Write((byte)edits.Length);foreach(var row in edits){w.Write((byte)row.Index);Wire.Write(w,row.Settings);}}));
            Runtime.Say(sent?"Viewport program changes sent; waiting for host confirmation.":"Could not send viewport program changes.");return sent;
        }
        static bool Send(object peer,byte[] bytes)
        {
            if(!installed||bytes.Length>8192)return false;
            var writer=Activator.CreateInstance(writerType,true);
            Method(writerType,"Begin").Invoke(writer,new[]{Enum.ToObject(messageType,Message)});
            Method(writerType,"WriteRaw").Invoke(writer,new object[]{bytes,0,bytes.Length});
            object answer=peer==null?Call(session,"SendToHost",writer,true):Call(session,"SendWriter",Field(peer,"Endpoint"),writer,true);
            return answer is bool&&(bool)answer;
        }
        public static void SendState(object peer,string message)
        { if(!PeerValid(peer)||Runtime.Loading)return;Send(peer,Wire.Encode(Runtime.Snapshot(message))); }
        public static bool Request(int index,Preparation edit)
        {
            if(!Synced){Runtime.Say("Waiting for the host's compatible Torpedo Room Controls companion.");return false;}
            bool sent=Send(null,Wire.Pack(Wire.Edit,w=>{w.Write(Last.Epoch);w.Write((byte)index);Wire.Write(w,edit);}));
            Runtime.Say(sent?"Settings sent; waiting for host confirmation.":"Could not send settings to host.");return sent;
        }
        public static void ApplySnapshot()
        {
            if(!Synced||Runtime.Loading)return;
            var ship=Runtime.Ship;if(ship==null||ship.TorpedoLaunchers==null)return;
            foreach(var row in Last.Tubes){
                if(row.Index>=ship.TorpedoLaunchers.Length)continue;
                var tube=ship.TorpedoLaunchers[row.Index];
                if(tube==null||tube.Torpedo==null||tube.Torpedo.TypeName!=row.Type)continue;
                var status=Runtime.Status(tube.Torpedo,true);if(status==null)continue;
                if(!ProgramValues.SamePreparation(status.Value,row.Settings))status.Value=row.Settings.Copy();
                var missile=Runtime.Missile(tube.Torpedo);
                if(Last.Enabled&&missile!=null&&(int)missile.PistolSetting!=row.Settings.Pistol)Runtime.SetPistol(missile,row.Settings.Pistol);
            }
        }
        // object[] allows patching internal upstream types without a hard assembly dependency.
        public static bool HostPacket(object[] __args)
        {
            if(Convert.ToByte(__args[1])!=Message)return true;
            try {
                object peer=Call(session,"FindByEndpoint",__args[0]);if(!PeerValid(peer))return false;
                byte[] bytes=(byte[])AccessTools.Method(__args[2].GetType(),"CopyRemaining").Invoke(__args[2],null);
                byte op;
                using(var r=Wire.Open(bytes,out op)){
                    if(op==Wire.Hello){Wire.Finish(r);ready.Add(peer);SendState(peer,"Host settings received.");}
                    else if(op==Wire.Viewport&&ready.Contains(peer)){
                        float previous;if(lastRequest.TryGetValue(peer,out previous)&&Time.unscaledTime-previous<0.1f)return false;lastRequest[peer]=Time.unscaledTime;
                        string epoch=r.ReadString();int count=r.ReadByte();if(count<1||count>16)throw new FormatException("Bad viewport tube count.");
                        var edits=new TubeSnapshot[count];for(int i=0;i<count;i++)edits[i]=new TubeSnapshot{Index=r.ReadByte(),Settings=Wire.Read(r)};Wire.Finish(r);
                        if(epoch!=Runtime.Epoch||!AllowedViewport(peer)||!Runtime.AcceptViewport(edits,peer))SendState(peer,"Viewport edit refused: refresh marked tubes, station and settings.");
                        else foreach(var other in ready)if(!ReferenceEquals(other,peer))SendState(other,"");
                    }
                    else if(op==Wire.Edit && ready.Contains(peer)){
                        float previous; if(lastRequest.TryGetValue(peer,out previous)&&Time.unscaledTime-previous<0.1f)return false;
                        lastRequest[peer]=Time.unscaledTime;
                        string epoch=r.ReadString();int index=r.ReadByte();var edit=Wire.Read(r);Wire.Finish(r);
                        if(epoch!=Runtime.Epoch){SendState(peer,"The loaded boat changed. Retry after refresh.");return false;}
                        Runtime.Accept(index,edit,peer);
                        foreach(var other in ready)if(!ReferenceEquals(other,peer))SendState(other,"");
                    }
                }
            }catch(Exception e){Debug.LogWarning("[TorpedoRoom] Rejected companion packet: "+e.Message);}
            return false;
        }
        public static bool ClientPacket(object[] __args)
        {
            if(Convert.ToByte(__args[1])!=Message)return true;
            try {
                if(!IsRemote||!Equals(__args[0],Field(session,"_host")))return false;
                byte[] bytes=(byte[])AccessTools.Method(__args[2].GetType(),"CopyRemaining").Invoke(__args[2],null);
                byte op;using(var r=Wire.Open(bytes,out op)){
                    if(op!=Wire.State)return false;
                    Last=Wire.Decode(r);lastState=Time.unscaledTime;
                    if(!string.IsNullOrEmpty(Last.Notice))Runtime.Say(Last.Notice);
                    ApplySnapshot();
                }
            }catch(Exception e){Last=null;Debug.LogWarning("[TorpedoRoom] Invalid host companion state: "+e.Message);}
            return false;
        }
    }
}
