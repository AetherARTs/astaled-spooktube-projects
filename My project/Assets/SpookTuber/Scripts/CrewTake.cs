using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace SpookTuber
{
    // Local production-camera track. No AI, physics or gameplay callbacks run in the replay.
    [DefaultExecutionOrder(300)]
    public sealed class CrewTake : MonoBehaviour
    {
        const string Format="SPOOKTAKE-3";
        static string SceneIdentity=>SceneManager.GetActiveScene().name.Replace("_v4","").Replace("_v5","");
        static string Content=>SceneManager.GetActiveScene().name.EndsWith("_v4",StringComparison.Ordinal)?(SceneIdentity=="Hospital"?"hospital-crew-v1":"production-house-crew-v2"):(SceneIdentity=="Hospital"?"hospital-solo-v":"production-house-solo-v")+(SceneManager.GetActiveScene().name.EndsWith("_v5",StringComparison.Ordinal)?"5":"6");
        public static string SceneForTake(string path)
        {
            try{
                if(new FileInfo(path).Length>128*1024*1024)return null;
                using var reader=new BinaryReader(File.OpenRead(path));string format=reader.ReadString();if(format!="SPOOKTAKE-1"&&format!="SPOOKTAKE-2"&&format!="SPOOKTAKE-3")return null;
                string content=reader.ReadString();string scene=reader.ReadString();
                if(scene!="Hospital"&&scene!="ProductionHouse")return null;
                if(content=="hospital-crew-v1"&&scene=="Hospital"||content=="production-house-crew-v2"&&scene=="ProductionHouse")return scene+"_v4";
                if(content=="hospital-solo-v5"&&scene=="Hospital"||content=="production-house-solo-v5"&&scene=="ProductionHouse")return scene+"_v5";
                return content=="hospital-solo-v6"&&scene=="Hospital"||content=="production-house-solo-v6"&&scene=="ProductionHouse"?scene:null;
            }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is FormatException){return null;}
        }
        // ponytail: 60-second local takes; chunk streaming is required before long multi-camera runs.
        const float SampleInterval=.05f, MaxDuration=60;
        public RawImage reviewImage;
        public Text reviewLabel;
        public bool Recording {get;private set;}
        public bool Unsaved {get;private set;}
        public bool Reviewing=>replayRoot;
        public bool Paused {get;set;}
        public float Playhead {get;private set;}
        public float Duration=>frames.Count>0?frames[^1].time:0;
        public int FrameCount=>frames.Count;
        public string Message {get;private set;}="MAIN-01 / READY";
        public string LastSavedPath {get;private set;}
        public string StorageDirectory {get;set;}
        public Transform ReplayRoot=>replayRoot?replayRoot.transform:null;
        public Camera ReviewCamera=>replayCamera;
        public RenderTexture ReviewTexture=>texture;
        public string TakeId {get;private set;}="";
        public string RunId {get;private set;}="";
        public IReadOnlyList<FilmedMoment> Moments=>evidence.moments;
        public bool ExternalPlayback {get;set;}
        readonly TakeEvidence evidence=new();
        MainCam equipment;
        Transform[] sources,ghosts;
        Renderer[] renderers,ghostRenderers;
        Light[] lights,ghostLights;
        AudioSource[] sounds,ghostSounds;
        AudioSource[] mutedSources;
        bool[] originalMute;
        AudioListener[] liveListeners;
        bool[] originalListeners;
        string[] keys;
        Vector3[] scales;
        int lensIndex;
        float began,nextSample;
        GameObject replayRoot;
        Camera replayCamera;
        RenderTexture texture;
        readonly List<Frame> frames=new();
        sealed class Frame
        {
            public float time;
            public Vector3[] positions;
            public Quaternion[] rotations;
            public byte[] flags;
            public int[] samples;
            public Frame(int n){positions=new Vector3[n];rotations=new Quaternion[n];flags=new byte[n];samples=new int[n];}
        }
        void Awake(){equipment=GetComponent<MainCam>();StorageDirectory=Path.Combine(Application.persistentDataPath,"Takes");}
        static string Key(Transform t)
        {
            if(t.TryGetComponent<MainCam>(out var item))return "Camera:"+item.cameraId;
            if(t.TryGetComponent<CarryItem>(out var gear)&&!string.IsNullOrEmpty(gear.itemId))return "Gear:"+gear.itemId;
            int ordinal=0;
            var siblings=t.parent?t.parent.Cast<Transform>():t.gameObject.scene.GetRootGameObjects().Select(g=>g.transform);
            foreach(var sibling in siblings){if(sibling==t)break;if(sibling.name==t.name)ordinal++;}
            return (t.parent?Key(t.parent)+"/":"")+t.name+"["+ordinal+"]";
        }
        static Dictionary<string,Transform> SceneSources()
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(g=>!g.GetComponent<Canvas>()&&g.layer!=9&&g.name!="QA_Camera")
                .SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.gameObject.layer!=9&&!t.GetComponentInParent<Canvas>(true))
                .ToDictionary(Key,t=>t,StringComparer.Ordinal);
        }
        void Bind(Transform[] nodes)
        {
            sources=nodes;renderers=nodes.Select(t=>t.GetComponent<Renderer>()).ToArray();lights=nodes.Select(t=>t.GetComponent<Light>()).ToArray();sounds=nodes.Select(t=>t.GetComponent<AudioSource>()).ToArray();
            foreach(var light in lights)if(light)light.cullingMask&=~(1<<9);
        }
        public bool StartRecording()
        {
            if(Recording||Reviewing||Unsaved||!equipment.Holder||equipment.Holder.GetComponent<CrewBody>().IsDowned)return false;
            var map=SceneSources();keys=map.Keys.OrderBy(k=>k,StringComparer.Ordinal).ToArray();
            if(keys.Length>3000){Message="Scene exceeds camera track capacity";return false;}
            Bind(keys.Select(k=>map[k]).ToArray());scales=sources.Select(t=>t.lossyScale).ToArray();
            lensIndex=Array.IndexOf(sources,equipment.lens);
            if(lensIndex<0){Message="Camera lens unavailable";return false;}
            frames.Clear();TakeId=Guid.NewGuid().ToString("N");RunId=RunSession.Current?RunSession.Current.RunId:"";
            evidence.Begin(TakeId,equipment.lens,lights);began=Time.time;nextSample=began;Recording=true;
            Message="REC / MAIN-01";return true;
        }
        void Capture()
        {
            var f=new Frame(sources.Length){time=Mathf.Min(Time.time-began,MaxDuration)};
            for(int i=0;i<sources.Length;i++){
                var t=sources[i];if(!t)continue;
                f.positions[i]=t.position;f.rotations[i]=t.rotation;
                f.flags[i]=(byte)((t.gameObject.activeInHierarchy?1:0)|(renderers[i]&&renderers[i].enabled?2:0)|(lights[i]&&lights[i].enabled?4:0)|(sounds[i]&&sounds[i].isPlaying&&!sounds[i].mute?8:0));
                if(sounds[i]&&sounds[i].clip)f.samples[i]=Mathf.Clamp(sounds[i].timeSamples,0,Mathf.Max(0,sounds[i].clip.samples-1));
            }
            frames.Add(f);
            evidence.Sample(equipment.lens,f.time);
        }
        public bool StopRecording()
        {
            if(!Recording)return false;
            if(frames.Count==0||Mathf.Min(Time.time-began,MaxDuration)>Duration+.001f)Capture();
            evidence.Finish();Recording=false;Unsaved=true;return SaveTake();
        }
        public bool SaveTake()
        {
            if(Recording||frames.Count==0)return false;
            if(!Unsaved)return !string.IsNullOrEmpty(LastSavedPath)&&File.Exists(LastSavedPath);
            try{
                Directory.CreateDirectory(StorageDirectory);
                string final=Path.Combine(StorageDirectory,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".sttake");
                string pending=final+".partial";
                using(var stream=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                    using(var w=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){
                        w.Write(Format);w.Write(Content);w.Write(SceneIdentity);w.Write(equipment.cameraId);
                        w.Write(keys.Length);w.Write(lensIndex);w.Write(frames.Count);
                        for(int i=0;i<keys.Length;i++){w.Write(keys[i]);Write(w,scales[i]);}
                        foreach(var f in frames){w.Write(f.time);for(int i=0;i<keys.Length;i++){
                            Write(w,f.positions[i]);var q=f.rotations[i];w.Write(q.x);w.Write(q.y);w.Write(q.z);w.Write(q.w);w.Write(f.flags[i]);w.Write(f.samples[i]);
                        }}
                        w.Write(TakeId);w.Write(RunId);evidence.Write(w);
                        w.Flush();
                    }
                    stream.Flush(true);
                }
                File.Move(pending,final);LastSavedPath=final;Unsaved=false;
                if(RunSession.Current)RunSession.Current.RegisterTake(final,TakeId,RunId,Duration);
                Message=$"TAKE SAVED / {Duration:0.0}s / "+(RunSession.Current&&!RunSession.Current.CanReview?"REVIEW AT HOME":"P TO REVIEW");return true;
            }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){Message="SAVE FAILED / take kept in memory / R TO RETRY";Debug.LogWarning(e.Message);return false;}
        }
        static void Write(BinaryWriter w,Vector3 v){w.Write(v.x);w.Write(v.y);w.Write(v.z);}
        static float Number(BinaryReader r){float f=r.ReadSingle();if(!float.IsFinite(f))throw new InvalidDataException("Non-finite track");return f;}
        static Vector3 Vector(BinaryReader r)=>new Vector3(Number(r),Number(r),Number(r));
        public bool LoadTake(string path)
        {
            if(Recording||Reviewing||Unsaved)return false;
            try{
                if(new FileInfo(path).Length>128*1024*1024)throw new InvalidDataException("Take exceeds memory budget");
                using var r=new BinaryReader(File.OpenRead(path));
                var format=r.ReadString();bool hasEvidence=format==Format;bool hasAudio=hasEvidence||format=="SPOOKTAKE-2";
                if((!hasAudio&&format!="SPOOKTAKE-1")||r.ReadString()!=Content||r.ReadString()!=SceneIdentity||r.ReadString()!=equipment.cameraId)throw new InvalidDataException("Take belongs to another scene or content version");
                int count=r.ReadInt32(),lens=r.ReadInt32(),length=r.ReadInt32();
                if(count<1||count>4096||lens<0||lens>=count||length<1||length>1203)throw new InvalidDataException("Invalid take size");
                var names=new string[count];var size=new Vector3[count];
                for(int i=0;i<count;i++){names[i]=r.ReadString();if(names[i].Length>1024)throw new InvalidDataException("Invalid binding");size[i]=Vector(r);}
                var map=SceneSources();if(names.Any(n=>!map.ContainsKey(n))||names.Distinct().Count()!=count)throw new InvalidDataException("Take assets unavailable: "+string.Join(", ",names.Where(n=>!map.ContainsKey(n)).Take(3)));
                var loaded=new List<Frame>();
                for(int j=0;j<length;j++){
                    var f=new Frame(count){time=Number(r)};
                    if(f.time<0||f.time>MaxDuration+.01f||(j>0&&f.time<=loaded[^1].time))throw new InvalidDataException("Invalid track timestamp");
                    for(int i=0;i<count;i++){
                        f.positions[i]=Vector(r);var q=new Quaternion(Number(r),Number(r),Number(r),Number(r));
                        if(Mathf.Abs(Quaternion.Dot(q,q)-1)>.01f)throw new InvalidDataException("Invalid rotation");
                        f.rotations[i]=q;f.flags[i]=r.ReadByte();if(f.flags[i]>(hasAudio?15:7))throw new InvalidDataException("Invalid visibility");
                        if(hasAudio){f.samples[i]=r.ReadInt32();var source=map[names[i]].GetComponent<AudioSource>();
                            if(f.samples[i]<0||(source&&source.clip&&f.samples[i]>source.clip.samples)||((f.flags[i]&8)!=0&&(!source||!source.clip)))throw new InvalidDataException("Invalid audio track: "+names[i]+" sample="+f.samples[i]+" length="+(source&&source.clip?source.clip.samples:0));
                            // Unity can report exactly clip.samples at the end; seeking requires the last valid sample.
                            if(source&&source.clip)f.samples[i]=Mathf.Min(f.samples[i],Mathf.Max(0,source.clip.samples-1));}
                    }
                    loaded.Add(f);
                }
                string id="",run="";var filmed=new List<FilmedMoment>();
                if(hasEvidence){
                    id=r.ReadString();run=r.ReadString();
                    if(!Guid.TryParseExact(id,"N",out _)||run.Length>0&&!Guid.TryParseExact(run,"N",out _))throw new InvalidDataException("Invalid take identity");
                    filmed=TakeEvidence.Read(r,loaded[^1].time,id);
                }
                if(r.BaseStream.Position!=r.BaseStream.Length)throw new InvalidDataException("Unexpected trailing data");
                Bind(names.Select(n=>map[n]).ToArray());keys=names;scales=size;lensIndex=lens;
                TakeId=id;RunId=run;evidence.moments.Clear();evidence.moments.AddRange(filmed);
                frames.Clear();frames.AddRange(loaded);LastSavedPath=path;Message="TAKE LOADED";return true;
            }catch(Exception e) when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException){Message="Cannot open take: "+e.Message;return false;}
        }
        public bool BeginReview()
        {
            if(Recording||Reviewing)return false;
            if(RunSession.Current&&!RunSession.Current.CanReview){Message="Review footage back at the production house";return false;}
            if(frames.Count==0){
                try{
                    var path=Directory.Exists(StorageDirectory)?Directory.GetFiles(StorageDirectory,"*.sttake").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault():null;
                    if(path==null){Message="NO RECORDED TAKES YET";return false;}
                    if(!LoadTake(path))return false;
                }catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){Message=e.Message;return false;}
            }
            replayRoot=new GameObject("TakeReview");replayRoot.layer=9;
            ghosts=new Transform[sources.Length];ghostRenderers=new Renderer[sources.Length];ghostLights=new Light[sources.Length];ghostSounds=new AudioSource[sources.Length];
            mutedSources=FindObjectsByType<AudioSource>(FindObjectsSortMode.None);originalMute=mutedSources.Select(s=>s.mute).ToArray();
            foreach(var sound in mutedSources)sound.mute=true;
            liveListeners=FindObjectsByType<AudioListener>(FindObjectsSortMode.None);originalListeners=liveListeners.Select(l=>l.enabled).ToArray();
            foreach(var listener in liveListeners)listener.enabled=false;
            var map=new Dictionary<Transform,Transform>();
            for(int i=0;i<sources.Length;i++){
                var go=new GameObject(sources[i].name);go.layer=9;go.transform.SetParent(replayRoot.transform);
                go.transform.localScale=scales[i];ghosts[i]=go.transform;map[sources[i]]=go.transform;
            }
            for(int i=0;i<sources.Length;i++){
                var src=sources[i];var go=ghosts[i].gameObject;
                if(src.TryGetComponent<SkinnedMeshRenderer>(out var skin)){
                    var clone=go.AddComponent<SkinnedMeshRenderer>();clone.sharedMesh=skin.sharedMesh;clone.sharedMaterials=skin.sharedMaterials;
                    clone.bones=skin.bones.Select(b=>map[b]).ToArray();clone.rootBone=skin.rootBone?map[skin.rootBone]:null;
                    clone.localBounds=skin.localBounds;clone.updateWhenOffscreen=true;ghostRenderers[i]=clone;
                }else if(src.TryGetComponent<TextMesh>(out var label)){
                    var clone=go.AddComponent<TextMesh>();clone.text=label.text;clone.font=label.font;clone.fontSize=label.fontSize;
                    clone.characterSize=label.characterSize;clone.anchor=label.anchor;clone.alignment=label.alignment;clone.color=label.color;
                    ghostRenderers[i]=go.GetComponent<Renderer>();ghostRenderers[i].sharedMaterials=renderers[i].sharedMaterials;
                }else if(src.TryGetComponent<MeshFilter>(out var mesh)&&renderers[i]){
                    go.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                    ghostRenderers[i]=go.AddComponent<MeshRenderer>();ghostRenderers[i].sharedMaterials=renderers[i].sharedMaterials;
                }
                if(ghostRenderers[i])ghostRenderers[i].renderingLayerMask=renderers[i].renderingLayerMask<<2;
                if(lights[i]){
                    var live=lights[i];var clone=go.AddComponent<Light>();clone.type=live.type;clone.color=live.color;clone.intensity=live.intensity;
                    clone.range=live.range;clone.spotAngle=live.spotAngle;clone.innerSpotAngle=live.innerSpotAngle;clone.shadows=live.shadows;
                    clone.shadowBias=live.shadowBias;clone.shadowNormalBias=live.shadowNormalBias;clone.cullingMask=1<<9;ghostLights[i]=clone;
                    clone.GetUniversalAdditionalLightData().renderingLayers=(uint)live.GetUniversalAdditionalLightData().renderingLayers<<2;
                }
                if(sounds[i]&&sounds[i].clip){
                    var live=sounds[i];var clone=go.AddComponent<AudioSource>();clone.clip=live.clip;clone.volume=live.volume;clone.pitch=live.pitch;
                    clone.spatialBlend=live.spatialBlend;clone.rolloffMode=live.rolloffMode;clone.minDistance=live.minDistance;clone.maxDistance=live.maxDistance;
                    clone.loop=live.loop;clone.playOnAwake=false;clone.dopplerLevel=0;ghostSounds[i]=clone;
                }
            }
            var cameraObject=new GameObject("RecordedCamera");cameraObject.transform.SetParent(replayRoot.transform);
            replayCamera=cameraObject.AddComponent<Camera>();replayCamera.enabled=false;replayCamera.cullingMask=1<<9;
            replayCamera.clearFlags=CameraClearFlags.SolidColor;replayCamera.backgroundColor=RenderSettings.fog?RenderSettings.fogColor:Color.black;
            cameraObject.AddComponent<AudioListener>();
            replayCamera.fieldOfView=65;replayCamera.nearClipPlane=.035f;
            texture=new RenderTexture(960,540,24);texture.Create();replayCamera.targetTexture=texture;
            if(reviewImage){reviewImage.texture=texture;reviewImage.gameObject.SetActive(true);}
            if(reviewLabel)reviewLabel.gameObject.SetActive(true);
            Playhead=0;Paused=false;ExternalPlayback=false;Seek(0);return true;
        }
        public void Seek(float time)
        {
            if(!Reviewing)return;
            Playhead=Mathf.Clamp(time,0,Duration);int hi=frames.FindIndex(f=>f.time>=Playhead);if(hi<0)hi=frames.Count-1;
            int lo=Mathf.Max(0,hi-1);var a=frames[lo];var b=frames[hi];
            float t=b.time>a.time?Mathf.InverseLerp(a.time,b.time,Playhead):0;
            for(int i=0;i<ghosts.Length;i++){
                ghosts[i].SetPositionAndRotation(Vector3.Lerp(a.positions[i],b.positions[i],t),Quaternion.Slerp(a.rotations[i],b.rotations[i],t));
                byte flags=(t>=1?b:a).flags[i];bool active=(flags&1)!=0;
                if(ghostRenderers[i])ghostRenderers[i].enabled=active&&(flags&2)!=0;
                if(ghostLights[i])ghostLights[i].enabled=active&&(flags&4)!=0;
                if(ghostSounds[i]){
                    var sound=ghostSounds[i];bool playing=active&&(flags&8)!=0&&!Paused;
                    if(!playing){if(sound.isPlaying)sound.Stop();}
                    else{
                        int sample=(t>=1?b:a).samples[i];
                        if(!sound.isPlaying){sound.timeSamples=sample;sound.Play();}
                        else if(Mathf.Abs(sound.timeSamples-sample)>sound.clip.frequency*.12f)sound.timeSamples=sample;
                    }
                }
            }
            replayCamera.transform.SetPositionAndRotation(ghosts[lensIndex].position,ghosts[lensIndex].rotation);
        }
        public void EndReview()
        {
            ExternalPlayback=false;
            if(replayCamera)replayCamera.GetComponent<AudioListener>().enabled=false;
            if(mutedSources!=null)for(int i=0;i<mutedSources.Length;i++)if(mutedSources[i])mutedSources[i].mute=originalMute[i];
            if(liveListeners!=null)for(int i=0;i<liveListeners.Length;i++)if(liveListeners[i])liveListeners[i].enabled=originalListeners[i];
            mutedSources=null;liveListeners=null;
            if(reviewImage){reviewImage.texture=null;reviewImage.gameObject.SetActive(false);}
            if(reviewLabel)reviewLabel.gameObject.SetActive(false);
            if(replayRoot)Destroy(replayRoot);replayRoot=null;
            if(texture){texture.Release();Destroy(texture);texture=null;}
        }
        void LateUpdate()
        {
            if(Recording&&Time.time>=nextSample){Capture();nextSample=Time.time+SampleInterval;if(Duration>=MaxDuration)StopRecording();}
            if(!Reviewing)return;
            if(!Paused&&!ExternalPlayback){Seek(Playhead+Time.unscaledDeltaTime);if(Playhead>=Duration)Paused=true;}
            if(Paused)foreach(var sound in ghostSounds)if(sound&&sound.isPlaying)sound.Stop();
            replayCamera.Render();
            if(reviewLabel)reviewLabel.text=$"TAKE / {equipment.cameraId}    {Playhead:0.0} / {Duration:0.0}s    {(Paused?"PAUSED":"PLAYING")}\nSPACE  PAUSE     ARROWS  SCRUB     P  CLOSE";
        }
        void OnApplicationQuit(){if(Recording)StopRecording();else if(Unsaved)SaveTake();}
        void OnDestroy(){EndReview();}
    }
}
