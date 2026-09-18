using UnityEngine;
using UnityEngine.InputSystem;

namespace SpookTuber
{
    [RequireComponent(typeof(CharacterController),typeof(CrewBody))]
    public sealed class CrewMotor : MonoBehaviour
    {
        public Camera viewCamera;
        public Light shoulderLight;
        public MainCam mainCam;
        public AudioSource footsteps;
        public float walkSpeed=2.2f, sprintSpeed=4.3f, jumpHeight=.65f;
        public float mouseSensitivity=.10f, stickSensitivity=125;
        public bool firstPerson=true;
        public UnityEngine.UI.Text hint, status;
        CharacterController motor;
        CrewBody body;
        InputActionMap inputs;
        InputAction move,look,sprint,jump,interact,lightToggle,pause,view,record,drop,review,scrub;
        float pitch=12,yaw,vertical;
        bool locked=true;
        float stepDistance;
        void Awake()
        {
            motor=GetComponent<CharacterController>();body=GetComponent<CrewBody>();
            inputs=new InputActionMap("Crew");
            move=inputs.AddAction("Move",InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            look=inputs.AddAction("Look",InputActionType.Value);
            look.AddBinding("<Mouse>/delta");look.AddBinding("<Gamepad>/rightStick");
            sprint=inputs.AddAction("Sprint",InputActionType.Button,"<Keyboard>/leftShift");sprint.AddBinding("<Gamepad>/leftStickPress");
            jump=inputs.AddAction("Jump",InputActionType.Button,"<Keyboard>/space");jump.AddBinding("<Gamepad>/buttonSouth");
            interact=inputs.AddAction("Interact",InputActionType.Button,"<Keyboard>/e");interact.AddBinding("<Gamepad>/buttonWest");
            lightToggle=inputs.AddAction("Light",InputActionType.Button,"<Keyboard>/f");lightToggle.AddBinding("<Gamepad>/dpad/up");
            pause=inputs.AddAction("Cursor",InputActionType.Button,"<Keyboard>/escape");pause.AddBinding("<Gamepad>/start");
            view=inputs.AddAction("View",InputActionType.Button,"<Keyboard>/v");view.AddBinding("<Gamepad>/rightStickPress");
            record=inputs.AddAction("Record",InputActionType.Button,"<Keyboard>/r");record.AddBinding("<Gamepad>/rightTrigger");
            drop=inputs.AddAction("Drop",InputActionType.Button,"<Keyboard>/q");drop.AddBinding("<Gamepad>/buttonEast");
            review=inputs.AddAction("Review",InputActionType.Button,"<Keyboard>/p");review.AddBinding("<Gamepad>/select");
            scrub=inputs.AddAction("Scrub",InputActionType.Value);
            scrub.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/leftArrow").With("Positive","<Keyboard>/rightArrow");
            scrub.AddBinding("<Gamepad>/dpad/x");
            yaw=transform.eulerAngles.y;
        }
        void OnEnable(){inputs?.Enable();}
        void Start(){SetCursor(true);}
        void OnDisable(){inputs?.Disable();SetCursor(false);}
        void OnDestroy(){inputs?.Dispose();}
        void SetCursor(bool value){locked=value;Cursor.lockState=value?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!value;}
        void Update()
        {
            if(RunSession.Current&&RunSession.Current.Studio.IsOpen){body.animator.SetFloat("Speed",0);return;}
            if(pause.WasPressedThisFrame())SetCursor(!locked);
            var take=mainCam?mainCam.Take:null;
            var session=RunSession.Current;
            if(session&&session.IsBusy)return;
            if(take&&review.WasPressedThisFrame()&&!body.IsDowned&&(!session||session.CanReview)){
                if(take.Reviewing)take.EndReview();
                else if(session&&session.Phase==RunSession.RunPhase.House&&take.FrameCount==0&&!string.IsNullOrEmpty(session.LastTake))session.ReviewLastMission();
                else take.BeginReview();
            }
            if(take&&take.Reviewing){
                body.animator.SetFloat("Speed",0);
                if(jump.WasPressedThisFrame())take.Paused=!take.Paused;
                float amount=scrub.ReadValue<float>();if(Mathf.Abs(amount)>.1f){take.Paused=true;take.Seek(take.Playhead+amount*3*Time.unscaledDeltaTime);}
                if(hint)hint.text="";return;
            }
            if(!locked)return;
            bool holding=mainCam&&mainCam.Holder==this;
            if(view.WasPressedThisFrame()&&!holding)firstPerson=!firstPerson;
            if(holding&&!body.IsDowned){
                if(record.WasPressedThisFrame()){if(take.Recording)take.StopRecording();else if(take.Unsaved)take.SaveTake();else take.StartRecording();}
                if(drop.WasPressedThisFrame()){mainCam.Drop();holding=false;}
            }
            if(lightToggle.WasPressedThisFrame())shoulderLight.enabled=!shoulderLight.enabled;
            var delta=look.ReadValue<Vector2>();
            float sensitivity=look.activeControl?.device is Gamepad?stickSensitivity*Time.deltaTime:mouseSensitivity;
            yaw+=delta.x*sensitivity;pitch=Mathf.Clamp(pitch-delta.y*sensitivity,-65,75);
            var input=Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1);
            var direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            if(body.IsDowned) {
                body.MoveHead(direction*1.5f,Time.deltaTime);
                if(hint)hint.text="BODY OFFLINE  /  stay close to your crew";
                if(status)status.text=$"HEAD ENERGY   {Mathf.CeilToInt(body.Energy)}\n"+(session?session.Notice:"");
                return;
            }
            transform.rotation=Quaternion.Euler(0,yaw,0);
            if(motor.isGrounded && vertical<0)vertical=-2;
            if(motor.isGrounded && jump.WasPressedThisFrame())vertical=Mathf.Sqrt(jumpHeight*19.62f);
            vertical=Mathf.Max(vertical-9.81f*Time.deltaTime,-25);
            var speed=sprint.IsPressed()?sprintSpeed:walkSpeed;
            var previous=transform.position;
            motor.Move((direction*speed+Vector3.up*vertical)*Time.deltaTime);
            var travelled=transform.position-previous;travelled.y=0;
            if(motor.isGrounded)stepDistance+=travelled.magnitude;
            if(stepDistance>.8f){
                stepDistance=0;if(footsteps)footsteps.Play();
                foreach(var enemy in FindObjectsByType<Surgeon>(FindObjectsSortMode.None))enemy.Hear(transform.position,sprint.IsPressed()?9:3.5f);
            }
            body.animator.SetFloat("Speed",input.magnitude*(sprint.IsPressed()?2:1),.12f,Time.deltaTime);
            if(transform.position.y < -5) {
                motor.enabled=false;transform.position=new Vector3(0,.1f,0);motor.enabled=true;vertical=0;
            }
            if(hint)hint.text="";
            if(Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,3.5f,~(1<<8),QueryTriggerInteraction.Collide)) {
                var station=hit.collider.GetComponentInParent<CrewWardrobe>();
                var door=hit.collider.GetComponentInParent<HospitalDoor>();
                if(door){if(hint)hint.text="E / "+(door.IsOpen?"Close door":"Open door");if(interact.WasPressedThisFrame())door.Use(this);}
                var gate=hit.collider.GetComponentInParent<MissionGate>();
                if(gate){if(hint)hint.text=gate.Prompt;if(interact.WasPressedThisFrame())gate.Use(this);}
                if(station) {
                    if(hint)hint.text="E   /   "+(body.WearsHoodie?"Store hoodie":"Wear crew hoodie");
                    if(interact.WasPressedThisFrame())body.SetOutfit(!body.WearsHoodie);
                }
                var cameraItem=hit.collider.GetComponentInParent<MainCam>();
                if(cameraItem&&!holding&&Vector3.Distance(body.headBone.position,cameraItem.transform.position)<=2.2f){
                    if(hint)hint.text="E   /   Pick up MainCam";
                    if(interact.WasPressedThisFrame())cameraItem.TryPickup(this);
                }
            }
            if(holding&&hint&&string.IsNullOrEmpty(hint.text))hint.text="R  REC / STOP     Q  PUT DOWN"+(!session||session.CanReview?"     P  REVIEW TAKE":"");
            if(status)status.text=take&&take.Recording?$"REC  /  {take.Duration:00.0} / 60s  /  MAIN-01":take?take.Message:"CREW 01     /     "+(body.WearsHoodie?"OFF DUTY":"CHASSIS");
            if(status&&session)status.text+="\n"+session.Notice;
        }
        void LateUpdate(){UpdateView();}
        public void UpdateView()
        {
            if(!viewCamera||!body)return;
            var center=body.ViewTarget.position;
            var rotation=Quaternion.Euler(pitch,yaw,0);
            var target=firstPerson&&!body.IsDowned?center+Vector3.up*.13f+transform.forward*.12f:center+Vector3.up*.12f;
            var desired=firstPerson&&!body.IsDowned?target:target-rotation*Vector3.forward*2.6f+rotation*Vector3.right*.35f;
            if(firstPerson&&!body.IsDowned&&Physics.Linecast(center,desired,out var nearHit,~(1<<8),QueryTriggerInteraction.Ignore))desired=nearHit.point+nearHit.normal*.05f;
            if(!firstPerson && Physics.Linecast(target,desired,out var hit,~(1<<8),QueryTriggerInteraction.Ignore))desired=hit.point+hit.normal*.12f;
            viewCamera.transform.SetPositionAndRotation(desired,rotation);
            body.head.shadowCastingMode=firstPerson&&!body.IsDowned?UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly:UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}

