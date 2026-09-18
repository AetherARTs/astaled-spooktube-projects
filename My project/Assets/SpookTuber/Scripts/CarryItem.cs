using UnityEngine;

namespace SpookTuber
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarryItem : MonoBehaviour
    {
        public enum Kind { Camera, GravityGlove, ProductionLight, Noisemaker }
        public Kind kind;
        public string itemId="",displayName="Equipment";
        public Transform leftGrip,rightGrip;
        public Vector3 heldOffset=new(.20f,-.23f,.43f);
        public Light workLight;
        public CrewInventory Owner {get;private set;}
        public bool Equipped=>Owner&&Owner.Active==this&&!Owner.Suspended;
        Rigidbody physicsBody;
        Collider[] shapes;
        Renderer[] visuals;
        void Awake(){physicsBody=GetComponent<Rigidbody>();shapes=GetComponentsInChildren<Collider>();visuals=GetComponentsInChildren<Renderer>();}
        public bool TryPickup(CrewMotor crew)=>crew&&crew.GetComponent<CrewInventory>().TryPickup(this);
        internal void Claim(CrewInventory owner)
        {
            Owner=owner;physicsBody.interpolation=RigidbodyInterpolation.None;physicsBody.isKinematic=true;
            foreach(var shape in shapes)shape.enabled=false;
            transform.SetParent(owner.Motor.viewCamera.transform,false);transform.localScale=Vector3.one;
            foreach(var node in GetComponentsInChildren<Transform>(true))node.gameObject.layer=8;
        }
        internal void Present(bool visible)
        {
            foreach(var visual in visuals)if(visual)visual.enabled=visible;
            if(workLight)workLight.enabled=visible;
            // Stowed transforms remain stable for the recorder; they are never destroyed.
            if(!visible){transform.localPosition=new Vector3(0,-.6f,-.25f);transform.localRotation=Quaternion.identity;}
        }
        internal void Release(Vector3 position,Vector3 velocity)
        {
            if(workLight)workLight.enabled=false;
            transform.SetParent(null,true);transform.SetPositionAndRotation(position,Owner.Motor.viewCamera.transform.rotation);Owner=null;
            foreach(var node in GetComponentsInChildren<Transform>(true))node.gameObject.layer=0;
            foreach(var visual in visuals)if(visual)visual.enabled=true;
            foreach(var shape in shapes)shape.enabled=true;
            Physics.SyncTransforms();physicsBody.isKinematic=false;physicsBody.interpolation=RigidbodyInterpolation.Interpolate;
            physicsBody.linearVelocity=velocity;
        }
        public void Use()
        {
            if(!Equipped)return;
            if(kind==Kind.ProductionLight&&workLight)workLight.enabled=!workLight.enabled;
            if(kind==Kind.Noisemaker){var audio=GetComponent<AudioSource>();if(audio)audio.Play();WorldNoise.Emit(transform.position,18,"Noisemaker");}
        }
    }
}
