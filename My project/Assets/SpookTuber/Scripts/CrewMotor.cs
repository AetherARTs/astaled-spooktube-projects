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
        public float walkSpeed=2.65f,sprintSpeed=5.25f,jumpHeight=.95f;
        public float mouseSensitivity=.10f,stickSensitivity=125;
        public bool firstPerson=true;
        public UnityEngine.UI.Text hint,status;
        public Vector3 PlanarVelocity {get;private set;}
        public float CrouchAmount {get;private set;}
        public bool Sliding=>slideTime>0;
        public bool Mantling=>mantleTime>0;
        public float Lean {get;private set;}
        public bool InputBlocked=>GameShell.Active||!locked||body.IsDowned||phone&&phone.IsOpen||RunSession.Current&&(RunSession.Current.IsBusy||RunSession.Current.Studio.IsOpen)||mainCam&&mainCam.Take.Reviewing;
        public bool Aiming=>!InputBlocked&&aim!=null&&aim.IsPressed();
        CharacterController motor;
        CrewBody body;
        CrewInventory inventory;
        GravityGlove glove;
        CrewPhone phone;
        InputActionMap inputs;
        InputAction move,look,sprint,jump,interact,lightToggle,pause,view,record,drop,review,scrub,aim,crouch,slide,lean,phoneToggle,use,slots,wheel;
        float pitch=10,yaw,vertical,stepDistance,standingHeight,slideTime,lastGrounded=-1,jumpUntil=-1,mantleTime;
        Vector3 slideDirection,mantleStart,mantleEnd;
        bool locked=true,wasGrounded;
        const int WorldMask=~((1<<8)|(1<<9));
        void Awake()
        {
            if(shoulderLight)shoulderLight.cullingMask&=~((1<<8)|(1<<9));
            motor=GetComponent<CharacterController>();body=GetComponent<CrewBody>();standingHeight=motor.height;
            inventory=GetComponent<CrewInventory>();if(!inventory)inventory=gameObject.AddComponent<CrewInventory>();
            glove=GetComponent<GravityGlove>();if(!glove)glove=gameObject.AddComponent<GravityGlove>();
            if(!GetComponent<CrewVoice>())gameObject.AddComponent<CrewVoice>();
            phone=GetComponent<CrewPhone>();if(!phone)phone=gameObject.AddComponent<CrewPhone>();
            inputs=new InputActionMap("Crew");
            move=inputs.AddAction("Move",InputActionType.Value);move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");move.AddBinding("<Gamepad>/leftStick");
            look=inputs.AddAction("Look",InputActionType.Value);look.AddBinding("<Mouse>/delta");look.AddBinding("<Gamepad>/rightStick");
            sprint=Button("Sprint","<Keyboard>/leftShift","<Gamepad>/leftStickPress");
            jump=Button("Jump","<Keyboard>/space","<Gamepad>/buttonSouth");interact=Button("Interact","<Keyboard>/e","<Gamepad>/buttonWest");
            lightToggle=Button("Light","<Keyboard>/f","<Gamepad>/dpad/up");pause=Button("Cursor","<Keyboard>/escape","<Gamepad>/start");
            view=Button("View","<Keyboard>/v","<Gamepad>/rightStickPress");record=Button("Record","<Keyboard>/r","<Gamepad>/rightTrigger");
            drop=Button("Drop","<Keyboard>/q");review=Button("Review","<Keyboard>/p","<Gamepad>/select");
            aim=Button("Aim / push","<Mouse>/rightButton","<Gamepad>/leftTrigger");use=Button("Use / grab","<Mouse>/leftButton","<Gamepad>/rightShoulder");
            crouch=Button("Crouch","<Keyboard>/leftCtrl","<Gamepad>/buttonEast");slide=Button("Slide","<Keyboard>/c");
            phoneToggle=Button("Phone","<Keyboard>/tab","<Gamepad>/dpad/down");
            lean=inputs.AddAction("Lean",InputActionType.Value);lean.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/z").With("Positive","<Keyboard>/x");
            slots=inputs.AddAction("Next item",InputActionType.Button,"<Gamepad>/leftShoulder");wheel=inputs.AddAction("Wheel",InputActionType.Value,"<Mouse>/scroll/y");
            scrub=inputs.AddAction("Scrub",InputActionType.Value);scrub.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/leftArrow").With("Positive","<Keyboard>/rightArrow");scrub.AddBinding("<Gamepad>/dpad/x");
            yaw=transform.eulerAngles.y;mouseSensitivity=PlayerPrefs.GetFloat("LookSensitivity",mouseSensitivity);
        }
        InputAction Button(string name,string key,string pad=null){var action=inputs.AddAction(name,InputActionType.Button,key);if(pad!=null)action.AddBinding(pad);return action;}
        void OnEnable(){inputs?.Enable();}
        void Start(){SetCursor(true);GameSettings.Apply();}
        void OnDisable(){inputs?.Disable();SetCursor(false);}
        void OnDestroy(){inputs?.Dispose();}
        public void SetCursor(bool value){locked=value;Cursor.lockState=value?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!value;}
        void Update()
        {
            if(GameShell.Active||GameUi.InputConsumed){Idle();return;}
            var session=RunSession.Current;var take=mainCam?mainCam.Take:null;
            if(session&&(session.Studio.IsOpen||session.IsBusy)){Idle();return;}
            if(phone.IsOpen){Idle();if(phoneToggle.WasPressedThisFrame())phone.Close();return;}
            if(pause.WasPressedThisFrame()){if(take&&take.Reviewing)take.EndReview();else GameShell.Pause(this);Idle();return;}
            if(phoneToggle.WasPressedThisFrame()&&!body.IsDowned&&(!take||!take.Reviewing)){phone.Open();Idle();return;}
            if(take&&review.WasPressedThisFrame()&&!body.IsDowned&&(!session||session.CanReview)){
                if(take.Reviewing)take.EndReview();else if(session&&session.Phase==RunSession.RunPhase.House&&take.FrameCount==0&&!string.IsNullOrEmpty(session.LastTake))session.ReviewLastMission();else take.BeginReview();
            }
            if(take&&take.Reviewing){Idle();if(jump.WasPressedThisFrame())take.Paused=!take.Paused;float amount=scrub.ReadValue<float>();if(Mathf.Abs(amount)>.1f){take.Paused=true;take.Seek(take.Playhead+amount*3*Time.unscaledDeltaTime);}return;}
            if(!locked){Idle();return;}
            var delta=look.ReadValue<Vector2>();float sensitivity=look.activeControl?.device is Gamepad?stickSensitivity*Time.deltaTime:mouseSensitivity;
            yaw+=delta.x*sensitivity;pitch=Mathf.Clamp(pitch-delta.y*sensitivity,-78,80);
            var input=Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1);var direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            if(body.IsDowned){glove.Release();body.MoveHead(direction*1.5f,Time.deltaTime);if(hint)hint.text="BODY OFFLINE / return to your body";if(status)status.text=$"HEAD ENERGY {body.Energy:0}\n{session?.Notice}";return;}
            transform.rotation=Quaternion.Euler(0,yaw,0);
            if(lightToggle.WasPressedThisFrame()&&shoulderLight)shoulderLight.enabled=!shoulderLight.enabled;
            if(view.WasPressedThisFrame()&&!inventory.Active)firstPerson=!firstPerson;
            var keyboard=Keyboard.current;
            if(keyboard!=null){if(keyboard.digit1Key.wasPressedThisFrame)inventory.Select(0);if(keyboard.digit2Key.wasPressedThisFrame)inventory.Select(1);if(keyboard.digit3Key.wasPressedThisFrame)inventory.Select(2);}
            float scroll=wheel.ReadValue<float>();if(glove.Held){if(scroll!=0)glove.AdjustDistance(Mathf.Sign(scroll)*.2f);}else if(scroll!=0||slots.WasPressedThisFrame())inventory.Select((inventory.Selected+(scroll>0?2:1))%3);
            if(drop.WasPressedThisFrame())inventory.Drop();
            if(glove.Equipped){if(use.WasPressedThisFrame())glove.Grab();if(!use.IsPressed())glove.Release();if(aim.WasPressedThisFrame())glove.Push();}
            else if(use.WasPressedThisFrame()&&inventory.Active)inventory.Active.Use();
            bool holding=mainCam&&mainCam.Holder==this;
            if(holding&&record.WasPressedThisFrame()){if(take.Recording)take.StopRecording();else if(take.Unsaved)take.SaveTake();else take.StartRecording();}
            Move(direction,input.magnitude);UpdateView();Interact();
            if(holding&&hint&&string.IsNullOrEmpty(hint.text))hint.text="R REC / STOP   RMB CLOSE VIEW   Q DROP";
            if(glove.Equipped&&hint&&string.IsNullOrEmpty(hint.text))hint.text=$"GRAVITY {glove.energy:0}% / LMB PULL   RMB PUSH";
            if(status)status.text=take&&take.Recording?$"REC  {take.Duration:00.0} / 60s":session&&session.Phase!=RunSession.RunPhase.House?session.Notice:"";
        }
        void Idle()
        {
            if(status)status.text=mainCam&&mainCam.Take.Recording?$"REC  {mainCam.Take.Duration:00.0} / 60s":"";
            PlanarVelocity=Vector3.zero;slideTime=0;body.animator.SetFloat("Speed",0);if(hint)hint.text="";glove.Release();
            if(!body.IsDowned&&motor.enabled&&(!mainCam||!mainCam.Take.Reviewing)){
                float dt=Mathf.Min(Time.deltaTime,.05f);vertical=motor.isGrounded?-2:Mathf.Max(-30,vertical-16*dt);motor.Move(Vector3.up*(vertical*dt));
            }
        }
        void Move(Vector3 direction,float input)
        {
            float dt=Mathf.Min(Time.deltaTime,.05f);bool grounded=motor.isGrounded;
            if(grounded){lastGrounded=Time.time;if(!wasGrounded&&vertical<-4)WorldNoise.Emit(transform.position,Mathf.Clamp(-vertical*1.6f,5,18),"Landing");if(vertical<0)vertical=-2;}
            wasGrounded=grounded;if(jump.WasPressedThisFrame())jumpUntil=Time.time+.14f;
            if(!Mantling&&jump.WasPressedThisFrame()&&input>.1f&&TryMantle())jumpUntil=-1;
            if(Mantling){
                mantleTime=Mathf.Max(0,mantleTime-dt);float t=1-mantleTime/.5f;
                Vector3 up=mantleStart+Vector3.up*(mantleEnd.y-mantleStart.y+.08f);
                var desired=t<.5f?Vector3.Lerp(mantleStart,up,t*2):Vector3.Lerp(up,mantleEnd,(t-.5f)*2);
                motor.Move(desired-transform.position);vertical=0;PlanarVelocity=Vector3.zero;
                if((desired-transform.position).magnitude>.18f&&t>.1f)mantleTime=0;
                body.animator.SetFloat("Speed",0);return;
            }
            if(slide.WasPressedThisFrame()&&grounded&&PlanarVelocity.magnitude>walkSpeed+.4f){slideTime=.8f;slideDirection=PlanarVelocity.normalized;WorldNoise.Emit(transform.position,8,"Slide");}
            bool duck=crouch.IsPressed()||Sliding;if(!duck&&!CanStand())duck=true;
            CrouchAmount=Mathf.MoveTowards(CrouchAmount,duck?1:0,dt*7);
            motor.height=Mathf.Lerp(standingHeight,standingHeight*.57f,CrouchAmount);motor.center=new Vector3(0,motor.height*.5f+.01f,0);
            if(jumpUntil>=Time.time&&Time.time-lastGrounded<=.12f&&!duck){vertical=Mathf.Sqrt(jumpHeight*2*16);jumpUntil=-1;lastGrounded=-1;}
            vertical=Mathf.Max(vertical-16*dt,-30);
            float speed=duck?1.35f:sprint.IsPressed()?sprintSpeed:walkSpeed;
            if(Sliding){slideTime=Mathf.Max(0,slideTime-dt);PlanarVelocity=slideDirection*Mathf.Lerp(2.5f,6,slideTime/.8f);}
            else PlanarVelocity=Vector3.MoveTowards(PlanarVelocity,direction*speed,dt*(grounded?(input>.01f?22:30):5));
            var previous=transform.position;motor.Move((PlanarVelocity+Vector3.up*vertical)*dt);var travelled=transform.position-previous;travelled.y=0;
            if(grounded&&!Sliding)stepDistance+=travelled.magnitude;
            if(stepDistance>(duck?.85f:sprint.IsPressed()?1.1f:.75f)){
                stepDistance=0;if(footsteps){footsteps.volume=duck?.1f:sprint.IsPressed()?.48f:.3f;footsteps.Play();}
                WorldNoise.Emit(transform.position,duck?1.7f:sprint.IsPressed()?11:4,"Footstep");
            }
            Lean=Mathf.Lerp(Lean,Sliding?0:lean.ReadValue<float>(),1-Mathf.Exp(-12*dt));
            body.animator.SetFloat("Speed",Mathf.Min(2,travelled.magnitude/Mathf.Max(dt,.001f)/walkSpeed),.10f,dt);
            if(transform.position.y<-8)body.KnockDown();
        }
        public bool CanStand()
        {
            float scale=transform.lossyScale.y,radius=motor.radius*scale*.96f;
            return !Physics.CheckCapsule(transform.position+Vector3.up*(motor.height*scale-radius+.03f),transform.position+Vector3.up*(standingHeight*scale-radius),radius,WorldMask,QueryTriggerInteraction.Ignore);
        }
        public bool TryMantle()
        {
            if(Mantling||body.IsDowned)return false;
            var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;var origin=transform.position+Vector3.up*.55f;
            if(!Physics.Raycast(origin,forward,out var wall,.8f,WorldMask,QueryTriggerInteraction.Ignore))return false;
            var surface=wall.collider.GetComponentInParent<MantleSurface>();var prop=wall.collider.GetComponentInParent<PhysicsProp>();
            if(!surface&&(!prop||!prop.mantle))return false;
            var above=wall.point+forward*.42f;above.y=transform.position.y+1.35f;
            if(!Physics.Raycast(above,Vector3.down,out var top,1.1f,WorldMask,QueryTriggerInteraction.Ignore)||top.normal.y<.8f)return false;
            float height=top.point.y-transform.position.y;if(height<.28f||height>1.2f)return false;
            var end=top.point+Vector3.up*.035f;float radius=motor.radius*transform.lossyScale.x;
            if(Physics.CheckCapsule(end+Vector3.up*(radius+.02f),end+Vector3.up*(standingHeight*transform.lossyScale.y-radius),radius,WorldMask,QueryTriggerInteraction.Ignore))return false;
            mantleStart=transform.position;mantleEnd=end;mantleTime=.5f;glove.Release();WorldNoise.Emit(top.point,4,"Mantle");return true;
        }
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var prop=hit.collider.GetComponentInParent<PhysicsProp>();
            if(prop&&prop.Body&&!prop.Body.isKinematic&&hit.moveDirection.y>-.4f)prop.Body.AddForceAtPosition(Vector3.ClampMagnitude(PlanarVelocity*prop.Body.mass*3,65),hit.point,ForceMode.Force);
        }
        void Interact()
        {
            if(hint)hint.text="";
            if(!Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,3.5f,WorldMask,QueryTriggerInteraction.Collide))return;
            bool pressed=interact.WasPressedThisFrame();var door=hit.collider.GetComponentInParent<HospitalDoor>();var gate=hit.collider.GetComponentInParent<MissionGate>();var station=hit.collider.GetComponentInParent<CrewWardrobe>();var item=hit.collider.GetComponentInParent<CarryItem>();var npc=hit.collider.GetComponentInParent<HomeNpc>();string prompt="";
            if(door){prompt="E / "+(door.IsOpen?"Close sliding door":"Open sliding door");if(pressed)door.Use(this);}
            else if(gate){prompt=gate.Prompt;if(pressed)gate.Use(this);}
            else if(npc){prompt="E / "+npc.displayName;if(pressed)npc.Use(this);}
            else if(station){prompt="E / "+(body.WearsHoodie?"Store hoodie":"Wear crew hoodie");if(pressed)body.SetOutfit(!body.WearsHoodie);}
            else if(item&&!item.Owner){prompt="E / Pick up "+item.displayName;if(pressed&&!inventory.TryPickup(item))prompt=inventory.Notice;}
            if(hint)hint.text=prompt;
        }
        void LateUpdate(){UpdateView();}
        public void UpdateView()
        {
            if(!viewCamera||!body)return;
            var rotation=Quaternion.Euler(pitch,yaw,0);Vector3 center;
            if(body.IsDowned)center=body.ViewTarget.position+Vector3.up*.12f;
            else center=transform.position+Vector3.up*((Mathf.Lerp(standingHeight,standingHeight*.57f,CrouchAmount)+.015f)*transform.lossyScale.y)+transform.forward*.075f;
            var desired=firstPerson&&!body.IsDowned?center+transform.right*(Lean*.28f):center-rotation*Vector3.forward*2.6f+rotation*Vector3.right*.35f;
            var offset=desired-center;if(offset.sqrMagnitude>0&&Physics.SphereCast(center,.08f,offset.normalized,out var hit,offset.magnitude,WorldMask,QueryTriggerInteraction.Ignore))desired=center+offset.normalized*Mathf.Max(0,hit.distance-.015f);
            float actualLean=Vector3.Dot(desired-center,transform.right)/.28f;
            viewCamera.transform.SetPositionAndRotation(desired,rotation*Quaternion.Euler(0,0,firstPerson?-actualLean*8:0));
            body.head.shadowCastingMode=firstPerson&&!body.IsDowned?UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly:UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
