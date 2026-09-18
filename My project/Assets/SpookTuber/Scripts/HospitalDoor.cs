using UnityEngine;
using UnityEngine.AI;

namespace SpookTuber
{
    public sealed class HospitalDoor : MonoBehaviour
    {
        public Transform leaf;
        public AudioSource sound;
        public NavMeshObstacle obstacle;
        public bool IsOpen {get;private set;}
        public Vector3 slideOffset=new(-2.1f,0,0);
        public float slideSpeed=3.2f;
        public float OpenFraction {get;private set;}
        Vector3 closed;
        bool cleared;
        void Awake(){closed=leaf.localPosition;}
        public bool Use(CrewMotor crew)
        {
            if(!crew||crew.GetComponent<CrewBody>().IsDowned||Vector3.Distance(transform.position,crew.transform.position)>3.5f)return false;
            SetOpen(!IsOpen);return true;
        }
        public void SetOpen(bool value,Surgeon opener=null)
        {
            if(IsOpen==value)return;
            if(!value&&Occupied())return;
            IsOpen=value;if(sound)sound.Play();
            if(!value&&obstacle)obstacle.enabled=true;
            WorldNoise.Emit(transform.position,9,"Sliding door",opener);
            foreach(var enemy in FindObjectsByType<Surgeon>(FindObjectsSortMode.None)){
                enemy.RepathAfterDoorChange();
            }
        }
        void Update()
        {
            if(RunSession.Current&&RunSession.Current.Phase==RunSession.RunPhase.Review)return;
            if(!IsOpen&&OpenFraction>0&&Occupied())SetOpen(true);
            leaf.localPosition=Vector3.MoveTowards(leaf.localPosition,closed+(IsOpen?slideOffset:Vector3.zero),slideSpeed*Time.deltaTime);
            OpenFraction=Vector3.Distance(leaf.localPosition,closed)/Mathf.Max(.001f,slideOffset.magnitude);
            bool passable=OpenFraction>.98f;
            if(passable!=cleared){cleared=passable;if(obstacle)obstacle.enabled=!passable;foreach(var enemy in FindObjectsByType<Surgeon>(FindObjectsSortMode.None))enemy.RepathAfterDoorChange();}
        }
        bool Occupied()
        {
            foreach(var hit in Physics.OverlapBox(transform.TransformPoint(new Vector3(0,1.22f,0)),new Vector3(.94f,1.19f,.3f),transform.rotation,~(1<<9),QueryTriggerInteraction.Ignore)){
                if(hit.transform.IsChildOf(transform))continue;
                if(hit is CharacterController||hit.GetComponentInParent<Surgeon>()||hit.attachedRigidbody&&!hit.attachedRigidbody.isKinematic)return true;
            }
            return false;
        }
    }
}
