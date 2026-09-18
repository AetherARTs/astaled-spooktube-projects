using UnityEngine;
namespace SpookTuber
{
    public sealed class CrewSecondaryMotion : MonoBehaviour
    {
        public Transform[] pendants;
        [Range(1,20)] public float stiffness=8;
        [Range(.1f,3)] public float damping=.9f;
        public float maximumAngle=25;
        Quaternion[] rest;
        Vector2[] angle,velocity;
        Vector3 previousPosition,previousVelocity;
        CrewBody body;
        void Awake(){
            body=GetComponent<CrewBody>();rest=new Quaternion[pendants.Length];
            angle=new Vector2[pendants.Length];velocity=new Vector2[pendants.Length];
            for(int i=0;i<pendants.Length;i++)rest[i]=pendants[i].localRotation;
            previousPosition=transform.position;
        }
        void LateUpdate(){
            float dt=Mathf.Min(Time.deltaTime,.05f);
            if(dt<=0)return;
            var speed=(transform.position-previousPosition)/dt;
            var acceleration=transform.InverseTransformDirection((speed-previousVelocity)/dt);
            var target=Vector2.ClampMagnitude(new Vector2(-acceleration.z,acceleration.x)*1.3f,maximumAngle);
            if(body.IsDowned)target=Vector2.zero;
            for(int i=0;i<pendants.Length;i++){
                float h=dt/4;
                for(int step=0;step<4;step++){
                    velocity[i]+=(stiffness*stiffness*(target-angle[i])-2*damping*stiffness*velocity[i])*h;
                    angle[i]=Vector2.ClampMagnitude(angle[i]+velocity[i]*h,maximumAngle);
                }
                pendants[i].localRotation=rest[i]*Quaternion.Euler(angle[i].x,0,angle[i].y);
            }
            previousPosition=transform.position;previousVelocity=speed;
        }
    }
}

