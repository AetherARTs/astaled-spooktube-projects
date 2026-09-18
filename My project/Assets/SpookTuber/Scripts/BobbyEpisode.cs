using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SpookTuber
{
    [Serializable] public sealed class EpisodeCut
    {
        public string momentId="",cameraId="MAIN-01",reason="";
        public string[] supportingIds=Array.Empty<string>();
        public float start,end;
        public IEnumerable<string> EvidenceIds=>momentId==""?Array.Empty<string>():new[]{momentId}.Concat(supportingIds??Array.Empty<string>());
    }
    [Serializable] public sealed class BobbyEpisode
    {
        public int version=1,editorLevel;
        public string id="",takeId="",runId="",sourcePath="",preset="Documentary";
        public List<EpisodeCut> cuts=new();
        public float Duration=>cuts.Sum(c=>c.end-c.start);
        public float Budget=>editorLevel>0?36:24;
        public static string Hash(string value)
        {
            using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();
        }
        public void Identify()
        {
            var location=sourcePath;sourcePath="";id="";id=Hash(JsonUtility.ToJson(this));sourcePath=location;
        }
        public static BobbyEpisode Build(CrewTake take,string preset,int level)
        {
            var result=new BobbyEpisode{takeId=take.TakeId,runId=take.RunId,sourcePath=take.LastSavedPath,preset=preset,editorLevel=level};
            var candidates=take.Moments.Select(m=>new{moment=m,score=m.Quality*(preset=="Horror"?.35f+.65f*m.Danger:.65f+.35f*m.Danger)+(level>0?m.framing*.08f:0)})
                .OrderByDescending(c=>c.score).ThenBy(c=>c.moment.id,StringComparer.Ordinal);
            foreach(var candidate in candidates){
                var m=candidate.moment;
                float pre=level>0?2:1,post=level>0?2.5f:1;
                float start=Mathf.Max(0,m.start-pre),end=Mathf.Min(take.Duration,m.end+post);
                // ponytail: one MainCam take per episode; cross-take/cross-camera playback needs a streaming cache first.
                end=Mathf.Min(end,start+12);
                if(end-start<1.5f)continue;
                var overlaps=result.cuts.Where(c=>start<c.end&&end>c.start).ToArray();
                if(overlaps.Length==1){
                    var existing=overlaps[0];float from=Mathf.Min(start,existing.start),to=Mathf.Max(end,existing.end);
                    if(to-from<=12&&result.Duration-(existing.end-existing.start)+to-from<=result.Budget){
                        existing.start=from;existing.end=to;existing.supportingIds=existing.supportingIds.Append(m.id).ToArray();
                    }
                    continue;
                }
                if(overlaps.Length>0||result.Duration+end-start>result.Budget)continue;
                result.cuts.Add(new EpisodeCut{momentId=m.id,start=start,end=end,reason=m.kind+" / "+m.subject});
                if(result.cuts.Count>=8)break;
            }
            if(result.cuts.Count==0&&take.Duration>=1.5f)result.cuts.Add(new EpisodeCut{start=0,end=Mathf.Min(8,take.Duration),reason="Unmarked footage / no readable subject confirmed"});
            result.cuts.Sort((a,b)=>a.start.CompareTo(b.start));result.Identify();return result;
        }
        public bool Validate(CrewTake take,out string reason)
        {
            reason="Episode ready";
            if(version!=1||takeId!=take.TakeId||runId!=take.RunId||!Guid.TryParseExact(takeId,"N",out _)||!Guid.TryParseExact(runId,"N",out _)||editorLevel<0||editorLevel>1||preset!="Documentary"&&preset!="Horror"||cuts==null||cuts.Count==0||cuts.Count>8){reason="Record new hospital footage to create an episode";return false;}
            var used=new HashSet<string>();
            foreach(var c in cuts){
                if(c==null||c.cameraId!="MAIN-01"||!float.IsFinite(c.start)||!float.IsFinite(c.end)||c.start<0||c.end>take.Duration+.001f||c.end-c.start<1.5f||c.reason==null||c.reason.Length>256){reason="A cut is outside the recorded footage";return false;}
                if(c.supportingIds==null||c.supportingIds.Length>512){reason="Invalid supporting evidence";return false;}
                foreach(var id in c.EvidenceIds){
                    var moment=take.Moments.FirstOrDefault(m=>m.id==id);
                    if(moment==null||!used.Add(id)||Mathf.Min(c.end,moment.end)-Mathf.Max(c.start,moment.start)<.49f){reason="A cut no longer contains its filmed moment";return false;}
                }
            }
            var ordered=cuts.OrderBy(c=>c.start).ToArray();
            for(int i=1;i<ordered.Length;i++)if(ordered[i].start<ordered[i-1].end-.001f){reason="Cuts overlap / each recorded moment can appear once";return false;}
            if(Duration>Budget+.001f){reason="Episode exceeds the editor's duration budget";return false;}
            return true;
        }
        public float SourceTime(float episodeTime,out int index)
        {
            float cursor=Mathf.Clamp(episodeTime,0,Duration);index=0;
            for(int i=0;i<cuts.Count;i++){
                index=i;var c=cuts[i];float length=c.end-c.start;
                if(cursor<length||i==cuts.Count-1)return c.start+Mathf.Min(cursor,length);
                cursor-=length;
            }
            return 0;
        }
        public EpisodeReceipt Evaluate(CrewTake take,long subscribers)
        {
            if(!Validate(take,out var error))throw new ArgumentException(error);
            Identify();float quality=0,usable=0;var families=new HashSet<string>();var evidence=new List<string>();var comments=new List<string>();
            foreach(var c in cuts)foreach(var evidenceId in c.EvidenceIds){
                var m=take.Moments.FirstOrDefault(m=>m.id==evidenceId);if(m==null)continue;
                float length=Mathf.Max(0,Mathf.Min(c.end,m.end)-Mathf.Max(c.start,m.start));
                quality+=m.Quality*(.6f+.4f*m.Danger)*length;usable+=length;evidence.Add(m.id);
                if(families.Add(m.kind))comments.Add(m.kind=="Pursuit"?"It started chasing while the camera was rolling.":m.kind=="Attack"?"That blade swing was uncomfortably close.":m.kind=="Warning"?"The warning was the moment to back away.":"There is something moving in that hospital.");
            }
            quality=usable>0?quality/usable:.025f;
            float retention=Mathf.Clamp01(.3f+.45f*(usable/Duration)+.08f*families.Count);
            uint seed=Convert.ToUInt32(id.Substring(0,8),16);double noise=.9+(seed/(double)uint.MaxValue)*.2;
            long reach=800+Math.Min(subscribers,30000)*3;
            long views=(long)Math.Floor(reach*quality*retention*noise);
            if(comments.Count==0)comments.Add("I couldn't make out a subject in this one.");
            return new EpisodeReceipt{episode=JsonUtility.FromJson<BobbyEpisode>(JsonUtility.ToJson(this)),algorithm="local-audience-v1",views=views,subscribers=(long)Math.Floor(views*.02*quality),revenueMinor=views*3,quality=quality,retention=retention,comments=comments.ToArray(),evidenceMomentIds=evidence.ToArray(),uploadedUtc=DateTime.UtcNow.ToString("O")};
        }
    }
    [Serializable] public sealed class EpisodeReceipt
    {
        public BobbyEpisode episode;
        public string transactionId,algorithm,uploadedUtc;
        public long views,subscribers,revenueMinor,teamMinor,personalMinor;
        public float quality,retention;
        public string[] comments,evidenceMomentIds;
    }
    [Serializable] public sealed class RecoveredTake
    {
        public string id,runId,path;
        public float duration;
        public bool recovered;
        public int teamPercent=60;
    }
}
