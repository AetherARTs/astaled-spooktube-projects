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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    [InitializeOnLoad] public static class BobbyPlayCheck
    {
        const string Flag="SpookTuber.BobbyCheck";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static readonly List<string> checks=new();
        static int stage;
        static float stamp;
        static double started;
        static RunSession session;
        static CrewMotor motor;
        static CrewBody body;
        static CrewTake take;
        static Surgeon surgeon;
        static string career;
        static bool legacyHouse;
        static string legacyMessage;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable] sealed class Envelope {public int schema=2;public string payload,checksum;}
        [Serializable] sealed class AutoCareer {public int version=3,quotaDay=1,quotaCycle=1,runCycle=1;public long quotaTarget=1000;public List<int> ownedGear=new();public List<RecoveredTake> takes=new();public List<EpisodeReceipt> publications=new();}
        static BobbyPlayCheck(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
        public static void Run(){Begin(false);}
        public static void RunReload(){Begin(true);}
        public static void RunAutomatic(){Begin(false);SessionState.SetBool(Flag+".Automatic",true);}
        public static void RunLegacy(){Begin(false);SessionState.SetBool(Flag+".Legacy",true);EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/Legacy/ProductionHouse_v4.unity");}
        static void Begin(bool reload)
        {
            SessionState.SetBool(Flag+".Reload",reload);SessionState.SetBool(Flag,true);
            SessionState.SetBool(Flag+".Legacy",false);
            SessionState.SetBool(Flag+".Automatic",false);
            EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/ProductionHouse.unity");EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        static void Check(bool pass,string name){if(!pass)throw new Exception(name);checks.Add(name);Debug.Log("BOBBY_CHECK "+name);}
        static void Next(){stage++;stamp=Time.time;File.WriteAllText(Path.Combine(QA,"bobby_test_progress.txt"),"Stage "+stage+"\n"+string.Join("\n",checks));}
        static void Bind(){motor=Object.FindFirstObjectByType<CrewMotor>();body=motor.GetComponent<CrewBody>();take=motor.mainCam.Take;}
        static void Place(float z)
        {
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=new Vector3(0,.08f,z);cc.enabled=true;Physics.SyncTransforms();
        }
        static void Aim(float yaw)
        {
            typeof(CrewMotor).GetField("yaw",Private).SetValue(motor,yaw);typeof(CrewMotor).GetField("pitch",Private).SetValue(motor,-4f);
            motor.transform.rotation=Quaternion.Euler(0,yaw,0);motor.UpdateView();motor.mainCam.UpdateHeldPose();
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{
                if(started==0){started=EditorApplication.timeSinceStartup;stamp=Time.time;Application.runInBackground=true;Application.logMessageReceived+=Log;}
                if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Bobby check timed out at stage "+stage);
                float dt=Time.time-stamp;
                if(SessionState.GetBool(Flag+".Automatic",false)){AutomaticTick(dt);return;}
                if(SessionState.GetBool(Flag+".Legacy",false)){
                    if(stage==0&&dt>1){
                        Bind();legacyHouse=take.LoadTake(Path.Combine(QA,"Legacy_House.sttake"));legacyMessage=take.Message;
                        File.WriteAllText(Path.Combine(QA,"legacy_binding_probe.txt"),"House loaded="+legacyHouse+" / "+legacyMessage);
                        SceneManager.LoadScene("Hospital_v4");Next();
                    }else if(stage==1&&dt>1){
                        Bind();Check(take.LoadTake(Path.Combine(QA,"Legacy_Hospital.sttake")),"Legacy hospital footage loads with its audio and poses");
                        Check(take.Moments.Count==0&&take.TakeId=="","Legacy footage does not acquire fabricated evidence");
                        var original=take.LastSavedPath;Check(take.SaveTake()&&take.LastSavedPath==original,"Saving an unchanged legacy take does not rewrite or duplicate it");
                        Check(legacyHouse,"Legacy house footage loads / "+legacyMessage);Finish(null);
                    }
                    return;
                }
                if(SessionState.GetBool(Flag+".Reload",false)){ReloadTick(dt);return;}
                if(stage==0&&dt>1){
                    session=RunSession.Current;career=Path.Combine(QA,"BobbyRunTest-"+Guid.NewGuid().ToString("N"));session.StorageDirectory=career;
                    Check(session.ReloadCareer(),"Isolated career starts without touching player saves");Check(session.OpenStudio(),"Empty workstation is accessible before first expedition");
                    Check(session.Studio.IsOpen&&!session.Depart(),"Studio focus blocks gameplay departure");CaptureUI("Bobby_Empty.png");session.Studio.Close();
                    Check(session.Depart(),"Expedition starts for the production loop");Next();
                }else if(stage==1&&dt>1&&session.Phase==RunSession.RunPhase.Hospital){
                    Bind();motor.enabled=false;surgeon=Object.FindFirstObjectByType<Surgeon>();surgeon.enabled=false;
                    surgeon.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.08f,13));surgeon.transform.rotation=Quaternion.Euler(0,180,0);Place(8);Aim(0);take.StorageDirectory=Path.Combine(career,"Takes");Next();
                }else if(stage==2&&dt>.5f){
                    Aim(0);var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var visible=TakeEvidence.Measure(motor.mainCam.lens,surgeon,lights);
                    File.WriteAllText(Path.Combine(QA,"evidence_probe.txt"),$"lens={motor.mainCam.lens.position} enemy={surgeon.eye.position} visible={visible.visible} readable={visible.readable} framing={visible.framing}");
                    Check(visible.visible>.66f&&visible.readable>.12f&&visible.framing>.12f,"Lit visible Surgeon is readable in the recording lens");CaptureLens("Evidence_Visible.png");
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.5f,10.5f);wall.transform.localScale=new Vector3(4,3,.25f);Physics.SyncTransforms();
                    Check(TakeEvidence.Measure(motor.mainCam.lens,surgeon,lights).visible==0,"Wall between lens and Surgeon gives zero visual evidence");CaptureLens("Evidence_Occluded.png");Object.DestroyImmediate(wall);
                    Aim(180);Check(TakeEvidence.Measure(motor.mainCam.lens,surgeon,lights).visible==0,"Subject behind the lens gives zero visual evidence");Aim(0);
                    var ambient=RenderSettings.ambientLight;var enabled=lights.Select(l=>l.enabled).ToArray();foreach(var light in lights)light.enabled=false;RenderSettings.ambientLight=Color.black;
                    Check(TakeEvidence.Measure(motor.mainCam.lens,surgeon,lights).readable==0,"Unlit subject is not credited as readable footage");
                    for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];RenderSettings.ambientLight=ambient;
                    Check(take.StartRecording(),"Real lens/world recording starts with evidence capture");Next();
                }else if(stage==3&&dt>2){
                    Check(take.Moments.Any(m=>m.kind=="Sighting"),"Continuous visibility seeds an actual sighting");surgeon.enabled=true;surgeon.Hear(body.transform.position,20);Next();
                }else if(stage==4&&dt>2){
                    Check(take.Moments.Any(m=>m.kind=="Warning"),"Warning evidence comes from the AI's confirmed state");Place(5);Aim(180);Next();
                }else if(stage==5&&dt>1.6f){Place(1);Aim(0);Next();
                }else if(stage==6&&dt>2.2f){
                    Check(!body.IsDowned,"Crew survives the filmed encounter");Check(take.StopRecording(),"Footage and confirmed moments save atomically");
                    Check(take.Moments.Count>=2,"Recording contains multiple confirmed moments");Check(session.Takes.Count==1&&!session.Takes[0].recovered,"Unrecovered footage cannot earn upload rewards");
                    surgeon.enabled=false;Place(session.RVPosition.z+1);Check(session.BeginExtraction(),"Crew returns with the recorded tape");Next();
                }else if(stage==7&&dt>1&&session.Phase==RunSession.RunPhase.House){
                    Check(session.CompletedRuns==1&&session.Takes[0].recovered,"Extraction recovers the tape in the career catalog");Check(session.TeamMoney==0&&session.PersonalMoney==0,"Mission completion alone awards no episode money");
                    Check(session.OpenStudio(),"Bobby loads recovered footage across the scene boundary");Next();
                }else if(stage==8&&dt>.5f&&session.Studio.IsOpen){
                    take=session.Studio.Source;var desk=session.Studio;
                    Check(desk.AwaitingChoice,"Recovered tape offers Bobby or manual editing");CaptureUI("v5_Bobby_Choice.png");
                    Check(desk.ChooseEditing(false)&&!desk.AwaitingChoice&&desk.Episode.cuts.Count==1&&desk.Episode.cuts[0].reason=="Manual source selection","Manual choice creates an editable source shot");
                    Check(desk.AutoEdit("Documentary"),"Manual editor can ask Bobby for a draft");var episode=desk.Episode;
                    Check(take.Reviewing&&take.ExternalPlayback,"Episode preview uses the isolated recorded world");
                    Check(take.Moments.Count>=2&&episode.Validate(take,out _),"Persisted evidence generates a valid EDL");
                    var again=BobbyEpisode.Build(take,"Documentary",session.BobbyLevel);Check(again.id==episode.id,"Same recorded inputs produce the same Bobby cut");
                    Check(episode.Duration<=episode.Budget&&episode.cuts.All(c=>c.end<=take.Duration),"Context padding respects footage and episode budgets");
                    var result=episode.Evaluate(take,0);var repeat=episode.Evaluate(take,0);
                    Check(result.views==repeat.views&&result.revenueMinor==repeat.revenueMinor,"Audience seed is stable across repeated evaluations");
                    Check(result.evidenceMomentIds.All(id=>episode.cuts.Any(c=>c.EvidenceIds.Contains(id))),"Audience comments reference only included evidence");
                    Check(result.views>0&&result.comments.Length>0,"Real filmed evidence yields a bounded audience response");
                    Check(session.Publications.Count==0&&session.TeamMoney==0,"Preview and auto editing do not settle rewards");
                    string before=JsonUtility.ToJson(episode);Check(desk.EditCut(0,.5f,0),"Manual trimming edits the EDL");Check(desk.Undo()&&JsonUtility.ToJson(desk.Episode)==before,"Undo restores the exact previous edit");Check(desk.Undo(true),"Redo reapplies the edit");desk.Undo();
                    var invalid=JsonUtility.FromJson<BobbyEpisode>(before);invalid.cuts[0].start=float.NaN;Check(!invalid.Validate(take,out _),"Non-finite edit rejected");
                    invalid=JsonUtility.FromJson<BobbyEpisode>(before);invalid.cuts.Add(invalid.cuts[0]);Check(!invalid.Validate(take,out _),"Overlapping or repeated moments rejected");
                    invalid=JsonUtility.FromJson<BobbyEpisode>(before);invalid.cuts[0].end=take.Duration+1;Check(!invalid.Validate(take,out _),"Cut beyond recorded footage rejected");
                    desk.Seek(desk.Episode.Duration*.5f);float expected=desk.Episode.SourceTime(desk.Playhead,out _);Check(Mathf.Abs(take.Playhead-expected)<.002f,"EDL playhead maps to actual recorded lens timestamps");
                    Check(take.ReplayRoot.GetComponentsInChildren<Rigidbody>(true).Length==0&&take.ReplayRoot.GetComponentsInChildren<Surgeon>(true).Length==0,"Episode playback has no physics or enemy AI");
                    Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Episode preview has exactly one audio listener");
                    var input=EventSystem.current;Check(input&&input.currentSelectedGameObject,"Workstation has a keyboard/gamepad focus target");
                    take.Paused=false;Next();
                }else if(stage==9&&dt>1){
                    Check(session.Studio.Playhead>0&&take.Playhead>0,"Episode plays moving recorded footage");take.Paused=true;CaptureUI("Bobby_Edit.png");
                    string blocked=Path.Combine(career,"blocked-save");File.WriteAllText(blocked,"QA write failure");session.StorageDirectory=blocked;
                    Check(!session.Studio.Publish()&&session.Publications.Count==0&&session.TeamMoney==0,"Failed upload save pays nothing and keeps the draft");session.StorageDirectory=career;
                    Check(session.Studio.Publish(),"Upload commits episode and payment together");
                    var paid=session.Publications.Single();Check(paid.teamMinor+paid.personalMinor==paid.revenueMinor,"Integer 60/40 split conserves the full revenue");
                    Check(session.TeamMoney==paid.teamMinor&&session.PersonalMoney==paid.personalMinor,"Career wallet matches committed upload receipt");
                    int revision=session.SaveRevision;Check(!session.Studio.Publish()&&session.SaveRevision==revision,"Duplicate upload does not pay or create a save revision");
                    session.Studio.AutoEdit("Horror");Check(!session.Studio.Publish()&&session.Publications.Count==1,"Changing the edit cannot farm an already released expedition");
                    Check(!session.UpgradeBobby()&&session.BobbyLevel==0,"Insufficient funds cannot install a free upgrade");
                    Check(session.Studio.ViewPublished()&&session.Studio.Episode.id==paid.episode.id,"Archive preview restores the exact published cuts");
                    session.SaveDraft(session.Studio.Episode,take);session.Studio.Seek(1);CaptureUI("Bobby_Upload.png");File.WriteAllText(Path.Combine(QA,"bobby_career_path.txt"),career);session.Studio.Close();Next();
                }else if(stage==10&&dt>1&&session.Phase==RunSession.RunPhase.House){
                    Check(session.ReloadCareer()&&session.Publications.Count==1,"Committed episode and reward survive career reload");
                    Check(session.TeamMoney==session.Publications[0].teamMinor,"Reload restores the exact Team Fund");
                    string shop=career+"-purchase";Directory.CreateDirectory(shop);string payload="{\"version\":2,\"completedRuns\":0,\"revision\":0,\"bobbyLevel\":0,\"teamMinor\":5000,\"personalMinor\":0,\"subscribers\":0,\"views\":0,\"takes\":[],\"publications\":[]}";
                    File.WriteAllText(Path.Combine(shop,"HospitalRun.json"),JsonUtility.ToJson(new Envelope{payload=payload,checksum=BobbyEpisode.Hash(payload)}));
                    session.StorageDirectory=shop;Check(session.ReloadCareer()&&session.TeamMoney==5000,"Purchase check loads its own explicit funded fixture");Check(session.OpenStudio(),"Upgrades are available at the home workstation");
                    session.StorageDirectory=Path.Combine(career,"blocked-save");Check(!session.UpgradeBobby()&&session.TeamMoney==5000&&session.BobbyLevel==0,"Failed purchase save neither charges nor grants an upgrade");session.StorageDirectory=shop;
                    Check(session.UpgradeBobby()&&session.TeamMoney==2500&&session.BobbyLevel==1,"Upgrade and its cost commit in the same revision");
                    Check(!session.UpgradeBobby()&&session.TeamMoney==2500,"Installed upgrade cannot charge a second time");session.Studio.Close();
                    Check(session.ReloadCareer()&&session.BobbyLevel==1&&session.TeamMoney==2500,"Purchased capability persists after reload");Finish(null);
                }
            }catch(Exception e){Finish(e.ToString());}
        }
        static void ReloadTick(float dt)
        {
            if(stage==0&&dt>1){
                career=File.ReadAllText(Path.Combine(QA,"bobby_career_path.txt"));session=RunSession.Current;session.StorageDirectory=career;
                Check(session.ReloadCareer()&&session.Publications.Count==1,"Fresh Unity process restores published episode");Check(session.TeamMoney==session.Publications[0].teamMinor,"Fresh process restores exact upload balance");
                Check(session.OpenStudio(),"Fresh process reopens saved editing source");Next();
            }else if(stage==1&&dt>1&&session.Studio.IsOpen){
                take=session.Studio.Source;Check(take.Moments.Count>=2&&session.Studio.Episode.Validate(take,out _),"Fresh process restores filmed evidence and saved EDL");
                var boundary=JsonUtility.FromJson<BobbyEpisode>(JsonUtility.ToJson(session.Studio.Episode));
                boundary.cuts=new List<EpisodeCut>{new(){start=0,end=1.5f,reason="Boundary check"},new(){start=take.Duration-2,end=take.Duration,reason="Boundary check"}};
                Check(boundary.Validate(take,out _),"Two disjoint cuts can use real recorded ranges");
                float cutTime=boundary.SourceTime(1.5f,out int cutIndex);take.Seek(cutTime);
                Check(cutIndex==1&&Mathf.Abs(take.Playhead-(take.Duration-2))<.001f,"Exact cut boundary jumps to the next recorded source range");
                boundary.cuts.Reverse();Check(Mathf.Abs(boundary.SourceTime(0,out _)-(take.Duration-2))<.001f&&boundary.Validate(take,out _),"Manual clip order changes playback without changing source timestamps");
                Check(!session.Studio.Publish()&&session.Publications.Count==1,"Restart cannot pay the same expedition again");
                var evidence=(List<FilmedMoment>)take.Moments;var all=evidence.ToArray();var isolated=all.First(m=>m.start>1&&m.end<take.Duration-1);evidence.Clear();evidence.Add(isolated);
                var basic=BobbyEpisode.Build(take,"Documentary",0);var advanced=BobbyEpisode.Build(take,"Documentary",1);evidence.Clear();evidence.AddRange(all);
                Check(advanced.Budget>basic.Budget&&!advanced.cuts.Select(c=>$"{c.start:R}/{c.end:R}/{c.momentId}").SequenceEqual(basic.cuts.Select(c=>$"{c.start:R}/{c.end:R}/{c.momentId}")),"Editor upgrade changes actual clip decisions and context budget");
                session.Studio.Close();Next();
            }else if(stage==2&&dt>1&&session.Phase==RunSession.RunPhase.House){
                session.StorageDirectory=career+"-purchase";Check(session.ReloadCareer()&&session.BobbyLevel==1&&session.TeamMoney==2500,"Fresh process restores purchased editor capability");
                string recovery=career+"-recovery";Directory.CreateDirectory(recovery);
                File.Copy(Path.Combine(career,"HospitalRun.json"),Path.Combine(recovery,"HospitalRun.json"),true);File.Copy(Path.Combine(career,"HospitalRun.json.bak"),Path.Combine(recovery,"HospitalRun.json.bak"),true);
                var damaged=JsonUtility.FromJson<Envelope>(File.ReadAllText(Path.Combine(recovery,"HospitalRun.json")));damaged.payload+=" ";var damagedJson=JsonUtility.ToJson(damaged);
                File.WriteAllText(Path.Combine(recovery,"HospitalRun.json"),damagedJson);session.StorageDirectory=recovery;
                Check(session.ReloadCareer()&&session.Publications.Count==1&&session.Notice.Contains("backup"),"Valid JSON with a mismatched checksum restores the verified backup with notice");
                File.WriteAllText(Path.Combine(recovery,"HospitalRun.json.bak"),"damaged backup");Check(!session.ReloadCareer(),"Two damaged saves are reported instead of starting a new career");Check(!session.Depart(),"Damaged career cannot be silently overwritten by departure");
                Check(File.ReadAllText(Path.Combine(recovery,"HospitalRun.json"))==damagedJson,"Recovery failure preserves the original files");Finish(null);
            }
        }
        static void AutomaticTick(float dt)
        {
            if(stage==0&&dt>1){
                session=RunSession.Current;var existing=File.ReadAllText(Path.Combine(QA,"bobby_career_path.txt"));var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(Path.Combine(existing,"HospitalRun.json")));
                var fixture=new AutoCareer{takes=JsonUtility.FromJson<AutoCareer>(envelope.payload).takes};string payload=JsonUtility.ToJson(fixture);career=Path.Combine(QA,"BobbyAutomatic-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(career);
                File.WriteAllText(Path.Combine(career,"HospitalRun.json"),JsonUtility.ToJson(new Envelope{payload=payload,checksum=BobbyEpisode.Hash(payload)}));session.StorageDirectory=career;
                Check(session.ReloadCareer()&&session.Publications.Count==0&&session.Credits==0,"Automatic path uses a separate unpaid career with real recorded source");Check(session.OpenStudio(),"Load recovered tape for Bobby automatic editing");Next();
            }else if(stage==1&&dt>1&&session.Studio.IsOpen){
                var desk=session.Studio;Check(desk.AwaitingChoice,"Automatic path starts with an explicit handoff choice");
                string blocked=Path.Combine(career,"blocked-save");File.WriteAllText(blocked,"QA blocked write");session.StorageDirectory=blocked;
                Check(!desk.ChooseEditing(true)&&desk.AwaitingChoice&&session.Credits==0&&session.Publications.Count==0,"Failed automatic upload keeps choice available and pays nothing");session.StorageDirectory=career;
                Check(desk.ChooseEditing(true)&&!desk.AwaitingChoice&&session.Publications.Count==1,"One Bobby choice edits and uploads without a separate publish click");
                var paid=session.Publications.Single();Check(session.Credits==paid.revenueMinor&&session.QuotaViews==paid.views,"Automatic release settles Credit and contract Views independently");Check(!desk.Source.Paused,"Bobby starts playback of the completed episode");Next();
            }else if(stage==2&&dt>.5f){
                Check(session.Studio.Playhead>0,"Automatic result plays the recorded episode");CaptureUI("v5_Bobby_Automatic.png");CaptureUI("v5_Bobby_16x10.png",1440,900);long credit=session.Credits,views=session.QuotaViews;
                Check(!session.Studio.Publish()&&session.Credits==credit&&session.QuotaViews==views,"Automatic release cannot pay or fill quota twice");session.Studio.Close();Next();
            }else if(stage==3&&dt>1&&session.Phase==RunSession.RunPhase.House){Check(session.ReloadCareer()&&session.Publications.Count==1&&session.QuotaViews==session.Publications[0].views,"Automatic episode and quota survive career reload");Finish(null);}
        }
        static void CaptureLens(string name)
        {
            var camera=motor.mainCam.lens.GetComponent<Camera>();camera.Render();SaveImage(camera.targetTexture,name);
        }
        static void CaptureUI(string name,int width=1600,int height=900)
        {
            var canvas=session.Studio.GetComponentsInChildren<Canvas>().Single(c=>c.name=="BobbyWorkstation");
            foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
            var camera=new GameObject("QA_Camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;var scaler=canvas.GetComponent<CanvasScaler>();scaler.enabled=false;canvas.scaleFactor=Mathf.Min(width/1600f,height/900f);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            if(session.Studio.Source&&session.Studio.Source.ReviewCamera)session.Studio.Source.ReviewCamera.Render();
            foreach(var label in canvas.GetComponentsInChildren<Text>()){label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);label.SetAllDirty();}
            Canvas.ForceUpdateCanvases();camera.Render();SaveImage(rt,name);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=true;camera.targetTexture=null;Object.Destroy(rt);Object.Destroy(camera.gameObject);
        }
        static void SaveImage(RenderTexture rt,string name)
        {
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(QA,name),image.EncodeToPNG());RenderTexture.active=old;Object.Destroy(image);
        }
        static void Log(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception)&&stack.Contains("SpookTuber"))File.AppendAllText(Path.Combine(QA,"bobby_runtime_errors.log"),message+"\n"+stack+"\n");}
        static void Finish(string error)
        {
            SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            File.WriteAllText(Path.Combine(QA,SessionState.GetBool(Flag+".Automatic",false)?"v5_bobby_automatic.txt":SessionState.GetBool(Flag+".Legacy",false)?"legacy_take_validation.txt":SessionState.GetBool(Flag+".Reload",false)?"bobby_reload_validation.txt":"bobby_playmode_validation.txt"),(error==null?"PASS":"FAIL")+"\n"+string.Join("\n",checks)+"\n"+error);
            Debug.Log(error??"SPOOKTUBER_BOBBY_PASS");EditorApplication.Exit(error==null?0:1);
        }
    }
}
