using UnityEngine;

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
        public Vector3 heldOffset=new Vector3(.20f,-.25f,.48f);
        public Vector3 aimedOffset=new Vector3(.02f,-.105f,.37f);
        public CrewMotor Holder {get;private set;}
        public CrewTake Take {get;private set;}
        Rigidbody physicsBody;
        Collider[] colliders;
        Camera previewCamera;
        Material screenMaterial;
        float nextPreview;
        float aimBlend;
        int poseFrame=-1;
        void Awake(){
            physicsBody=GetComponent<Rigidbody>();colliders=GetComponentsInChildren<Collider>();Take=GetComponent<CrewTake>();
            if(screen){
                LivePreview=new RenderTexture(320,180,16);LivePreview.Create();
                previewCamera=lens.gameObject.AddComponent<Camera>();previewCamera.enabled=false;previewCamera.fieldOfView=65;previewCamera.nearClipPlane=.035f;
                previewCamera.cullingMask=~(1<<9);previewCamera.targetTexture=LivePreview;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=RenderSettings.fog?RenderSettings.fogColor:Color.black;
                screenMaterial=new Material(screen.sharedMaterial);screenMaterial.SetTexture("_BaseMap",LivePreview);
                // The box's rear face maps both UV axes opposite to the LCD viewing direction.
                screenMaterial.SetTextureScale("_BaseMap",new Vector2(-1,-1));screenMaterial.SetTextureOffset("_BaseMap",Vector2.one);screen.sharedMaterial=screenMaterial;
            }
        }
        public bool TryPickup(CrewMotor crew)
        {
            if(!crew || Holder || crew.GetComponent<CrewBody>().IsDowned)return false;
            var origin=crew.GetComponent<CrewBody>().headBone.position;
            if(Vector3.Distance(origin,transform.position)>2.2f)return false;
            var direction=transform.position-origin;
            if(Physics.Raycast(origin,direction.normalized,out var hit,direction.magnitude,~(1<<8),QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<MainCam>()!=this)return false;
            Holder=crew;physicsBody.interpolation=RigidbodyInterpolation.None;physicsBody.isKinematic=true;
            foreach(var c in colliders)c.enabled=false;
            transform.SetParent(crew.viewCamera.transform,false);
            transform.localPosition=heldOffset;transform.localRotation=Quaternion.identity;
            foreach(var t in GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            crew.firstPerson=true;return true;
        }
        public void Drop()
        {
            if(!Holder)return;
            var forward=Holder.transform.forward;
            // The drop point is beside the crew, not at a third-person camera position.
            var origin=Holder.GetComponent<CrewBody>().headBone.position;
            var next=origin+forward*.48f;
            if(Physics.SphereCast(origin,.18f,forward,out var hit,.48f,~(1<<8),QueryTriggerInteraction.Ignore))next=origin+forward*Mathf.Max(0,hit.distance-.02f);
            transform.SetParent(null,true);transform.position=next;Holder=null;
            foreach(var t in GetComponentsInChildren<Transform>())t.gameObject.layer=0;
            foreach(var c in colliders)c.enabled=true;
            Physics.SyncTransforms();physicsBody.isKinematic=false;physicsBody.interpolation=RigidbodyInterpolation.Interpolate;physicsBody.linearVelocity=forward*.4f;
        }
        void Update(){if(Holder&&Holder.GetComponent<CrewBody>().IsDowned)Drop();}
        void OnCollisionEnter(Collision hit)
        {
            float speed=hit.relativeVelocity.magnitude;if(Holder||speed<1)return;
            if(impact)impact.Play();
            foreach(var enemy in FindObjectsByType<Surgeon>(FindObjectsSortMode.None))enemy.Hear(transform.position,Mathf.Min(10,2+speed*2));
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
