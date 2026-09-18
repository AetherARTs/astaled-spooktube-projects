using UnityEngine;

namespace SpookTuber
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsProp : MonoBehaviour
    {
        public enum WeightClass { Light, Medium, Heavy }
        public WeightClass weight;
        public bool mantle;
        public AudioSource impact;
        public float noiseScale=1;
        public Rigidbody Body {get;private set;}
        public float MassLimit=>weight==WeightClass.Light?5:weight==WeightClass.Medium?18:45;
        float nextImpact;
        void Awake(){Body=GetComponent<Rigidbody>();}
        void OnCollisionEnter(Collision collision)
        {
            float speed=collision.relativeVelocity.magnitude;
            if(speed<.75f||Time.time<nextImpact)return;nextImpact=Time.time+.22f;
            if(impact){impact.volume=Mathf.Clamp01(speed*.12f);impact.Play();}
            WorldNoise.Emit(transform.position,Mathf.Clamp(speed*Mathf.Sqrt(Body.mass)*noiseScale,2,24),"Impact");
        }
    }
}
