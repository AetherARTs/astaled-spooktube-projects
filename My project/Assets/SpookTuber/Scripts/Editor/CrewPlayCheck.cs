using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    [InitializeOnLoad]
    public static class CrewPlayCheck
    {
        const string Flag="SpookTuber.PlayCheck";
        static CrewBody body;
        static CrewMotor motor;
        static Keyboard keyboard;
        static Vector3 start;
        static double stamp;
        static int stage;
        static MainCam mainCam;
        static CrewTake take;
        static Vector3 recordedLens,recordedHips;
        static GameObject lensWall;
        static readonly List<string> checks=new();
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static CrewPlayCheck(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
        public static void Run()
        {
            SessionState.SetBool(Flag+".Reload",false);Start();
        }
        public static void RunReload()
        {
            SessionState.SetBool(Flag+".Reload",true);Start();
        }
        static void Start()
        {
            SessionState.SetBool(Flag,true);
            EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/ProductionHouse.unity");
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string name) {
            if(!value)throw new Exception(name);
            checks.Add(name);
        }
        static void PressKeys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{
                if(!body){
                    body=Object.FindFirstObjectByType<CrewMotor>().GetComponent<CrewBody>();motor=body.GetComponent<CrewMotor>();
                    InputSystem.settings=Object.Instantiate(InputSystem.settings);
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;
                    keyboard=InputSystem.AddDevice<Keyboard>();stamp=EditorApplication.timeSinceStartup;stage=0;
                    Application.logMessageReceived+=OnLog;
                }
                double dt=EditorApplication.timeSinceStartup-stamp;
                if(stage==0&&dt>.7){
                    if(SessionState.GetBool(Flag+".Reload",false)){
                        take=motor.mainCam.Take;
                        var path=File.ReadAllText(Path.Combine(QA,"reload_take_path.txt"));
                        Check(take.LoadTake(path),"Saved take loads after restarting Unity");
                        Check(take.FrameCount>10&&take.Duration>1,"Recorded timeline persists across sessions");
                        Check(take.BeginReview(),"Fresh session rebuilds replay from current assets");
                        take.Paused=true;take.Seek(take.Duration);take.ReviewCamera.Render();SaveTexture(take.ReviewTexture,"Unity_MainCam_Reload.png");
                        Check(take.ReviewTexture.IsCreated(),"Reloaded take renders");
                        take.EndReview();Finish(null);return;
                    }
                    Check(body.animator.avatar.isHuman&&body.animator.avatar.isValid,"Humanoid avatar valid in Play Mode");
                    var hand=body.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    Check(hand.position.y<body.headBone.position.y-.3f,"Idle arms rest below head");
                    Check(!Array.Exists(body.GetComponentsInChildren<Transform>(),t=>t.name=="CHR_Player_Face"),"Blank display has no face geometry");
                    body.SetOutfit(false);
                    Check(!body.outfit.enabled&&!body.gear.enabled&&body.chassis.enabled,"Bare chassis wardrobe");
                    body.SetOutfit(true);
                    Check(body.outfit.enabled&&body.gear.enabled&&!body.chassis.enabled,"Hoodie wardrobe hides chassis");
                    start=body.transform.position;PressKeys(Key.W);stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==1&&dt>1){
                    File.WriteAllText(Path.Combine(QA,"input_probe.txt"),$"start={start} now={body.transform.position} frames={Time.frameCount} time={Time.time} focus={Application.isFocused} key={keyboard.wKey.isPressed} motor={motor.enabled} controller={body.GetComponent<CharacterController>().enabled} action={InputSystem.ListEnabledActions().Count}");
                    Check(body.transform.position.z-start.z>.7f,"W input moves character forward");
                    start=body.transform.position;PressKeys(Key.W,Key.LeftShift);stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==2&&dt>.65){
                    Check(body.transform.position.z-start.z>1.3f,"Sprint input increases movement");
                    start=body.transform.position;PressKeys(Key.Space);stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==3&&dt>.18){
                    Check(body.transform.position.y>start.y+.12f,"Jump input lifts character");
                    PressKeys();stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==4&&dt>.8){
                    var cc=body.GetComponent<CharacterController>();cc.enabled=false;body.transform.position=new Vector3(0,.1f,0);cc.enabled=true;
                    Capture("Unity_PlayMode_Hoodie.png");
                    body.KnockDown();
                    Check(body.IsDowned&&!body.animator.enabled&&!body.GetComponent<CharacterController>().enabled,"Death disables animated controller");
                    Check(Array.TrueForAll(body.ragdoll,b=>!b.isKinematic),"Death enables all eleven ragdoll bodies");
                    Check(!body.head.enabled,"Original head renderer hidden");
                    Check(body.ViewTarget.name=="DetachedHead_"+body.name,"Separate head survives body death");
                    body.MoveHead(Vector3.right*1000,1);
                    Check(Vector3.Distance(body.ViewTarget.position,body.hips.position+Vector3.up*.8f)<=body.followRadius+.01f,"Detached head cannot scout beyond radius");
                    Check(body.ViewTarget.position.x<4.3f,"Detached head collides with apartment wall");
                    stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==5&&dt>3){
                    File.WriteAllText(Path.Combine(QA,"ragdoll_probe.txt"),"root="+body.transform.position+" hips="+body.hips.position+" time="+Time.time+"\n"+string.Join("\n",Array.ConvertAll(body.ragdoll,b=>b.name+" pos="+b.position+" vel="+b.linearVelocity+" scale="+b.transform.lossyScale)));
                    Capture("Unity_PlayMode_Ragdoll.png");
                    Check(body.hips.position.y>-.3f&&body.hips.position.y<1,"Ragdoll settles on apartment floor");
                    Check(body.ragdollColliders.Length==15,"Hands and shoes have compound colliders");
                    foreach(var c in body.ragdollColliders)if(c.name.EndsWith("ShoeCollision"))Check(c.bounds.min.y>-.04f,"Shoe collider remains above floor");
                    foreach(var rb in body.ragdoll)
                        Check(float.IsFinite(rb.position.x)&&float.IsFinite(rb.position.y)&&rb.position.y>-.4f&&Vector3.Distance(rb.position,body.hips.position)<2.5f,"Stable body "+rb.name);
                    Capture("Unity_PlayMode_Ragdoll.png");
                    body.Repair();stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==6&&dt>.5){
                    Check(!body.IsDowned&&body.animator.enabled&&body.GetComponent<CharacterController>().enabled,"Repair restores player control");
                    Check(Array.TrueForAll(body.ragdoll,b=>b.isKinematic),"Repair disables ragdoll simulation");
                    Check(body.head.enabled,"Repair restores original head");
                    mainCam=motor.mainCam;take=mainCam.Take;take.StorageDirectory=Path.Combine(QA,"RecordedTakes");
                    Check(!mainCam.TryPickup(motor),"MainCam rejects pickup beyond reach");
                    var cc=body.GetComponent<CharacterController>();cc.enabled=false;
                    body.transform.position=new Vector3(-2.8f,.1f,-1.43f);cc.enabled=true;
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA_Occluder";
                    wall.transform.position=Vector3.Lerp(body.headBone.position,mainCam.transform.position,.5f);wall.transform.localScale=Vector3.one*.25f;
                    Physics.SyncTransforms();Check(!mainCam.TryPickup(motor),"MainCam rejects pickup through an obstruction");
                    Object.DestroyImmediate(wall);Physics.SyncTransforms();
                    Check(mainCam.TryPickup(motor),"MainCam pickup within reach");
                    Check(mainCam.Holder==motor&&mainCam.GetComponent<Rigidbody>().isKinematic,"Held camera has one owner and no dynamic body");
                    Check(!mainCam.TryPickup(motor),"Held camera rejects duplicate pickup");
                    PressKeys(Key.R);stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==7&&dt>.3){
                    var left=body.animator.GetBoneTransform(HumanBodyBones.LeftHand);var right=body.animator.GetBoneTransform(HumanBodyBones.RightHand);
                    Check(mainCam.LivePreview&&mainCam.LivePreview.IsCreated(),"Held MainCam displays a live lens preview");
                    SaveTexture(mainCam.LivePreview,"Unity_MainCam_LensPreview.png");
                    File.WriteAllText(Path.Combine(QA,"hand_grip_probe.txt"),$"left={left.position} target={mainCam.leftGrip.position} distance={Vector3.Distance(left.position,mainCam.leftGrip.position)}\nright={right.position} target={mainCam.rightGrip.position} distance={Vector3.Distance(right.position,mainCam.rightGrip.position)}");
                    Capture("Unity_MainCam_Grip.png");
                    var rt=new RenderTexture(1200,900,24);motor.viewCamera.targetTexture=rt;motor.viewCamera.Render();SaveTexture(rt,"Unity_MainCam_FirstPerson.png");motor.viewCamera.targetTexture=null;Object.Destroy(rt);
                    Check(Vector3.Distance(left.position,mainCam.leftGrip.position)<.055f&&Vector3.Distance(right.position,mainCam.rightGrip.position)<.055f,"Both hands reach the physical camera grips");
                    Check(take.Recording&&take.FrameCount>1,"R input records actual frames");
                    body.SetOutfit(false);PressKeys();stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==8&&dt>.4){
                    PressKeys(Key.Q);stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==9&&dt>.6){
                    Check(!mainCam.Holder&&!mainCam.GetComponent<Rigidbody>().isKinematic,"Q input drops physical camera");
                    Check(take.Recording,"Dropped camera continues the same take");
                    PressKeys();body.KnockDown();stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==10&&dt>1.2){
                    Check(!mainCam.TryPickup(motor),"Detached head cannot pick up MainCam");
                    recordedLens=mainCam.lens.position;recordedHips=body.hips.position;
                    Check(take.StopRecording(),"Take commits to disk");
                    Check(File.Exists(take.LastSavedPath)&&new FileInfo(take.LastSavedPath).Length>1000,"Saved take contains recorded data");
                    File.WriteAllText(Path.Combine(QA,"reload_take_path.txt"),take.LastSavedPath);
                    Check(take.LoadTake(take.LastSavedPath),"Saved take reloads with stable camera and scene bindings");
                    body.Repair();body.SetOutfit(true);
                    PressKeys(Key.P);stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==11&&dt>.4){
                    Check(take.Reviewing,"P input opens recorded take");
                    var livePosition=body.hips.position;var liveCamera=mainCam.transform.position;
                    take.Paused=true;take.Seek(take.Duration);
                    Check(Vector3.Distance(take.ReviewCamera.transform.position,recordedLens)<.001f,"Replay uses recorded lens pose");
                    Check(Array.Exists(take.ReplayRoot.GetComponentsInChildren<Transform>(),t=>t.name=="Hips"&&Vector3.Distance(t.position,recordedHips)<.001f),"Replay preserves recorded ragdoll pose");
                    File.WriteAllText(Path.Combine(QA,"replay_components.txt"),string.Join("\n",Array.ConvertAll(take.ReplayRoot.GetComponentsInChildren<Component>(true),c=>c.GetType().FullName)));
                    take.ReviewCamera.Render();SaveTexture(take.ReviewTexture,"Unity_MainCam_Replay.png");
                    Check(take.ReplayRoot.GetComponentsInChildren<Rigidbody>(true).Length==0&&take.ReplayRoot.GetComponentsInChildren<Collider>(true).Length==0
                        &&Array.TrueForAll(take.ReplayRoot.GetComponentsInChildren<MonoBehaviour>(true),c=>c is UnityEngine.Rendering.Universal.UniversalAdditionalLightData||c is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData),"Replay contains rendering components only, no physics or gameplay scripts");
                    take.Seek(0);take.Seek(take.Duration);
                    Check(Vector3.Distance(body.hips.position,livePosition)<.0001f&&Vector3.Distance(mainCam.transform.position,liveCamera)<.0001f,"Scrubbing leaves live actors unchanged");
                    take.ReviewCamera.Render();SaveTexture(take.ReviewTexture,"Unity_MainCam_Replay.png");
                    PressKeys();stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==12&&dt>.2){PressKeys(Key.P);stage++;stamp=EditorApplication.timeSinceStartup;}
                else if(stage==13&&dt>.2){
                    Check(!take.Reviewing,"P input closes take review");PressKeys();
                    var corrupt=Path.Combine(QA,"RecordedTakes","truncated.sttake");File.WriteAllBytes(corrupt,new byte[]{1,2,3});
                    Check(!take.LoadTake(corrupt),"Truncated take is rejected without losing the current take");
                    Check(take.FrameCount>10,"Valid take retained after invalid import");
                    File.Delete(corrupt);
                    Check(mainCam.TryPickup(motor)&&take.StartRecording(),"Camera can record another take after review");
                    string blocked=Path.Combine(QA,"blocked_take_folder");File.WriteAllText(blocked,"directory collision for write-failure test");
                    take.StorageDirectory=blocked;
                    Check(!take.StopRecording()&&take.Unsaved&&take.FrameCount>0,"Write failure preserves unsaved recorded frames");
                    Check(!take.StartRecording(),"A new recording cannot overwrite an unsaved take");
                    take.StorageDirectory=Path.Combine(QA,"RecordedTakes");File.Delete(blocked);
                    Check(take.SaveTake()&&!take.Unsaved,"Saving can be retried after a storage error");
                    lensWall=GameObject.CreatePrimitive(PrimitiveType.Cube);lensWall.name="QA_LensWall";
                    lensWall.transform.position=body.headBone.position+Vector3.forward*.30f;
                    lensWall.transform.localScale=new Vector3(2,2,.15f);Physics.SyncTransforms();
                    stage++;stamp=EditorApplication.timeSinceStartup;
                }
                else if(stage==14&&dt>.25){
                    File.WriteAllText(Path.Combine(QA,"lens_clearance.txt"),"head="+body.headBone.position+" view="+motor.viewCamera.transform.position+" lens="+mainCam.lens.position);
                    Check(!Physics.Linecast(body.headBone.position,mainCam.lens.position,~(1<<8),QueryTriggerInteraction.Ignore),"Held lens cannot record through a wall");
                    Check(!Physics.Linecast(body.headBone.position,motor.viewCamera.transform.position,~(1<<8),QueryTriggerInteraction.Ignore),"First-person view stays in front of a wall");
                    Object.DestroyImmediate(lensWall);Finish(null);
                }
            }catch(Exception e){Finish(e.ToString());}
        }
        static void Capture(string name)
        {
            var shadow=body.head.shadowCastingMode;body.head.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            var camera=new GameObject("QA_Camera").AddComponent<Camera>();
            camera.transform.position=body.hips.position+new Vector3(2,1,2.4f);
            camera.transform.LookAt(body.hips.position+Vector3.up*.3f);
            var rt=new RenderTexture(1200,900,24);camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;var image=new Texture2D(1200,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(QA,name),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;
            Object.Destroy(rt);Object.Destroy(image);Object.Destroy(camera.gameObject);
            body.head.shadowCastingMode=shadow;
        }
        static void SaveTexture(RenderTexture rt,string name)
        {
            var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(QA,name),image.EncodeToPNG());RenderTexture.active=old;Object.Destroy(image);
        }
        static void OnLog(string message,string trace,LogType type){
            if(type==LogType.Exception||type==LogType.Error)File.AppendAllText(Path.Combine(QA,"playmode_errors.log"),message+"\n"+trace+"\n");
        }
        static void Finish(string failure)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
            SessionState.SetBool(Flag,false);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory(QA);
            File.WriteAllText(Path.Combine(QA,SessionState.GetBool(Flag+".Reload",false)?"take_reload_validation.txt":"playmode_validation.txt"),(failure==null?"PASS":"FAIL")+"\n"+string.Join("\n",checks)+"\n"+failure);
            Debug.Log(failure==null?"SPOOKTUBER_PLAYMODE_PASS":failure);
            EditorApplication.Exit(failure==null?0:1);
        }
        public static void BuildPlayer()
        {
            Directory.CreateDirectory("../Build");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled),s=>s.path),
                locationPathName="../Build/SpookTuber.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development
            });
            File.WriteAllText(Path.Combine(QA,"standalone_build.txt"),report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nBytes: "+report.summary.totalSize);
            EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
        }
    }
}







