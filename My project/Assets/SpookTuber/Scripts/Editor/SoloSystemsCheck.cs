using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    [InitializeOnLoad] public static class SoloSystemsCheck
    {
        const string Flag="SpookTuber.SoloSystems";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static readonly List<string> checks=new();
        static IEnumerator<float> routine;
        static float resume;
        static double started;
        static Keyboard keyboard;
        static Mouse mouse;
        static CrewMotor motor;
        static CrewBody body;
        static CrewInventory inventory;
        static CrewPhone phone;
        static RunSession session;
        static string career;
        static bool restoreMic,hadMic,hadDevice;
        static int micPreference;
        static string devicePreference;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable] class Envelope {public int schema=2;public string payload,checksum;}
        static SoloSystemsCheck(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
        public static void Run(){SessionState.SetBool(Flag+".UI",false);SessionState.SetBool(Flag+".Hardware",false);SessionState.SetBool(Flag+".Visual",false);Begin();}
        public static void UI(){SessionState.SetBool(Flag+".UI",true);SessionState.SetBool(Flag+".Hardware",false);SessionState.SetBool(Flag+".Visual",false);Begin();}
        public static void Visual(){SessionState.SetBool(Flag+".Hardware",false);SessionState.SetBool(Flag+".Visual",true);Begin();}
        public static void Hardware(){SessionState.SetBool(Flag+".Hardware",true);SessionState.SetBool(Flag+".Visual",false);Begin();}
        static void Begin(){SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/ProductionHouse.unity");EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();}
        static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);File.WriteAllLines(Path.Combine(QA,"v5_systems_progress.txt"),checks);}
        static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static void Buttons(bool left=false,bool right=false)=>InputSystem.QueueStateEvent(mouse,new MouseState{buttons=(ushort)((left?1:0)|(right?2:0))});
        static void Bind(){motor=Object.FindFirstObjectByType<CrewMotor>();body=motor.GetComponent<CrewBody>();inventory=motor.GetComponent<CrewInventory>();phone=motor.GetComponent<CrewPhone>();}
        static void Place(Vector3 p,float yaw=0,float pitch=0)
        {
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=p;cc.enabled=true;
            typeof(CrewMotor).GetField("yaw",Private).SetValue(motor,yaw);typeof(CrewMotor).GetField("pitch",Private).SetValue(motor,pitch);typeof(CrewMotor).GetField("vertical",Private).SetValue(motor,0f);
            motor.transform.rotation=Quaternion.Euler(0,yaw,0);motor.SetCursor(true);motor.UpdateView();Physics.SyncTransforms();
        }
        static void Fixture(int day=1,bool finished=false,long views=0,long credits=5000)
        {
            career=Path.Combine(QA,"SoloSystems-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(career);
            string payload=$"{{\"version\":3,\"quotaDay\":{day},\"quotaCycle\":1,\"runCycle\":1,\"quotaTarget\":1000,\"quotaViews\":{views},\"dayFinished\":{finished.ToString().ToLowerInvariant()},\"teamMinor\":{credits},\"ownedGear\":[],\"takes\":[],\"publications\":[]}}";
            File.WriteAllText(Path.Combine(career,"HospitalRun.json"),JsonUtility.ToJson(new Envelope{payload=payload,checksum=BobbyEpisode.Hash(payload)}));session.StorageDirectory=career;Check(session.ReloadCareer(),"Load isolated career "+day+"/3");
        }
        static CarryItem Pickup(CarryItem.Kind kind)
        {
            var item=Object.FindObjectsByType<CarryItem>(FindObjectsSortMode.None).First(i=>i.kind==kind&&!i.Owner);item.transform.position=motor.viewCamera.transform.position+motor.viewCamera.transform.forward*.8f;Physics.SyncTransforms();Check(inventory.TryPickup(item),"Pickup "+kind+" / "+inventory.Notice);return item;
        }
        static IEnumerator<float> Exercise()
        {
            yield return 1.5f;Bind();session=RunSession.Current;Fixture();
            Check(Mathf.Abs(motor.transform.lossyScale.x-.88f)<.001f,"Approved complete player scaled to 88 percent");
            Check(Object.FindObjectsByType<HomeNpc>(FindObjectsSortMode.None).Length==2,"Bobby and supply NPC occupy the home");
            Check(session.TradeGear(CarryItem.Kind.ProductionLight,false)&&session.Credits==3800,"Credit buys work light");
            Check(!session.TradeGear(CarryItem.Kind.ProductionLight,false)&&session.Credits==3800,"Duplicate purchase rejected");
            Check(session.TradeGear(CarryItem.Kind.Noisemaker,false)&&session.Credits==3200,"Credit buys noisemaker");
            long quota=session.QuotaViews;Check(session.TradeGear(CarryItem.Kind.Noisemaker,true)&&session.Credits==3500&&session.QuotaViews==quota,"Sell refunds half Credit without spending Views");
            string blocked=Path.Combine(career,"blocked");File.WriteAllText(blocked,"file");session.StorageDirectory=blocked;
            Check(!session.TradeGear(CarryItem.Kind.Noisemaker,false)&&session.Credits==3500&&!session.OwnsGear(CarryItem.Kind.Noisemaker),"Failed save neither charges nor grants gear");session.StorageDirectory=career;
            Check(session.TradeGear(CarryItem.Kind.Noisemaker,false),"Retry purchase after save failure");
            Place(new Vector3(0,.08f,-4));Pickup(CarryItem.Kind.Camera);Pickup(CarryItem.Kind.GravityGlove);Pickup(CarryItem.Kind.ProductionLight);
            var extra=Object.FindObjectsByType<CarryItem>(FindObjectsSortMode.None).First(i=>i.kind==CarryItem.Kind.Noisemaker);extra.transform.position=motor.viewCamera.transform.position+Vector3.forward*.6f;
            Check(inventory.Count==3&&!inventory.TryPickup(extra),"Exactly three slots; fourth pickup refused");
            Keys(Key.Digit1);yield return .3f;Keys();Check(inventory.Active.kind==CarryItem.Kind.Camera,"Number key selects camera");
            Vector3 rest=motor.mainCam.transform.localPosition;Buttons(false,true);yield return .35f;
            Check(motor.Aiming&&Vector3.Distance(motor.mainCam.transform.localPosition,motor.mainCam.aimedOffset)<.015f&&Vector3.Distance(rest,motor.mainCam.transform.localPosition)>.1f,"RMB smoothly brings the LCD close");Capture("v5_Play_Camera_Aim.png");Buttons();yield return .3f;
            Check(Quaternion.Angle(motor.mainCam.transform.rotation,motor.viewCamera.transform.rotation)<.1f,"Held camera follows view orientation");
            Capture("v5_Play_Camera_Rest.png");
            motor.mainCam.Take.StorageDirectory=Path.Combine(career,"Takes");Check(motor.mainCam.Take.StartRecording(),"New scene records without exhausting track budget");yield return 2;
            Keys(Key.Digit2);yield return .3f;Keys();Check(!motor.mainCam.Take.Recording&&File.Exists(motor.mainCam.Take.LastSavedPath)&&inventory.Active.kind==CarryItem.Kind.GravityGlove,"Switching gear saves and stows the running camera");
            phone.Open();yield return .3f;Check(phone.IsOpen&&inventory.Suspended&&motor.InputBlocked,"Physical phone suspends held tools and world actions");
            Check(EventSystem.current&&EventSystem.current.currentSelectedGameObject,"Phone has a visible keyboard focus target");Capture("v5_Phone_Home.png");Capture("v5_Phone_16x10.png",1440,900);
            Vector3 before=motor.transform.position;Keys(Key.W,Key.R,Key.Q);yield return .45f;Keys();Check(Vector3.Distance(before,motor.transform.position)<.05f&&inventory.Count==3,"Phone focus blocks movement, recording and drop");
            foreach(string app in new[]{"Controls","Mission","Map","Equipment","Harmony","Supply","Settings"}){phone.Show(app);yield return .2f;Capture("v5_Phone_"+app+".png");Check(phone.CurrentApp==app,"Phone opens "+app);}
            Keys(Key.Escape);yield return .2f;Keys();Check(phone.IsOpen&&phone.CurrentApp=="Home","Escape returns app to launcher");yield return .1f;Keys(Key.Escape);yield return .2f;Keys();Check(!phone.IsOpen&&!inventory.Suspended,"Escape stows phone and restores tools");
            var voice=motor.GetComponent<CrewVoice>();Check(Mathf.Abs(CrewVoice.Rms(new[]{-.1f,.1f})-.1f)<.0001f&&CrewVoice.Rms(new[]{float.NaN})==0,"Microphone RMS measures real sample amplitude and rejects NaN");
            int noises=0;Action<Vector3,float,string> heard=(p,r,k)=>{if(k=="Voice")noises++;};WorldNoise.Emitted+=heard;
            typeof(CrewVoice).GetField("<Listening>k__BackingField",Private).SetValue(voice,true);voice.ProcessLevel(0);voice.ProcessLevel(.15f);typeof(CrewVoice).GetField("<Listening>k__BackingField",Private).SetValue(voice,false);WorldNoise.Emitted-=heard;
            Check(noises==1,"Voice threshold dispatches audible world noise; silence does not");File.WriteAllLines(Path.Combine(QA,"v5_microphone_devices.txt"),voice.Devices);
            Place(new Vector3(0,.08f,-7),0);yield return .3f;before=motor.transform.position;Keys(Key.W);yield return .8f;float walk=(motor.transform.position-before).magnitude;Keys();yield return .3f;
            Place(new Vector3(0,.08f,-7),0);yield return .2f;before=motor.transform.position;Keys(Key.W,Key.LeftShift);yield return .8f;float run=(motor.transform.position-before).magnitude;
            Check(run>walk*1.45f,"Sprint travels substantially faster than walking");Keys(Key.W,Key.LeftShift,Key.C);yield return .2f;Check(motor.Sliding&&motor.CrouchAmount>.5f,"Sprint plus C enters a physical crouch slide");Keys();yield return 1;
            Place(new Vector3(0,.08f,-7));yield return .2f;before=motor.transform.position;Keys(Key.Space);yield return .22f;Keys();Check(motor.transform.position.y>before.y+.35f,"Space jumps off the floor");
            phone.Open();yield return 1;Check(motor.transform.position.y<.15f,"Opening phone in air still obeys gravity");phone.Close();
            Keys(Key.LeftCtrl);yield return .35f;Check(motor.CrouchAmount>.95f&&motor.GetComponent<CharacterController>().height<1.1f,"Crouch lowers view and collision capsule");
            var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=motor.transform.position+Vector3.up*1.25f;ceiling.transform.localScale=new Vector3(2,.15f,2);Physics.SyncTransforms();Keys();yield return .3f;
            Check(motor.CrouchAmount>.95f&&!motor.CanStand(),"Low ceiling prevents uncrouching into solid geometry");Object.Destroy(ceiling);yield return .4f;
            Keys(Key.X);yield return .35f;Check(motor.Lean>.8f,"X leans around the right edge");Keys();yield return .3f;
            Place(new Vector3(0,.08f,-7));var ledge=GameObject.CreatePrimitive(PrimitiveType.Cube);ledge.name="QA_Ledge";ledge.transform.position=new Vector3(0,.4f,-6.15f);ledge.transform.localScale=new Vector3(2,.8f,1);ledge.AddComponent<MantleSurface>();Physics.SyncTransforms();
            Check(motor.TryMantle(),"Reachable low ledge starts collision checked mantle");yield return .7f;Check(motor.transform.position.y>.75f&&!motor.Mantling,"Mantle finishes standing on ledge");Object.Destroy(ledge);
            Place(new Vector3(0,.08f,-7),0,18);inventory.Select(1);yield return .2f;
            var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.name="QA_PhysicalCrate";prop.transform.position=motor.viewCamera.transform.position+motor.viewCamera.transform.forward*2;prop.transform.localScale=Vector3.one*.45f;var rb=prop.AddComponent<Rigidbody>();rb.mass=2;prop.AddComponent<PhysicsProp>();Physics.SyncTransforms();
            var glove=motor.GetComponent<GravityGlove>();Buttons(true);yield return .1f;Check(glove.Held==rb,"Gravity glove acquires an eligible Rigidbody");Vector3 old=rb.position;glove.AdjustDistance(-.7f);yield return .6f;Check(Vector3.Distance(old,rb.position)>.12f&&glove.energy<100,"Glove pulls through real physics and consumes energy");Buttons(true,true);yield return .1f;Check(!glove.Held&&rb.linearVelocity.magnitude>1,"RMB releases held prop with a physical impulse");Buttons();Object.Destroy(prop);
            inventory.Select(2);Keys(Key.Q);yield return .3f;Keys();Check(inventory.Count==2&&!inventory.Active,"Q returns selected item to physics world");
            Fixture(3,true,500,600);Check(session.QuotaFailed&&!session.Depart(),"Missed day-three View quota blocks the next expedition");Check(session.RetryContract()&&session.QuotaDay==1&&session.QuotaCycle==2&&session.Credits==600,"Retry starts a new contract and preserves Credit");
            Fixture(3,true,1100,600);Check(session.Depart(),"Completed View quota allows the next contract");yield return 3;Bind();Check(session.QuotaDay==1&&session.QuotaCycle==2&&session.QuotaViews==0&&session.QuotaTarget==1350&&session.Credits==600,"New three-day contract increases target without converting Credit");
            var surgeon=Object.FindFirstObjectByType<Surgeon>();surgeon.enabled=false;Check(surgeon.GetComponent<NavMeshAgent>().isOnNavMesh,"Expanded hospital has a walkable AI spawn");
            Check(Object.FindObjectsByType<MapZone>(FindObjectsSortMode.None).Any(z=>z.label=="MORGUE"),"Hospital includes distinct clinical wings");
            var door=GameObject.Find("Door_Spine-10").GetComponent<HospitalDoor>();Place(new Vector3(0,.08f,18),-90);door.SetOpen(true);yield return 1;
            Check(door.OpenFraction>.98f&&!door.obstacle.enabled,"Sliding door fully retracts and clears navigation");
            Check(!Physics.Linecast(new Vector3(-1.7f,1,18),new Vector3(-3.4f,1,18),~((1<<8)|(1<<9))),"Open sliding door has physical walking clearance");
            Place(new Vector3(-2.5f,.08f,18));door.SetOpen(false);Check(door.IsOpen,"Door refuses to crush a crew member standing in the opening");
            Place(new Vector3(0,.08f,18));door.SetOpen(false);yield return 1;Check(door.OpenFraction<.01f&&door.obstacle.enabled,"Closed sliding door restores blocking collider and navigation");
            Place(new Vector3(0,.08f,8));Capture("v5_Play_Hospital.png");
            var agent=surgeon.GetComponent<NavMeshAgent>();agent.Warp(new Vector3(-8.5f,.08f,30));surgeon.enabled=true;WorldNoise.Emit(new Vector3(-8.5f,1,33),10,"QA impact");yield return .2f;
            Check(Vector3.Distance(surgeon.LastKnownPosition,new Vector3(-8.5f,1,33))<.2f,"Actual world sound updates AI investigation target");surgeon.enabled=false;
            Place(session.RVPosition+Vector3.forward*2);Check(session.BeginExtraction(),"RV location follows the expanded exterior");yield return 4;
            Check(session.Phase==RunSession.RunPhase.House&&session.DayFinished&&session.QuotaDay==1,"Extraction settles day once and returns to home");Check(session.Depart(),"Next expedition starts day two");yield return 3;Check(session.QuotaDay==2,"Quota advances by one day per completed expedition");
        }
        static IEnumerator<float> Poses()
        {
            yield return 1;Bind();session=RunSession.Current;Fixture();Place(new Vector3(0,.08f,-4));Pickup(CarryItem.Kind.Camera);
            var measurements=new List<string>();
            void Measure(string name,Transform target){var hand=body.animator.GetBoneTransform(HumanBodyBones.RightHand);var shoulder=body.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var elbow=body.animator.GetBoneTransform(HumanBodyBones.RightLowerArm);measurements.Add($"{name} / target {target.position} / hand {hand.position} / error {Vector3.Distance(target.position,hand.position)} / shoulder {shoulder.position} / arm length {Vector3.Distance(shoulder.position,elbow.position)+Vector3.Distance(elbow.position,hand.position)}");}
            foreach(var pose in new[]{new Vector3(.02f,-.07f,.33f)}){motor.mainCam.aimedOffset=pose;Buttons(false,true);yield return .5f;Capture("pose_camera_"+pose.y+".png");Measure("camera "+pose,motor.mainCam.rightGrip);}
            var grip=body.GetComponentInChildren<CrewCameraGrip>();var fingers=(List<(Transform bone,Quaternion rest,Vector3 axis,float angle)>)typeof(CrewCameraGrip).GetField("fingers",Private).GetValue(grip);
            for(int i=0;i<fingers.Count;i++){var f=fingers[i];f.angle=-f.angle;fingers[i]=f;}
            yield return .5f;Capture("pose_camera_reverse_curl.png");
            Buttons();phone.Open();yield return .5f;Capture("pose_phone_base.png");Measure("phone base",phone.RightGrip);
            var device=(GameObject)typeof(CrewPhone).GetField("device",Private).GetValue(phone);device.transform.localScale=Vector3.one*.60f;device.transform.localPosition=new Vector3(.09f,-.035f,.30f);yield return .5f;Capture("pose_phone_compact.png");Measure("phone compact",phone.RightGrip);
            File.WriteAllLines(Path.Combine(QA,"v5_hand_pose_probe.txt"),measurements);
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{
                if(routine==null){started=EditorApplication.timeSinceStartup;Application.runInBackground=true;InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();routine=SessionState.GetBool(Flag+".UI",false)?Interface():SessionState.GetBool(Flag+".Hardware",false)?Sensors():SessionState.GetBool(Flag+".Visual",false)?Poses():Exercise();}
                if(EditorApplication.timeSinceStartup-started>300)throw new Exception("Timed out after "+checks.LastOrDefault());
                if(Time.time<resume)return;if(routine.MoveNext())resume=Time.time+routine.Current;else Finish(null);
            }catch(Exception e){Finish(e.ToString());}
        }
        static IEnumerator<float> Interface()
        {
            yield return 1;Bind();session=RunSession.Current;Fixture();Place(new Vector3(0,.08f,-4));Pickup(CarryItem.Kind.Camera);
            Buttons(false,true);yield return .5f;Capture("v5_Play_Camera_Aim.png");Buttons();phone.Open();yield return .5f;
            Capture("v5_Phone_Home.png");Capture("v5_Phone_16x10.png",1440,900);
            Keys(Key.Enter);yield return .2f;Keys();Check(phone.CurrentApp=="Mission","Native keyboard Enter activates focused phone app");yield return .2f;
            Keys(Key.Escape);yield return .2f;Keys();Check(phone.CurrentApp=="Home","Escape returns to apps");
            foreach(string app in new[]{"Controls","Mission","Map","Equipment","Harmony","Supply","Settings"}){phone.Show(app);yield return .2f;Capture("v5_Phone_"+app+".png");}
            var menu=phone.GetComponentInChildren<Dropdown>();EventSystem.current.SetSelectedGameObject(menu.gameObject);Keys(Key.Enter);yield return .3f;Keys();
            Check(menu.transform.Find("Dropdown List"),"Native keyboard opens microphone device list");Capture("v5_Phone_Device_List.png");
            Keys(Key.Escape);yield return .6f;Keys();Check(phone.IsOpen&&phone.CurrentApp=="Settings"&&!menu.transform.Find("Dropdown List"),$"First Escape dismisses device list and keeps Settings / open={phone.IsOpen} app={phone.CurrentApp} popup={(menu&&menu.transform.Find("Dropdown List"))}");yield return .2f;
            Keys(Key.Escape);yield return .2f;Keys();Check(phone.CurrentApp=="Home","Second Escape returns from Settings to apps");yield return .2f;
            Keys(Key.Escape);yield return .2f;Keys();Check(!phone.IsOpen,"Third Escape stows phone");
        }
        static IEnumerator<float> Sensors()
        {
            yield return 1;Bind();var voice=motor.GetComponent<CrewVoice>();hadMic=PlayerPrefs.HasKey("VoiceEnabled");hadDevice=PlayerPrefs.HasKey("VoiceDevice");micPreference=PlayerPrefs.GetInt("VoiceEnabled",1);devicePreference=PlayerPrefs.GetString("VoiceDevice","");restoreMic=true;
            Check(voice.Enable(devicePreference),"Windows microphone opens / "+voice.Status);int previous=Microphone.GetPosition(voice.Device);yield return 1.7f;
            Check(voice.Listening&&Microphone.IsRecording(voice.Device)&&Microphone.GetPosition(voice.Device)!=previous,"Physical microphone capture buffer advances");
            Check(voice.Status=="MIC LIVE"||voice.Status=="MIC / SOUND DETECTED","Actual audio samples reach the live microphone meter");
            File.WriteAllText(Path.Combine(QA,"v5_microphone_hardware.txt"),$"Device: {voice.Device}\nStatus: {voice.Status}\nSample cursor: {Microphone.GetPosition(voice.Device)}\nCurrent RMS: {voice.Level}\nAudio stays in a one-second RAM buffer; no microphone recording file written.\nHuman speech at the user's chosen gain still needs playtest calibration.\n");voice.enabled=false;
        }
        static void Capture(string name,int width=1600,int height=900)
        {
            var camera=motor.viewCamera;var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
            var hud=GameObject.Find("CrewHUD").GetComponent<Canvas>();hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=camera;hud.planeDistance=.15f;
            foreach(var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)){text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);text.SetAllDirty();}
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(QA,name),image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;hud.renderMode=RenderMode.ScreenSpaceOverlay;hud.worldCamera=null;Object.Destroy(image);Object.Destroy(rt);
        }
        static void Finish(string error){SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;if(restoreMic){if(hadMic)PlayerPrefs.SetInt("VoiceEnabled",micPreference);else PlayerPrefs.DeleteKey("VoiceEnabled");if(hadDevice)PlayerPrefs.SetString("VoiceDevice",devicePreference);else PlayerPrefs.DeleteKey("VoiceDevice");PlayerPrefs.Save();}File.WriteAllText(Path.Combine(QA,SessionState.GetBool(Flag+".UI",false)?"v5_ui_check.txt":SessionState.GetBool(Flag+".Hardware",false)?"v5_microphone_check.txt":SessionState.GetBool(Flag+".Visual",false)?"v5_visual_check.txt":"v5_systems_check.txt"),(error==null?"PASS":"FAIL / "+error)+"\n"+string.Join("\n",checks));EditorApplication.Exit(error==null?0:1);}
    }
}
