using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpookTuber
{
    public sealed class RunSession : MonoBehaviour
    {
        public enum RunPhase { House, Loading, Hospital, Extracting, Returning, Review }
        public static RunSession Current {get;private set;}
        public RunPhase Phase {get;private set;}
        public string Notice {get;private set;}="";
        public bool IsBusy=>Phase==RunPhase.Loading||Phase==RunPhase.Returning||reviewLoading;
        public bool CanReview=>Phase==RunPhase.House||Phase==RunPhase.Review;
        public string LastTake=>record.takePath;
        public string RunId=>record.runId;
        public int CompletedRuns=>record.completedRuns;
        public long TeamMoney=>record.teamMinor;
        public long PersonalMoney=>record.personalMinor;
        public long Subscribers=>record.subscribers;
        public long TotalViews=>record.views;
        public long Credits=>record.teamMinor+record.personalMinor;
        public int QuotaDay=>record.quotaDay;
        public int QuotaCycle=>record.quotaCycle;
        public long QuotaViews=>record.quotaViews;
        public long QuotaTarget=>record.quotaTarget;
        public bool DayFinished=>record.dayFinished;
        public bool QuotaFailed=>record.quotaDay==3&&record.dayFinished&&record.quotaViews<record.quotaTarget;
        public string QuotaStatus=>$"DAY {QuotaDay}/3 / VIEWS {QuotaViews:N0}/{QuotaTarget:N0}";
        public int BobbyLevel=>record.bobbyLevel;
        public int SaveRevision=>record.revision;
        public IReadOnlyList<RecoveredTake> Takes=>record.takes;
        public IReadOnlyList<EpisodeReceipt> Publications=>record.publications;
        public BobbyEpisode Draft=>record.draft;
        public BobbyDesk Studio {get;private set;}
        public string StorageDirectory {get;set;}
        CrewMotor crew;
        CrewTake take;
        float countdown,downedAt=-1,retryAfter;
        bool hoodie=true;
        bool reviewLoading,studioRequested,backupRecovered;
        string blockedSavePath;
        string reviewPath;
        [Serializable] sealed class Journal
        {
            public int version=3,completedRuns,revision,bobbyLevel;
            public int quotaDay=1,quotaCycle=1,runCycle=1,contractsPassed;
            public long quotaViews,quotaTarget=1000;
            public bool dayFinished;
            public List<int> ownedGear=new();
            public string runId="",settledRunId="",takePath="",outcome="",finishedUtc="";
            public long teamMinor,personalMinor,subscribers,views;
            public List<RecoveredTake> takes=new();
            public List<EpisodeReceipt> publications=new();
            public BobbyEpisode draft;
        }
        [Serializable] sealed class Envelope { public int schema=2;public string payload,checksum; }
        Journal record=new();
        string SavePath=>Path.Combine(StorageDirectory,"HospitalRun.json");
        void Awake()
        {
            if(Current&&Current!=this){Destroy(gameObject);return;}
            Current=this;StorageDirectory=Application.persistentDataPath;
            transform.SetParent(null);DontDestroyOnLoad(gameObject);
            Studio=gameObject.AddComponent<BobbyDesk>();
            Phase=SceneManager.GetActiveScene().name=="Hospital"?RunPhase.Hospital:RunPhase.House;
            LoadJournal();SceneManager.sceneLoaded+=SceneLoaded;
        }
        void Start(){StartCoroutine(BindScene());}
        void OnDestroy(){if(Current==this){Current=null;SceneManager.sceneLoaded-=SceneLoaded;}}
        void SceneLoaded(Scene scene,LoadSceneMode mode){if(scene.name=="MainMenu"){Destroy(gameObject);return;}StartCoroutine(BindScene());}
        IEnumerator BindScene()
        {
            // CrewBody creates the stable detached-head track in Awake.
            yield return null;
            crew=FindFirstObjectByType<CrewMotor>();if(!crew)yield break;
            var rv=FindObjectsByType<MissionGate>(FindObjectsSortMode.None).FirstOrDefault(g=>g.action==MissionGate.Action.Extract);if(rv)RVPosition=rv.transform.position;
            take=crew.mainCam.Take;crew.GetComponent<CrewBody>().SetOutfit(hoodie);downedAt=-1;
            if(Phase==RunPhase.Review){
                foreach(var enemy in FindObjectsByType<Surgeon>(FindObjectsSortMode.None))enemy.enabled=false;
                if(!take.LoadTake(reviewPath)||!take.BeginReview()){
                    reviewLoading=false;Notice=take.Message;Phase=RunPhase.Returning;SceneManager.LoadSceneAsync("ProductionHouse");
                }
                else {reviewLoading=false;if(studioRequested){studioRequested=false;Studio.Open(take);}}
            }else if(SceneManager.GetActiveScene().name=="Hospital"){
                Phase=RunPhase.Hospital;Notice="HOSPITAL / record what you find, return to the RV";
                if(string.IsNullOrEmpty(record.runId))record.runId=Guid.NewGuid().ToString("N");
                crew.mainCam.TryPickup(crew);
            }else Phase=RunPhase.House;
        }
        public bool Depart()
        {
            if(Phase!=RunPhase.House||Studio.IsOpen||!crew||crew.GetComponent<CrewBody>().IsDowned||!FlushTake())return false;
            hoodie=crew.GetComponent<CrewBody>().WearsHoodie;
            var next=Copy();next.runId=Guid.NewGuid().ToString("N");next.outcome="In progress";
            if(next.dayFinished){
                if(next.quotaDay<3)next.quotaDay++;
                else if(next.quotaViews>=next.quotaTarget){next.contractsPassed++;next.quotaCycle++;next.quotaDay=1;next.quotaViews=0;next.quotaTarget=Math.Min(1000000,(long)(next.quotaTarget*1.35));}
                else {Notice="3-day quota missed / publish remaining footage or retry the contract from your phone";return false;}
                next.dayFinished=false;
            }
            next.runCycle=next.quotaCycle;
            if(!SaveJournal(next))return false;
            Notice="Loading hospital...";Phase=RunPhase.Loading;SceneTravel.Go("Hospital");return true;
        }
        bool FlushTake()
        {
            if(!take)return true;
            if(take.Recording&&!take.StopRecording()||take.Unsaved&&!take.SaveTake()){
                Notice="Footage is still in memory / free disk space and retry";return false;
            }
            return true;
        }
        public void RegisterTake(string path,string id,string runId,float duration)
        {
            if((Phase==RunPhase.Hospital||Phase==RunPhase.Extracting)&&runId==record.runId){
                record.takePath=path;
                if(!record.takes.Any(t=>t.id==id))record.takes.Add(new RecoveredTake{id=id,runId=runId,path=path,duration=duration,quotaCycle=record.runCycle});
                SaveJournal();
            }
        }
        public bool BeginExtraction()
        {
            if(Phase!=RunPhase.Hospital||!crew||crew.GetComponent<CrewBody>().IsDowned||!AtRV())return false;
            countdown=3;Phase=RunPhase.Extracting;return true;
        }
        public Vector3 RVPosition {get;private set;}=new(0,0,-5.4f);
        public bool AtRV()=>crew&&Vector3.Distance(crew.transform.position,RVPosition)<3.5f;
        void Update()
        {
            if(!crew||IsBusy||Phase==RunPhase.House)return;
            if(Phase==RunPhase.Review){if(take&&!take.Reviewing&&!Studio.IsOpen)ReturnFromReview();return;}
            if(crew.GetComponent<CrewBody>().IsDowned){
                if(downedAt<0)downedAt=Time.time;
                float remaining=Mathf.Max(0,10-(Time.time-downedAt));
                Notice=$"CREW OFFLINE / cloud recovery in {remaining:0}s";
                if(remaining==0&&Time.unscaledTime>=retryAfter){if(!Complete(false))retryAfter=Time.unscaledTime+2;}
                return;
            }
            if(Phase==RunPhase.Extracting){
                if(!AtRV()){Phase=RunPhase.Hospital;Notice="Departure cancelled / return to RV";return;}
                countdown-=Time.deltaTime;Notice=$"RV DEPARTURE / {Mathf.CeilToInt(countdown)} / step away to cancel";
                if(countdown<=0)Complete(true);
            }
        }
        bool Complete(bool extracted,string destination="ProductionHouse")
        {
            if(Phase!=RunPhase.Hospital&&Phase!=RunPhase.Extracting)return false;
            if(!FlushTake()){Phase=RunPhase.Hospital;return false;}
            // Settle once by run ID; reopening footage never completes another run.
            var next=Copy();
            if(next.settledRunId!=next.runId){
                next.completedRuns++;next.settledRunId=next.runId;
                next.dayFinished=true;
                next.outcome=extracted?"Extracted":"Cloud recovery";next.finishedUtc=DateTime.UtcNow.ToString("O");
                foreach(var source in next.takes.Where(t=>t.runId==next.runId))source.recovered=true;
            }
            if(!SaveJournal(next)){Phase=RunPhase.Hospital;return false;}
            Notice=extracted?"BACK HOME / equipment recovered / review footage at the desk":"CLOUD RECOVERY / crew and basic equipment restored";
            Phase=RunPhase.Returning;SceneTravel.Go(destination);return true;
        }
        public bool ReturnToTitle()
        {
            if(Phase==RunPhase.Hospital||Phase==RunPhase.Extracting)return Complete(false,"MainMenu");
            if(Phase!=RunPhase.House||!FlushTake()||!SaveJournal())return false;
            Phase=RunPhase.Returning;SceneTravel.Go("MainMenu");return true;
        }
        public bool ReviewLastMission()
        {
            if(Phase!=RunPhase.House||string.IsNullOrEmpty(record.takePath)||!File.Exists(record.takePath)){
                Notice="No hospital footage saved yet";return false;
            }
            if(!FlushTake())return false;
            string scene=CrewTake.SceneForTake(record.takePath);if(scene==null){Notice="Footage content is unavailable / original file kept";return false;}
            studioRequested=false;reviewPath=record.takePath;reviewLoading=true;Phase=RunPhase.Review;SceneTravel.Go(scene);return true;
        }
        public bool OpenStudio()
        {
            if(Phase!=RunPhase.House||Studio.IsOpen||!FlushTake())return false;
            var source=record.takes.LastOrDefault(t=>t.recovered&&File.Exists(t.path));
            if(source==null){Studio.Open(null);return true;}
            return SwitchStudioTake(source.path);
        }
        public bool SwitchStudioTake(string path)
        {
            if(Phase!=RunPhase.House&&Phase!=RunPhase.Review)return false;
            string scene=CrewTake.SceneForTake(path);if(scene==null){Notice="Footage content is unavailable / original file kept";return false;}
            if(Studio.IsOpen)Studio.Close(false);
            studioRequested=true;reviewPath=path;reviewLoading=true;Phase=RunPhase.Review;Notice="Bobby is loading the footage...";
            SceneTravel.Go(scene);return true;
        }
        public void CloseStudio(){if(Phase==RunPhase.Review)ReturnFromReview();}
        void ReturnFromReview(){Phase=RunPhase.Returning;Notice="FOOTAGE REVIEW COMPLETE";SceneTravel.Go("ProductionHouse");}
        Journal Copy()=>JsonUtility.FromJson<Journal>(JsonUtility.ToJson(record));
        public bool SaveDraft(BobbyEpisode episode,CrewTake source)
        {
            if(!Studio.IsOpen||!episode.Validate(source,out var reason)){Notice="Draft could not be saved";return false;}
            episode.Identify();var next=Copy();next.draft=JsonUtility.FromJson<BobbyEpisode>(JsonUtility.ToJson(episode));return SaveJournal(next);
        }
        public bool Publish(BobbyEpisode episode,CrewTake source,out EpisodeReceipt receipt)
        {
            receipt=null;
            if(!Studio.IsOpen||!source||!source.Reviewing||episode==null||!episode.Validate(source,out _)){Notice="Preview valid footage at Bobby's desk first";return false;}
            var recovered=record.takes.FirstOrDefault(t=>t.id==episode.takeId&&t.runId==episode.runId&&t.recovered);
            if(recovered==null){Notice="Bring this footage home before uploading";return false;}
            // A local expedition has one release. Different trims/take copies cannot farm the same expedition.
            receipt=record.publications.FirstOrDefault(p=>p.episode.runId==episode.runId);
            if(receipt!=null){Notice="This expedition already has a published episode / no extra payment";return false;}
            var next=Copy();var result=episode.Evaluate(source,record.subscribers);
            result.transactionId="reward:"+result.episode.id;result.teamMinor=result.revenueMinor*recovered.teamPercent/100;result.personalMinor=result.revenueMinor-result.teamMinor;
            try{checked{next.teamMinor+=result.teamMinor;next.personalMinor+=result.personalMinor;next.subscribers+=result.subscribers;next.views+=result.views;if(recovered.quotaCycle==next.quotaCycle)next.quotaViews+=result.views;}}
            catch(OverflowException){Notice="Career balance limit reached";return false;}
            next.publications.Add(result);next.draft=result.episode;
            if(!SaveJournal(next))return false;
            receipt=result;Notice=$"UPLOADED / {result.views:N0} views / +{result.subscribers:N0} subscribers";return true;
        }
        public const long BobbyUpgradeCost=2500;
        public bool RetryContract()
        {
            if(Phase!=RunPhase.House||Studio.IsOpen||!QuotaFailed)return false;
            var next=Copy();next.quotaCycle++;next.quotaDay=1;next.quotaViews=0;next.dayFinished=false;
            if(!SaveJournal(next))return false;Notice="New 3-day contract / owned equipment and Credit kept";return true;
        }
        public bool OwnsGear(CarryItem.Kind kind)=>kind==CarryItem.Kind.Camera||kind==CarryItem.Kind.GravityGlove||record.ownedGear.Contains((int)kind);
        public static long GearPrice(CarryItem.Kind kind)=>kind==CarryItem.Kind.ProductionLight?1200:kind==CarryItem.Kind.Noisemaker?600:0;
        public bool TradeGear(CarryItem.Kind kind,bool sell)
        {
            long price=GearPrice(kind);
            if(Phase!=RunPhase.House||Studio.IsOpen||price==0||OwnsGear(kind)!=sell){Notice="That trade is not available";return false;}
            if(!sell&&Credits<price){Notice="Not enough Credit";return false;}
            var next=Copy();
            if(sell){if(Credits>long.MaxValue-price/2){Notice="Career balance limit reached";return false;}next.ownedGear.Remove((int)kind);next.teamMinor+=price/2;}
            else{next.ownedGear.Add((int)kind);long team=Math.Min(next.teamMinor,price);next.teamMinor-=team;next.personalMinor-=price-team;}
            if(!SaveJournal(next))return false;
            if(sell)foreach(var item in FindObjectsByType<CarryItem>(FindObjectsSortMode.None))if(item.kind==kind&&item.Owner)item.Owner.Drop(item);
            Notice=(sell?"Sold / +":"Purchased / -")+(sell?price/2m:price)/100m+" Credit";return true;
        }
        public bool UpgradeBobby()
        {
            if(!Studio.IsOpen||record.bobbyLevel>=1){Notice="Bobby's context editor is already installed";return false;}
            if(Credits<BobbyUpgradeCost){Notice="Not enough Credit for the context editor";return false;}
            var next=Copy();long fromTeam=Math.Min(next.teamMinor,BobbyUpgradeCost);next.teamMinor-=fromTeam;next.personalMinor-=BobbyUpgradeCost-fromTeam;next.bobbyLevel=1;
            if(!SaveJournal(next))return false;
            Notice="CONTEXT EDITOR INSTALLED / longer lead-ins, cleaner framing, 36s episodes";return true;
        }
        public bool ReloadCareer()
        {
            if(Phase!=RunPhase.House||Studio.IsOpen)return false;
            return LoadJournal();
        }
        static void Validate(Journal loaded)
        {
            if(loaded==null||loaded.version!=3||loaded.completedRuns<0||loaded.revision<0||loaded.bobbyLevel<0||loaded.bobbyLevel>1||loaded.teamMinor<0||loaded.personalMinor<0||loaded.subscribers<0||loaded.views<0||loaded.takes==null||loaded.publications==null||loaded.takes.Count>4096||loaded.publications.Count>2048)throw new InvalidDataException("Invalid career");
            if(loaded.teamMinor>long.MaxValue-loaded.personalMinor||loaded.quotaDay<1||loaded.quotaDay>3||loaded.quotaCycle<1||loaded.quotaCycle>=int.MaxValue||loaded.runCycle<1||loaded.contractsPassed<0||loaded.quotaViews<0||loaded.quotaTarget<1||loaded.quotaTarget>1000000||loaded.ownedGear==null||loaded.ownedGear.Any(g=>g<2||g>3)||loaded.ownedGear.Distinct().Count()!=loaded.ownedGear.Count)throw new InvalidDataException("Invalid contract or equipment");
            if(loaded.takes.Any(t=>t==null||!Guid.TryParseExact(t.id,"N",out _)||!Guid.TryParseExact(t.runId,"N",out _)||string.IsNullOrEmpty(t.path)||t.path.Length>1024||!float.IsFinite(t.duration)||t.duration<0||t.duration>60.01f||t.teamPercent<0||t.teamPercent>100)||loaded.takes.Select(t=>t.id).Distinct().Count()!=loaded.takes.Count)throw new InvalidDataException("Invalid footage catalog");
            if(loaded.publications.Any(p=>p==null||p.episode==null||string.IsNullOrEmpty(p.transactionId)||p.transactionId!="reward:"+p.episode.id||p.views<0||p.subscribers<0||p.revenueMinor<0||p.teamMinor<0||p.personalMinor<0||p.teamMinor>p.revenueMinor||p.personalMinor!=p.revenueMinor-p.teamMinor)||loaded.publications.Select(p=>p.episode.runId).Distinct().Count()!=loaded.publications.Count)throw new InvalidDataException("Invalid reward ledger");
        }
        bool LoadJournal()
        {
            bool found=false;backupRecovered=false;
            foreach(var path in new[]{SavePath,SavePath+".bak"}){
                if(!File.Exists(path))continue;found=true;
                try{
                    if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("Career exceeds size limit");
                    var json=File.ReadAllText(path);var envelope=JsonUtility.FromJson<Envelope>(json);Journal loaded;
                    if(envelope!=null&&envelope.payload!=null){
                        if(envelope.schema!=2||BobbyEpisode.Hash(envelope.payload)!=envelope.checksum)throw new InvalidDataException("Career checksum failed");
                        loaded=JsonUtility.FromJson<Journal>(envelope.payload);
                    }else{
                        loaded=JsonUtility.FromJson<Journal>(json);
                        if(loaded==null||loaded.version!=1)throw new InvalidDataException("Unrecognized career format");
                        loaded.version=2;loaded.takes??=new();loaded.publications??=new();
                    }
                    if(loaded.version==2){loaded.version=3;loaded.quotaDay=1;loaded.quotaCycle=1;loaded.runCycle=1;loaded.quotaTarget=1000;loaded.quotaViews=0;loaded.dayFinished=false;loaded.ownedGear=new();}
                    Validate(loaded);record=loaded;blockedSavePath=null;backupRecovered=path.EndsWith(".bak",StringComparison.Ordinal);
                    if(backupRecovered)Notice="Restored career backup / revision "+record.revision;
                    return true;
                }catch(Exception e) when(e is IOException||e is InvalidDataException||e is ArgumentException||e is UnauthorizedAccessException){Notice="Run journal could not be read";}
            }
            if(found){blockedSavePath=SavePath;Notice="Career and backup could not be read / existing files kept";return false;}
            record=new Journal();blockedSavePath=null;return true;
        }
        bool SaveJournal(Journal candidate=null)
        {
            if(blockedSavePath==SavePath){Notice="Career recovery required / existing files kept";return false;}
            try{
                var next=candidate??Copy();next.revision=checked(record.revision+1);Validate(next);
                Directory.CreateDirectory(StorageDirectory);
                var payload=JsonUtility.ToJson(next);var json=JsonUtility.ToJson(new Envelope{payload=payload,checksum=BobbyEpisode.Hash(payload)},true);var pending=SavePath+".partial";
                using(var file=new FileStream(pending,FileMode.Create,FileAccess.Write,FileShare.None)){
                    var bytes=System.Text.Encoding.UTF8.GetBytes(json);file.Write(bytes,0,bytes.Length);file.Flush(true);
                }
                if(File.ReadAllText(pending)!=json)throw new IOException("Run journal verification failed");
                if(File.Exists(SavePath))File.Replace(pending,SavePath,SavePath+(backupRecovered?".damaged":".bak"));else File.Move(pending,SavePath);
                record=next;backupRecovered=false;
                return true;
            }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is InvalidDataException||e is OverflowException){Notice="Could not save career / no payment or purchase committed / retry";Debug.LogWarning(e.Message);return false;}
        }
    }
}
