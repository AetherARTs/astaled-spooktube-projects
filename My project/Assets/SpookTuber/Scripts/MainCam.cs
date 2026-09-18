using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SpookTuber
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Rigidbody),typeof(CrewTake))]
    public sealed class MainCam : MonoBehaviour
    {
        public string cameraId="MAIN-01";
        public Transform lens;
        public Transform leftGrip,rightGrip;
        public AudioSource impact;
        public Renderer screen;
        public RenderTexture LivePreview {get;private set;}
        public Vector3 heldOffset=new Vector3(.14f,-.18f,.35f);
        public Vector3 aimedOffset=new Vector3(.02f,-.07f,.33f);
        public CrewMotor Holder=>carry&&carry.Equipped?carry.Owner.Motor:null;
        public CrewTake Take {get;private set;}
        Rigidbody physicsBody;
        Collider[] colliders;
        CarryItem carry;
        Camera previewCamera;
        Material screenMaterial;
        float nextPreview;
        float aimBlend;
        int poseFrame=-1;
        void Awake(){
            physicsBody=GetComponent<Rigidbody>();colliders=GetComponentsInChildren<Collider>();Take=GetComponent<CrewTake>();
            carry=GetComponent<CarryItem>();if(!carry)carry=gameObject.AddComponent<CarryItem>();
            carry.kind=CarryItem.Kind.Camera;carry.itemId=cameraId;carry.displayName="MainCam";carry.leftGrip=leftGrip;carry.rightGrip=rightGrip;
            if(screen){
                LivePreview=new RenderTexture(320,180,16);LivePreview.Create();
                previewCamera=lens.gameObject.AddComponent<Camera>();previewCamera.enabled=false;previewCamera.fieldOfView=65;previewCamera.nearClipPlane=.035f;
                previewCamera.cullingMask=~(1<<9);previewCamera.targetTexture=LivePreview;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=RenderSettings.fog?RenderSettings.fogColor:Color.black;
                // The 320x180 equipment monitor does not need a second set of world shadow maps.
                previewCamera.GetUniversalAdditionalCameraData().renderShadows=false;
                screenMaterial=new Material(screen.sharedMaterial);screenMaterial.SetTexture("_BaseMap",LivePreview);
                // The box's rear face maps both UV axes opposite to the LCD viewing direction.
                screenMaterial.SetTextureScale("_BaseMap",new Vector2(-1,-1));screenMaterial.SetTextureOffset("_BaseMap",Vector2.one);screen.sharedMaterial=screenMaterial;
            }
        }
        public bool TryPickup(CrewMotor crew)
        {
            return carry&&carry.TryPickup(crew);
        }
        public void Drop()
        {
            if(carry&&carry.Owner)carry.Owner.Drop(carry);
        }
        void Update(){if(Holder&&Holder.GetComponent<CrewBody>().IsDowned)Drop();}
        void OnCollisionEnter(Collision hit)
        {
            float speed=hit.relativeVelocity.magnitude;if(Holder||speed<1)return;
            if(impact)impact.Play();
            WorldNoise.Emit(transform.position,Mathf.Min(10,2+speed*2),"Camera impact");
        }
        void LateUpdate(){
            UpdateHeldPose();
            if(previewCamera&&(Holder||Take.Recording)&&!Take.Reviewing&&Time.unscaledTime>=nextPreview){previewCamera.Render();nextPreview=Time.unscaledTime+1f/30;}
        }
        void OnDestroy(){if(LivePreview){LivePreview.Release();Destroy(LivePreview);}if(screenMaterial)Destroy(screenMaterial);}
        public void UpdateHeldPose()
        {
            if(!Holder)return;
            if(poseFrame!=Time.frameCount){poseFrame=Time.frameCount;aimBlend=Mathf.MoveTowards(aimBlend,Holder.Aiming?1:0,Time.deltaTime*6);}
            float eased=Mathf.SmoothStep(0,1,aimBlend);
            transform.localPosition=Vector3.Lerp(heldOffset,aimedOffset,eased);
            // Held poses belong to the view, not the interpolated world Rigidbody.
            transform.localRotation=Quaternion.identity;
            var origin=Holder.GetComponent<CrewBody>().headBone.position;
            var delta=lens.position-origin;
            if(delta.sqrMagnitude>0&&Physics.SphereCast(origin,.04f,delta.normalized,out var hit,delta.magnitude,~(1<<8),QueryTriggerInteraction.Ignore))
                transform.position-=delta.normalized*(delta.magnitude-Mathf.Max(0,hit.distance-.01f));
        }
    }
}
