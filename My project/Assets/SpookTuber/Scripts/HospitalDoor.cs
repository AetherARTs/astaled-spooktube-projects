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
        Quaternion closed;
        void Awake(){closed=leaf.localRotation;}
        public bool Use(CrewMotor crew)
        {
            if(!crew||crew.GetComponent<CrewBody>().IsDowned||Vector3.Distance(transform.position,crew.transform.position)>3.5f)return false;
            SetOpen(!IsOpen);return true;
        }
        public void SetOpen(bool value,Surgeon opener=null)
        {
            if(IsOpen==value)return;
            IsOpen=value;if(sound)sound.Play();
            if(obstacle)obstacle.enabled=!value;
            foreach(var enemy in FindObjectsByType<Surgeon>(FindObjectsSortMode.None)){
                if(enemy!=opener)enemy.Hear(transform.position,9);
                enemy.RepathAfterDoorChange();
            }
        }
        void Update()
        {
            if(RunSession.Current&&RunSession.Current.Phase==RunSession.RunPhase.Review)return;
            leaf.localRotation=Quaternion.RotateTowards(leaf.localRotation,closed*Quaternion.Euler(0,IsOpen?-95:0,0),150*Time.deltaTime);
        }
    }
}
