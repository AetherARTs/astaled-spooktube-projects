using UnityEngine;

namespace SpookTuber
{
    public sealed class GravityGlove : MonoBehaviour
    {
        public float range=4.5f,maxMass=45,energy=100;
        public Rigidbody Held {get;private set;}
        public string Notice {get;private set;}="";
        public float Distance {get;private set;}=2;
        CrewMotor motor;
        CrewInventory inventory;
        Vector3 localPoint;
        float cooldown;
        public bool Equipped=>inventory&&inventory.Active&&inventory.Active.kind==CarryItem.Kind.GravityGlove&&!inventory.Suspended;
        void Awake(){motor=GetComponent<CrewMotor>();inventory=GetComponent<CrewInventory>();}
        public bool Grab()
        {
            if(!Equipped||energy<5||motor.GetComponent<CrewBody>().IsDowned)return false;
            var view=motor.viewCamera.transform;
            if(!Physics.Raycast(view.position,view.forward,out var hit,range,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))return false;
            var prop=hit.collider.GetComponentInParent<PhysicsProp>();
            if(!prop||!prop.Body||prop.Body.isKinematic||prop.Body.mass>Mathf.Min(maxMass,prop.MassLimit)){Notice="That object is anchored or too heavy";return false;}
            Held=prop.Body;localPoint=Held.transform.InverseTransformPoint(hit.point);Distance=Mathf.Clamp(hit.distance,.9f,range);
            Held.WakeUp();Notice="LMB hold / wheel distance / RMB push";return true;
        }
        public void Release(){Held=null;}
        public void AdjustDistance(float amount){Distance=Mathf.Clamp(Distance+amount,.9f,range);}
        public bool Push()
        {
            if(!Equipped||energy<12||Time.time<cooldown)return false;
            if(!Held&&!Grab())return false;
            var target=Held;Release();target.AddForce(motor.viewCamera.transform.forward*Mathf.Min(22,target.mass*4),ForceMode.Impulse);
            energy-=12;cooldown=Time.time+.5f;WorldNoise.Emit(target.position,5,"Gravity pulse");return true;
        }
        void FixedUpdate()
        {
            if(!Held){energy=Mathf.Min(100,energy+Time.fixedDeltaTime*7);return;}
            if(!Equipped||motor.InputBlocked||energy<=0){Release();return;}
            var view=motor.viewCamera.transform;var point=Held.transform.TransformPoint(localPoint);var delta=point-view.position;
            if(delta.magnitude>range+1){Release();return;}
            if(Physics.Raycast(view.position,delta.normalized,out var hit,delta.magnitude-.04f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)&&hit.rigidbody!=Held){Notice="Signal blocked";Release();return;}
            var desired=view.position+view.forward*Distance;
            var force=(desired-point)*65-Held.GetPointVelocity(point)*14;
            Held.AddForceAtPosition(Vector3.ClampMagnitude(force,Mathf.Min(350,80+Held.mass*10)),point,ForceMode.Force);
            energy=Mathf.Max(0,energy-Time.fixedDeltaTime*(1.5f+Held.mass*.18f));
        }
        void OnDisable(){Release();}
    }
}
