using UnityEngine;

namespace SpookTuber
{
    public sealed class HomeNpc : MonoBehaviour
    {
        public enum Role { Bobby, Supply }
        public Role role;
        public string displayName="Bobby";
        public Transform head,leftArm,rightArm;
        Quaternion headRest,leftRest,rightRest;
        CrewMotor crew;
        void Start(){crew=FindFirstObjectByType<CrewMotor>();if(head)headRest=head.localRotation;if(leftArm)leftRest=leftArm.localRotation;if(rightArm)rightRest=rightArm.localRotation;}
        public bool Use(CrewMotor user)
        {
            if(!user||user.GetComponent<CrewBody>().IsDowned||Vector3.Distance(user.transform.position,transform.position)>3.5f)return false;
            if(role==Role.Bobby)return RunSession.Current&&RunSession.Current.OpenStudio();
            user.GetComponent<CrewPhone>().Open("Supply");return true;
        }
        void LateUpdate()
        {
            if(!crew)return;
            var direction=crew.transform.position-transform.position;float turn=direction.magnitude<5?Mathf.Clamp(Vector3.SignedAngle(transform.forward,direction,Vector3.up),-35,35):Mathf.Sin(Time.time*.4f)*9;
            if(head)head.localRotation=Quaternion.Slerp(head.localRotation,headRest*Quaternion.Euler(0,turn,Mathf.Sin(Time.time*.7f)*2),Time.deltaTime*3);
            if(leftArm)leftArm.localRotation=leftRest*Quaternion.Euler(Mathf.Sin(Time.time*1.2f)*4,0,0);
            if(rightArm)rightArm.localRotation=rightRest*Quaternion.Euler(Mathf.Sin(Time.time*1.2f+1)*4,0,0);
        }
    }
}
