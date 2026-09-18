using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpookTuber
{
    [Serializable] public sealed class FilmedMoment
    {
        public string id,subject,kind;
        public float start,end,visible,readable,framing,stability;
        public float Quality=>Mathf.Clamp01(visible*readable*(.65f+.25f*framing+.10f*stability));
        public float Danger=>kind=="Attack"?1:kind=="Pursuit"?.85f:kind=="Warning"?.55f:.15f;
    }

    // Measures what the recording lens actually sees; proximity and player inputs earn nothing.
    public sealed class TakeEvidence
    {
        public const float Interval=.2f, MinimumVisible=.5f, GapTolerance=.75f;
        public readonly List<FilmedMoment> moments=new();
        readonly Dictionary<Surgeon,FilmedMoment> active=new();
        readonly Dictionary<string,int> samples=new();
        Surgeon[] subjects;
        Light[] lights;
        Quaternion previous;
        float next,last;
        string takeId;
        public void Begin(string id,Transform lens,Light[] sceneLights)
        {
            moments.Clear();active.Clear();samples.Clear();takeId=id;next=0;last=0;previous=lens.rotation;
            subjects=UnityEngine.Object.FindObjectsByType<Surgeon>(FindObjectsSortMode.None).OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            lights=sceneLights.Where(l=>l).ToArray();
        }
        public void Sample(Transform lens,float time)
        {
            if(time<next)return;next=time+Interval;
            float stability=1-Mathf.Clamp01(Quaternion.Angle(previous,lens.rotation)/Mathf.Max(.01f,time-last)/180);
            previous=lens.rotation;last=time;
            foreach(var subject in subjects){
                if(!subject||!subject.gameObject.activeInHierarchy)continue;
                var measure=Measure(lens,subject,lights);
                if(measure.visible<.66f||measure.readable<.12f||measure.framing<.12f)continue;
                string kind=subject.State==Surgeon.Behaviour.Attack?"Attack":subject.State==Surgeon.Behaviour.Hunt?"Pursuit":subject.State==Surgeon.Behaviour.Warning?"Warning":"Sighting";
                if(!active.TryGetValue(subject,out var moment)||time-moment.end>GapTolerance||moment.kind!=kind){
                    moment=new FilmedMoment{id=takeId+":"+moments.Count,subject=subject.name,kind=kind,start=time,end=time};
                    active[subject]=moment;moments.Add(moment);samples[moment.id]=0;
                }
                int n=++samples[moment.id];moment.end=time;
                moment.visible+=(measure.visible-moment.visible)/n;moment.readable+=(measure.readable-moment.readable)/n;
                moment.framing+=(measure.framing-moment.framing)/n;moment.stability+=(stability-moment.stability)/n;
            }
        }
        public void Finish(){moments.RemoveAll(m=>m.end-m.start<MinimumVisible);}
        static Vector3 Project(Transform lens,Vector3 point)
        {
            var p=lens.InverseTransformPoint(point);float height=Mathf.Tan(65*Mathf.Deg2Rad*.5f)*p.z;
            return new Vector3(.5f+p.x/(2*height*16/9),.5f+p.y/(2*height),p.z);
        }
        public static (float visible,float readable,float framing) Measure(Transform lens,Surgeon subject,Light[] lights)
        {
            var points=new[]{subject.eye.position,subject.transform.position+Vector3.up*1.15f,subject.transform.position+Vector3.up*.35f};
            float visible=0,readable=0;
            const int mask=~((1<<8)|(1<<9)|(1<<10));
            foreach(var point in points){
                var screen=Project(lens,point);float distance=Vector3.Distance(lens.position,point);
                if(screen.z<=.05f||distance>18||screen.x<.04f||screen.x>.96f||screen.y<.04f||screen.y>.96f||Physics.Linecast(lens.position,point,mask,QueryTriggerInteraction.Ignore))continue;
                visible+=1f/points.Length;
                // ponytail: light/occlusion estimate for this authored map; sensor profiles are needed for IR/thermal and volumetric fog.
                float illumination=RenderSettings.ambientLight.grayscale*1.8f;
                foreach(var light in lights){
                    if(!light||!light.isActiveAndEnabled||light.intensity<=0)continue;
                    var delta=point-light.transform.position;float d=delta.magnitude;
                    if(light.type!=LightType.Point&&light.type!=LightType.Spot)continue;
                    if(d>=light.range||Physics.Linecast(light.transform.position,point,mask,QueryTriggerInteraction.Ignore))continue;
                    float cone=light.type==LightType.Spot?1-Mathf.InverseLerp(light.innerSpotAngle*.5f,light.spotAngle*.5f,Vector3.Angle(light.transform.forward,delta)):1;
                    illumination+=light.intensity*light.color.grayscale*Mathf.Pow(1-d/light.range,2)*cone;
                }
                float fog=RenderSettings.fog?1-Mathf.InverseLerp(RenderSettings.fogStartDistance,RenderSettings.fogEndDistance,distance):1;
                readable+=Mathf.Clamp01(illumination)*fog/points.Length;
            }
            var head=Project(lens,points[0]);var feet=Project(lens,points[2]);var center=Project(lens,points[1]);
            float span=Mathf.Abs(head.y-feet.y);
            float size=Mathf.InverseLerp(.04f,.3f,span)*(1-Mathf.InverseLerp(.85f,1.3f,span));
            float framing=size*(1-Mathf.Clamp01(Vector2.Distance(new Vector2(center.x,center.y),new Vector2(.5f,.5f))*1.3f));
            return(visible,visible>0?readable/visible:0,framing);
        }
        public void Write(BinaryWriter w)
        {
            w.Write(moments.Count);
            foreach(var m in moments){w.Write(m.id);w.Write(m.subject);w.Write(m.kind);w.Write(m.start);w.Write(m.end);w.Write(m.visible);w.Write(m.readable);w.Write(m.framing);w.Write(m.stability);}
        }
        public static List<FilmedMoment> Read(BinaryReader r,float duration,string id)
        {
            int count=r.ReadInt32();if(count<0||count>512)throw new InvalidDataException("Invalid evidence count");
            var result=new List<FilmedMoment>();var ids=new HashSet<string>();
            for(int i=0;i<count;i++){
                var m=new FilmedMoment{id=r.ReadString(),subject=r.ReadString(),kind=r.ReadString(),start=r.ReadSingle(),end=r.ReadSingle(),visible=r.ReadSingle(),readable=r.ReadSingle(),framing=r.ReadSingle(),stability=r.ReadSingle()};
                if(m.id.Length>100||!m.id.StartsWith(id+":",StringComparison.Ordinal)||!ids.Add(m.id)||m.subject.Length>128||!new[]{"Sighting","Warning","Pursuit","Attack"}.Contains(m.kind)||!float.IsFinite(m.start)||!float.IsFinite(m.end)||m.start<0||m.end>duration+.01f||m.end-m.start<MinimumVisible-.001f||new[]{m.visible,m.readable,m.framing,m.stability}.Any(v=>!float.IsFinite(v)||v<0||v>1))throw new InvalidDataException("Invalid filmed evidence");
                result.Add(m);
            }
            return result;
        }
    }
}
