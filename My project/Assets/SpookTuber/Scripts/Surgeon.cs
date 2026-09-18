using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SpookTuber
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Surgeon : MonoBehaviour
    {
        public enum Behaviour { Patrol, Interested, Warning, Hunt, Search, Cooldown, Attack }
        public Behaviour State {get;private set;}
        public Vector3 LastKnownPosition {get;private set;}
        public Transform[] patrol=System.Array.Empty<Transform>();
        public Transform eye,visual;
        public AudioSource warning,blade,footstep;
        public float sightRange=10,warningSeconds=1.15f,searchSeconds=5;
        public bool SeesCrew {get;private set;}
        CrewBody crew;
        NavMeshAgent agent;
        float stateAt,lastSeen,doorAt,stepAt,repathAt;
        Vector3 requestedDestination;
        int patrolIndex;
        HospitalDoor blockedDoor;
        readonly Dictionary<string,Transform> bones=new();
        readonly Dictionary<string,Quaternion> rest=new();
        void Awake()
        {
            agent=GetComponent<NavMeshAgent>();
            foreach(var t in visual.GetComponentsInChildren<Transform>()){bones[t.name]=t;rest[t.name]=t.localRotation;}
            if(RunSession.Current&&RunSession.Current.Phase==RunSession.RunPhase.Review){agent.enabled=false;enabled=false;}
        }
        void Start(){var motor=FindFirstObjectByType<CrewMotor>();if(motor)crew=motor.GetComponent<CrewBody>();Change(Behaviour.Patrol);}
        void OnDisable(){if(agent&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;}
        void Change(Behaviour value)
        {
            State=value;stateAt=Time.time;
            if(!agent.isOnNavMesh)return;
            agent.isStopped=value==Behaviour.Warning||value==Behaviour.Attack||value==Behaviour.Cooldown;
            agent.speed=value==Behaviour.Hunt?2.75f:1.15f;
            if(value==Behaviour.Warning&&warning)warning.Play();
            if(value==Behaviour.Attack&&blade)blade.Play();
            if(value==Behaviour.Patrol&&patrol.Length>0&&patrol[patrolIndex%patrol.Length])MoveTo(patrol[patrolIndex%patrol.Length].position);
            if(value==Behaviour.Search||value==Behaviour.Interested||value==Behaviour.Hunt)MoveTo(LastKnownPosition);
        }
        void MoveTo(Vector3 point){requestedDestination=point;agent.SetDestination(point);}
        public void RepathAfterDoorChange(){repathAt=Time.time+.25f;}
        public bool CanSee(CrewBody target)
        {
            if(!target||target.IsDowned)return false;
            var delta=target.headBone.position+Vector3.up*.1f-eye.position;
            if(delta.sqrMagnitude>sightRange*sightRange||Vector3.Angle(transform.forward,delta)>58)return false;
            return !Physics.Linecast(eye.position,eye.position+delta,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
        }
        public void Hear(Vector3 point,float radius)
        {
            if(!enabled||State==Behaviour.Hunt||State==Behaviour.Attack||State==Behaviour.Warning)return;
            float distance=Vector3.Distance(point,eye.position);
            if(Physics.Linecast(eye.position,point+Vector3.up*.2f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))radius*=.45f;
            if(distance>radius)return;
            LastKnownPosition=point;Change(Behaviour.Interested);
        }
        void Update()
        {
            if(!crew||!agent.isOnNavMesh)return;
            if(repathAt>0&&Time.time>=repathAt){repathAt=0;if(!agent.isStopped)agent.SetDestination(requestedDestination);}
            if(crew.IsDowned){if(State!=Behaviour.Cooldown)Change(Behaviour.Cooldown);return;}
            SeesCrew=CanSee(crew);
            if(SeesCrew){
                // LOS and pursuit must use one observed pose, including during Rigidbody interpolation.
                var observed=crew.headBone.position;observed.y=crew.transform.position.y;
                LastKnownPosition=observed;lastSeen=Time.time;
            }
            float elapsed=Time.time-stateAt;
            if(SeesCrew&&State!=Behaviour.Hunt&&State!=Behaviour.Warning&&State!=Behaviour.Attack&&State!=Behaviour.Cooldown)Change(Behaviour.Warning);
            elapsed=Time.time-stateAt;
            switch(State){
                case Behaviour.Patrol:
                    if(!agent.pathPending&&agent.remainingDistance<.5f&&elapsed>2){patrolIndex++;Change(Behaviour.Patrol);}break;
                case Behaviour.Interested:
                    if(elapsed>6||(!agent.pathPending&&agent.remainingDistance<.6f))Change(Behaviour.Search);break;
                case Behaviour.Warning:
                    Face(LastKnownPosition);
                    if(elapsed>warningSeconds)Change(SeesCrew?Behaviour.Hunt:Behaviour.Search);break;
                case Behaviour.Hunt:
                    if(SeesCrew)MoveTo(LastKnownPosition);
                    if(SeesCrew&&Vector3.Distance(transform.position,crew.transform.position)<1.28f)Change(Behaviour.Attack);
                    else if(Time.time-lastSeen>1.5f)Change(Behaviour.Search);break;
                case Behaviour.Attack:
                    Face(LastKnownPosition);
                    if(elapsed>.72f){
                        if(CanSee(crew)&&Vector3.Distance(transform.position,crew.transform.position)<1.4f)crew.KnockDown();
                        Change(Behaviour.Cooldown);
                    }break;
                case Behaviour.Search:
                    if(elapsed>searchSeconds)Change(Behaviour.Cooldown);break;
                case Behaviour.Cooldown:
                    if(elapsed>2)Change(Behaviour.Patrol);break;
            }
            if(agent.velocity.magnitude>.2f&&Time.time>stepAt){if(footstep)footstep.Play();stepAt=Time.time+(State==Behaviour.Hunt?.38f:.7f);}
            // A closed door buys time; the Surgeon must reach it and visibly force it open.
            if(!agent.isStopped&&agent.hasPath&&agent.pathStatus==NavMeshPathStatus.PathPartial&&agent.remainingDistance<1.5f){
                if(!blockedDoor){
                    float nearest=2.5f;
                    foreach(var door in FindObjectsByType<HospitalDoor>(FindObjectsSortMode.None)){
                        float d=Vector3.Distance(transform.position,door.transform.position);
                        if(!door.IsOpen&&d<nearest){blockedDoor=door;nearest=d;doorAt=Time.time;if(warning)warning.Play();}
                    }
                }else if(Time.time-doorAt>1.6f){blockedDoor.SetOpen(true,this);blockedDoor=null;}
            }else blockedDoor=null;
        }
        void Face(Vector3 point)
        {
            var delta=point-transform.position;delta.y=0;
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),100*Time.deltaTime);
        }
        void Pose(string name,float degrees)
        {
            if(!bones.TryGetValue(name,out var bone))return;
            var axis=bone.parent.InverseTransformDirection(transform.right);
            bone.localRotation=Quaternion.AngleAxis(degrees,axis)*rest[name];
        }
        void LateUpdate()
        {
            if(!agent||!agent.enabled)return;
            float moving=Mathf.Clamp01(agent.velocity.magnitude),wave=Mathf.Sin(Time.time*(State==Behaviour.Hunt?9:4.5f))*moving;
            Pose("LThigh",wave*22);Pose("RThigh",-wave*22);
            Pose("LShin",Mathf.Max(0,-wave)*24);Pose("RShin",Mathf.Max(0,wave)*24);
            Pose("LUpperArm",-wave*13);Pose("RUpperArm",State==Behaviour.Attack?-100+Mathf.Clamp01((Time.time-stateAt)/.72f)*150:wave*13);
            Pose("RForearm",State==Behaviour.Warning?-45:-12);Pose("LForearm",-9);
            Pose("Head",Mathf.Sin(Time.time*.6f)*5);Pose("Spine",State==Behaviour.Hunt?12:3);
        }
    }
}
