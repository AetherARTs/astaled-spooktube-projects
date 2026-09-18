using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    [InitializeOnLoad]
    public static class HospitalPlayCheck
    {
        const string Flag="SpookTuber.HospitalCheck";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static readonly List<string> checks=new();
        static int stage,completed;
        static float stamp;
        static double started;
        static CrewMotor motor;
        static CrewBody body;
        static CrewTake take;
        static Surgeon surgeon;
        static HospitalDoor door;
        static Vector3 lastKnown;
        static string savedPath;
        static float savedDuration;
        static RunSession session;
        static HospitalPlayCheck(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
        public static void Run()
        {
            SessionState.SetBool(Flag+".Reload",false);
            SessionState.SetBool(Flag+".Door",false);
            SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/ProductionHouse.unity");
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        public static void RunReload()
        {
            SessionState.SetBool(Flag+".Door",false);
            SessionState.SetBool(Flag+".Reload",true);SessionState.SetBool(Flag,true);
            EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/Hospital.unity");EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        public static void RunDoor()
        {
            SessionState.SetBool(Flag+".Reload",false);SessionState.SetBool(Flag+".Door",true);SessionState.SetBool(Flag,true);
            EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/Hospital.unity");EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);Debug.Log("HOSPITAL_CHECK "+name);}
        static void Next(){stage++;stamp=Time.time;File.WriteAllText(Path.Combine(QA,"hospital_test_progress.txt"),"Stage "+stage+"\n"+string.Join("\n",checks));}
        static void Bind(){motor=Object.FindFirstObjectByType<CrewMotor>();body=motor.GetComponent<CrewBody>();take=motor.mainCam.Take;}
        static void Place(Vector3 position)
        {
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=position;cc.enabled=true;Physics.SyncTransforms();motor.UpdateView();
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{
                if(started==0){started=EditorApplication.timeSinceStartup;stamp=Time.time;Application.logMessageReceived+=Log;Application.runInBackground=true;
                    InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;}
                if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Hospital test timed out at stage "+stage);
                float dt=Time.time-stamp;
                if(SessionState.GetBool(Flag+".Door",false)){
                    if(stage==0&&dt>1){
                        Bind();surgeon=Object.FindFirstObjectByType<Surgeon>();door=GameObject.Find("Door_Link-10").GetComponent<HospitalDoor>();
                        Check(!door.IsOpen,"Investigation starts behind a closed connecting door");
                        surgeon.GetComponent<NavMeshAgent>().Warp(new Vector3(-5.5f,.08f,12));surgeon.Hear(new Vector3(-5.5f,0,6),20);Next();
                    }else if(stage==1){
                        if(dt>12&&!door.IsOpen)throw new Exception("Surgeon did not force the door: "+surgeon.State+" at "+surgeon.transform.position+" path="+surgeon.GetComponent<NavMeshAgent>().pathStatus);
                        if(door.IsOpen){Check(dt>=1.6f,"Surgeon reaches and forces the door after a warning delay");Next();}
                    }else if(stage==2&&dt>1){
                        Check(!Physics.Linecast(new Vector3(-5.5f,1,9.3f),new Vector3(-5.5f,1,10.7f),~((1<<8)|(1<<10))),"Forced door leaves a physically open passage");
                        var agent=surgeon.GetComponent<NavMeshAgent>();var path=new NavMeshPath();NavMesh.CalculatePath(agent.transform.position,agent.destination,NavMesh.AllAreas,path);
                        File.WriteAllText(Path.Combine(QA,"surgeon_door_probe.txt"),"State="+surgeon.State+"\nPosition="+agent.transform.position+"\nDestination="+agent.destination+"\nCurrent="+agent.pathStatus+"\nFresh="+path.status+"\nRemaining="+agent.remainingDistance+"\nObstacle="+door.obstacle.enabled);
                        Check(surgeon.GetComponent<NavMeshAgent>().pathStatus==NavMeshPathStatus.PathComplete,"Investigation resumes through the opened door");Next();
                    }else if(stage==3&&dt>3){
                        Check(surgeon.transform.position.z<9.4f,"Surgeon physically crosses into the next room");Finish(null);
                    }
                    return;
                }
                if(SessionState.GetBool(Flag+".Reload",false)&&dt>1){
                    Bind();var path=Directory.GetFiles(Path.Combine(QA,"HospitalRunTest/Takes"),"*.sttake").OrderByDescending(File.GetLastWriteTimeUtc).First();
                    bool loaded=take.LoadTake(path);Check(loaded,"Hospital take survives fresh Unity process / "+take.Message);
                    Check(AudioFrames(path)>0,"Persisted take retains recorded audio events");Finish(null);return;
                }
                if(stage==0&&dt>1){
                    Bind();session=RunSession.Current;Check(session&&session.Phase==RunSession.RunPhase.House,"House starts with a mission session");
                    session.StorageDirectory=Path.Combine(QA,"HospitalRunTest");completed=session.CompletedRuns;
                    Check(session.Depart(),"Depart loads hospital");Check(!session.Depart(),"Duplicate departure rejected");Next();
                }else if(stage==1&&SceneManager.GetActiveScene().name=="Hospital"&&session.Phase==RunSession.RunPhase.Hospital&&dt>1){
                    Bind();surgeon=Object.FindFirstObjectByType<Surgeon>();surgeon.enabled=false;
                    take.StorageDirectory=Path.Combine(QA,"HospitalRunTest/Takes");
                    Check(motor.mainCam.Holder==motor,"MainCam accompanies crew into the mission");
                    Check(surgeon.GetComponent<NavMeshAgent>().isOnNavMesh,"Surgeon spawns on walkable floor");
                    Place(new Vector3(0,.08f,2));
                    Check(!session.BeginExtraction(),"Extraction rejected outside RV zone");
                    Check(!take.BeginReview(),"Hospital cannot be paused by entering footage review");
                    door=GameObject.Find("Door_W1").GetComponent<HospitalDoor>();door.SetOpen(true);Next();
                }else if(stage==2&&dt>1.2f){
                    var path=new NavMeshPath();Check(NavMesh.CalculatePath(new Vector3(0,.08f,6),new Vector3(-3.1f,.08f,6),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Open door reconnects navigation");
                    Check(!Physics.Linecast(new Vector3(-.9f,1,6),new Vector3(-2.3f,1,6),~((1<<8)|(1<<10))),"Open doorway has physical clearance");
                    door.SetOpen(false);Next();
                }else if(stage==3&&dt>1.2f){
                    Check(Physics.Linecast(new Vector3(-.9f,1,6),new Vector3(-2.3f,1,6),~((1<<8)|(1<<10))),"Closed door blocks line of sight");
                    Place(new Vector3(0,.08f,8));
                    var agent=surgeon.GetComponent<NavMeshAgent>();Check(agent.Warp(new Vector3(0,.08f,13)),"Surgeon can reach main corridor");surgeon.transform.rotation=Quaternion.Euler(0,180,0);
                    surgeon.enabled=true;surgeon.Hear(body.transform.position,20);
                    Check(take.StartRecording(),"MainCam records hospital tracks");Next();
                }else if(stage==4&&dt>.25f){
                    Check(surgeon.State==Surgeon.Behaviour.Warning,"Surgeon gives a warning before pursuit");Check(!body.IsDowned,"Warning does not deal immediate damage");Next();
                }else if(stage==5&&dt>1.25f){
                    Check(surgeon.State==Surgeon.Behaviour.Hunt,"Visible crew is pursued after warning");
                    lastKnown=surgeon.LastKnownPosition;Place(new Vector3(-5,.08f,-3));Next();
                }else if(stage==6&&dt>.35f){
                    Check(!surgeon.SeesCrew,"Hospital walls occlude crew");
                    File.WriteAllText(Path.Combine(QA,"surgeon_sight_probe.txt"),"Before teleport="+lastKnown+"\nAfter settle="+surgeon.LastKnownPosition+"\nHidden crew="+body.transform.position);
                    Check(Vector3.Distance(surgeon.LastKnownPosition,body.transform.position)>2,"Interpolated bones cannot reveal a hidden root position");
                    lastKnown=surgeon.LastKnownPosition;Next();
                }else if(stage==7&&dt>7){
                    Check(Vector3.Distance(surgeon.LastKnownPosition,lastKnown)<.2f,"Occluded crew does not update last known position during search");
                    Check(surgeon.State!=Surgeon.Behaviour.Hunt&&surgeon.State!=Surgeon.Behaviour.Warning,"Lost target search is bounded");
                    Check(take.StopRecording(),"Hospital footage flushes to disk");savedPath=take.LastSavedPath;savedDuration=take.Duration;
                    Check(File.Exists(savedPath)&&savedDuration>8,"Saved hospital take contains the actual encounter timeline");
                    bool loaded=take.LoadTake(savedPath);Check(loaded,"Hospital take loads with recorded sound tracks / "+take.Message);
                    Check(AudioFrames(savedPath)>0,"Take contains playing audio samples");
                    Check(session.LastTake==savedPath,"Mission journal links the saved take");
                    surgeon.enabled=false;Place(new Vector3(0,.08f,-5.2f));
                    Check(session.BeginExtraction(),"RV starts extraction countdown");Check(!session.BeginExtraction(),"Duplicate extraction rejected");
                    Place(new Vector3(0,.08f,-1));Next();
                }else if(stage==8&&dt>.3f){
                    Check(session.Phase==RunSession.RunPhase.Hospital,"Walking away cancels departure");Place(new Vector3(0,.08f,-5.2f));
                    motor.mainCam.Drop();Check(session.BeginExtraction(),"Basic recovery permits departure after dropping gear");Next();
                }else if(stage==9&&session.Phase==RunSession.RunPhase.House&&dt>1){
                    Bind();Check(SceneManager.GetActiveScene().name=="ProductionHouse","Extraction returns to production house");
                    Check(session.CompletedRuns==completed+1,"Completed run is settled exactly once");
                    Check(motor.mainCam&&!body.IsDowned,"Crew and basic MainCam are restored at home");
                    Check(File.Exists(Path.Combine(session.StorageDirectory,"HospitalRun.json")),"Atomic run journal exists");
                    Check(session.ReviewLastMission(),"Home review opens saved hospital footage");Next();
                }else if(stage==10&&SceneManager.GetActiveScene().name=="Hospital"&&dt>1){
                    Bind();if(!take.Reviewing){if(dt>5)throw new Exception("Mission review failed: "+take.Message+" / "+session.Notice);return;}
                    Check(session.Phase==RunSession.RunPhase.Review,"Review has a separate mission phase");
                    surgeon=Object.FindFirstObjectByType<Surgeon>(FindObjectsInactive.Include);
                    Check(!surgeon.enabled&&!surgeon.GetComponent<NavMeshAgent>().enabled,"Review never runs enemy simulation");
                    Check(Mathf.Abs(take.Duration-savedDuration)<.01f,"Cross-scene review retains recorded duration");
                    Check(take.ReplayRoot.GetComponentsInChildren<Collider>().Length==0&&take.ReplayRoot.GetComponentsInChildren<Surgeon>().Length==0,"Replay clones have no gameplay or physics");
                    Check(take.ReplayRoot.GetComponentsInChildren<AudioSource>().Length>0,"Replay has recorded spatial sound sources");
                    Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Only recorded camera listens during review");
                    take.Seek(Mathf.Min(1,take.Duration));take.ReviewCamera.Render();SaveTexture(take.ReviewTexture,"Hospital_RecordedReview.png");
                    take.EndReview();Next();
                }else if(stage==11&&session.Phase==RunSession.RunPhase.House&&dt>1){
                    Bind();Check(session.CompletedRuns==completed+1,"Review grants no duplicate completion");
                    Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Live audio listener is restored after review");
                    Check(session.Depart(),"A second mission can start");Next();
                }else if(stage==12&&session.Phase==RunSession.RunPhase.Hospital&&dt>1){
                    Bind();surgeon=Object.FindFirstObjectByType<Surgeon>();
                    Place(new Vector3(0,.08f,8));var agent=surgeon.GetComponent<NavMeshAgent>();agent.Warp(new Vector3(0,.08f,9.1f));surgeon.transform.rotation=Quaternion.Euler(0,180,0);
                    surgeon.Hear(body.transform.position,20);Next();
                }else if(stage==13&&body&&body.IsDowned){
                    Check(true,"Surgeon windup can knock down crew in range");
                    Check(!session.BeginExtraction(),"Detached head cannot extract alone");Next();
                }else if(stage==14&&session.Phase==RunSession.RunPhase.House){
                    Bind();Check(!body.IsDowned,"Solo all-down returns through cloud recovery");
                    Check(session.CompletedRuns==completed+2,"Recovery settles only its own run");
                    string previous=session.StorageDirectory,blocked=Path.Combine(QA,"HospitalRunTest/write-blocker");File.WriteAllText(blocked,"file, not directory");session.StorageDirectory=blocked;
                    Check(!session.Depart()&&session.Phase==RunSession.RunPhase.House,"Failed journal write cannot transition out of home");session.StorageDirectory=previous;
                    Finish(null);
                }
            }catch(Exception e){Finish("Stage "+stage+": "+e);}
        }
        static int AudioFrames(string path)
        {
            using var r=new BinaryReader(File.OpenRead(path));Check(r.ReadString()=="SPOOKTAKE-3","New takes retain audio and filmed evidence");r.ReadString();r.ReadString();r.ReadString();
            int count=r.ReadInt32();r.ReadInt32();int frames=r.ReadInt32();
            for(int i=0;i<count;i++){r.ReadString();for(int j=0;j<3;j++)r.ReadSingle();}
            int audible=0;
            for(int frame=0;frame<frames;frame++){
                r.ReadSingle();for(int i=0;i<count;i++){for(int j=0;j<7;j++)r.ReadSingle();if((r.ReadByte()&8)!=0)audible++;r.ReadInt32();}
            }
            return audible;
        }
        static void SaveTexture(RenderTexture rt,string name)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(QA,name),image.EncodeToPNG());RenderTexture.active=previous;Object.Destroy(image);
        }
        static void Log(string message,string stack,LogType type)
        {
            if((type==LogType.Error||type==LogType.Exception)&&stack.Contains("SpookTuber"))File.AppendAllText(Path.Combine(QA,"hospital_runtime_errors.log"),message+"\n"+stack+"\n");
        }
        static void Finish(string failure)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SessionState.SetBool(Flag,false);
            File.WriteAllText(Path.Combine(QA,SessionState.GetBool(Flag+".Door",false)?"hospital_door_validation.txt":SessionState.GetBool(Flag+".Reload",false)?"hospital_take_reload_validation.txt":"hospital_playmode_validation.txt"),(failure==null?"PASS":"FAIL")+"\n"+string.Join("\n",checks)+"\n"+failure);
            Debug.Log(failure==null?"SPOOKTUBER_HOSPITAL_PLAY_PASS":failure);EditorApplication.Exit(failure==null?0:1);
        }
    }
}
