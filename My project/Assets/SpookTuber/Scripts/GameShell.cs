using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpookTuber
{
    public sealed class GameShell : MonoBehaviour
    {
        public bool loading;
        public static GameShell Active {get;private set;}
        public string Page {get;private set;}="Home";
        public bool IsPause {get;private set;}
        CrewMotor crew;
        RectTransform root,page;
        Text message,progressLabel;
        Image progress;
        readonly List<Button> buttons=new();
        readonly Color ink=new(.89f,.88f,.81f),muted=new(.55f,.63f,.64f),amber=new(.95f,.65f,.27f),panel=new(.065f,.087f,.097f,.98f);
        int openedFrame;
        public static void Pause(CrewMotor motor)
        {
            if(Active)return;
            var shell=new GameObject("Pause menu").AddComponent<GameShell>();shell.IsPause=true;shell.crew=motor;
            motor.SetCursor(false);Time.timeScale=0;AudioListener.pause=true;
        }
        void Start()
        {
            Active=this;openedFrame=Time.frameCount;GameSettings.Apply();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();root=(RectTransform)transform;
            if(!EventSystem.current){var input=new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule));input.transform.SetParent(transform);input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            Show("Home");
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        Image Box(string name,float x,float y,float w,float h,Color color){var image=Rect(name,page,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;return image;}
        Text Label(string name,string value,float x,float y,float w,float h,int size=25,Color? color=null)
        {
            var text=Rect(name,page,x,y,w,h).gameObject.AddComponent<Text>();text.font=size>32?GameUi.Heading:GameUi.Body;text.fontSize=size;text.color=color??ink;text.text=value;text.supportRichText=false;text.raycastTarget=false;return text;
        }
        Button Button(string name,string text,float x,float y,float w,Action action,bool enabled=true)
        {
            var image=Box(name,x,y,w,64,panel);var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.interactable=enabled;
            var colors=b.colors;colors.highlightedColor=colors.selectedColor=new Color(2.1f,1.9f,1.5f);b.colors=colors;
            var label=Label(name+" text",text,x+22,y+10,w-44,48,25);label.alignment=TextAnchor.MiddleLeft;
            var outline=image.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(2,-2);
            b.onClick.AddListener(()=>action());buttons.Add(b);return b;
        }
        public void Show(string name)
        {
            string focus=EventSystem.current&&EventSystem.current.currentSelectedGameObject?EventSystem.current.currentSelectedGameObject.name:"";
            Page=name;if(page){page.gameObject.SetActive(false);Destroy(page.gameObject);}buttons.Clear();
            page=Rect("Menu page",root,0,0,1600,900);
            Box("Dim",0,0,1600,900,new Color(.008f,.015f,.020f,IsPause||name!="Home"?.96f:.23f));
            Box("Left matte",0,0,name=="Home"?720:1600,900,new Color(.010f,.018f,.024f,name=="Home"?.87f:.55f));
            Box("Recording line",80,96,52,4,amber);
            Label("Section",loading?"CREW TRANSPORT / PLEASE STAND BY":IsPause?"PRODUCTION ON HOLD":"S.T. / INDEPENDENT PRODUCTION",80,120,1300,42,23,amber);
            Label("Title",loading?"ON THE WAY":name=="Home"?(IsPause?"PAUSED":"SPOOKTUBER"):name.ToUpperInvariant(),80,178,1400,100,70);
            Label("Footer","SPOOKTUBER   /   SOLO PRODUCTION     •     ARROWS + ENTER SELECT    /    ESC BACK",80,840,1430,42,20,muted);
            if(loading){
                Label("Destination","RV / "+SceneTravel.Destination.Replace("ProductionHouse","PRODUCTION HOUSE").Replace("_"," ").ToUpperInvariant(),80,325,1200,64,34);
                Label("Tip","Keep a way back. Light the subject. Start rolling before it sees you.",80,438,1120,95,27,muted);
                Box("Load track",80,672,1440,6,panel);progress=Box("Progress",80,672,1,6,amber);progressLabel=Label("Load percent","LOADING",80,700,1000,50,26);return;
            }
            if(name=="Home"){
                Label("Tagline",IsPause?"Your crew and recording are paused.":"EXPLORE  /  RECORD  /  MAKE IT HOME",80,290,630,70,26,muted);
                Button("Play",IsPause?"RESUME EXPEDITION":"ENTER PRODUCTION HOUSE",80,390,540,()=>{if(IsPause)Resume();else SceneTravel.Go("ProductionHouse");});
                Button("Options","OPTIONS",80,472,540,()=>Show("Options"));Button("Controls","HOW TO PLAY",80,554,540,()=>Show("Controls"));
                Button("Credits","CREDITS",80,636,540,()=>Show("Credits"));
                Button("Leave",IsPause?"RETURN TO TITLE":"QUIT GAME",80,718,540,()=>{if(IsPause)Show("Leave expedition");else {GameSettings.Save();Application.Quit();}});
                if(!IsPause){Label("Crew note","THE NEXT EPISODE\nIS OUT THERE.",990,665,500,135,38);Label("Mode note","One crew. Three days. Make the views count.",990,794,540,42,21,muted);}
            }else if(name=="Options")Options();
            else if(name=="Controls"){
                Label("Move controls","WASD  Move       MOUSE  Look\nSHIFT  Run       SPACE  Jump / mantle\nCTRL  Crouch     C  Sprint slide\nZ / X  Lean around corners\nE  Interact     F  Shoulder flashlight",80,310,720,300,29);
                Label("Gear controls","1 / 2 / 3 or WHEEL  Change equipment\nQ  Drop    R  Record / stop\nRMB  Raise MainCam / push with glove\nLMB HOLD  Pull / hold with glove\nTAB  Crew phone     ESC  Back / pause",830,310,690,300,29);
                Label("Loop","FILM → RETURN TO RV → BOBBY OR MANUAL EDIT → PUBLISH\nCredit buys equipment. Views fill the three-day quota. Your phone keeps the world running.",80,654,1440,110,27,amber);Back();
            }else if(name=="Credits"){
                Label("Credit text","SPOOKTUBER / ASTALED\nGame direction and references: AetherARTs / Kurak\nOriginal editable models made in Blender. Built with Unity.\nChakra Petch by Cadson Demak / SIL Open Font License.\n\nFull font license is included with the game.\nPublishing in this game does not post to a real social service.",80,318,1380,360,29);Back();
            }else if(name=="Leave expedition"){
                bool mission=RunSession.Current&&RunSession.Current.Phase!=RunSession.RunPhase.House;
                Label("Leave warning",mission?"Returning to the title ends this expedition.\nYour recorded footage and equipment will be recovered.\nThe expedition counts toward the three-day contract.":"Return to the title?\nYour career and recorded footage will be kept.",80,325,1400,210,32);
                Button("Confirm leave","SAVE AND RETURN TO TITLE",80,605,650,()=>{if(!RunSession.Current){SceneTravel.Go("MainMenu");Resume();}else if(RunSession.Current.ReturnToTitle())Resume();else message.text=RunSession.Current.Notice;});
                Button("Cancel leave","STAY WITH THE CREW",830,605,650,()=>Show("Home"));message=Label("Save error","",80,706,1380,100,26,amber);
            }
            if(buttons.Count>0)EventSystem.current.SetSelectedGameObject((buttons.FirstOrDefault(b=>b.name==focus)??buttons[0]).gameObject);
        }
        void Back()=>Button("Back","< BACK",80,748,500,()=>{GameSettings.Save();Show("Home");});
        void Setting(string name,string value,float x,float y,Action down,Action up)
        {
            Label(name+" label",name.ToUpperInvariant(),x,y,530,40,22,muted);var text=Label(name+" value",value,x,y+41,340,55,30);
            Button(name+" down","-",x+365,y+32,70,down);Button(name+" up","+",x+448,y+32,70,up);
        }
        void Options()
        {
            void SetFloat(string key,float value,float min,float max){GameSettings.Set(key,Mathf.Clamp(value,min,max));Show("Options");}
            Setting("Master volume",GameSettings.Volume.ToString("P0"),80,300,()=>SetFloat("Volume",GameSettings.Volume-.1f,0,1),()=>SetFloat("Volume",GameSettings.Volume+.1f,0,1));
            Setting("Look sensitivity",GameSettings.Look.ToString("0.000"),80,425,()=>SetFloat("LookSensitivity",GameSettings.Look-.025f,.025f,.35f),()=>SetFloat("LookSensitivity",GameSettings.Look+.025f,.025f,.35f));
            Setting("Field of view",GameSettings.FieldOfView.ToString("0"),80,550,()=>SetFloat("FieldOfView",GameSettings.FieldOfView-5,60,95),()=>SetFloat("FieldOfView",GameSettings.FieldOfView+5,60,95));
            string[] quality={"PERFORMANCE","BALANCED","HIGH"};
            Setting("Graphics",quality[GameSettings.Quality],830,300,()=>{GameSettings.Set("GraphicsQuality",(GameSettings.Quality+2)%3);Show("Options");},()=>{GameSettings.Set("GraphicsQuality",(GameSettings.Quality+1)%3);Show("Options");});
            Button("Fullscreen",PlayerPrefs.GetInt("Fullscreen",Screen.fullScreen?1:0)==1?"DISPLAY / FULLSCREEN":"DISPLAY / WINDOWED",830,425,650,()=>{bool fullscreen=PlayerPrefs.GetInt("Fullscreen",Screen.fullScreen?1:0)!=1;Screen.fullScreen=fullscreen;PlayerPrefs.SetInt("Fullscreen",fullscreen?1:0);Show("Options");});
            Button("Resolution",$"RESOLUTION / {PlayerPrefs.GetInt("WindowWidth",Screen.width)} × {PlayerPrefs.GetInt("WindowHeight",Screen.height)}",830,510,650,()=>{int width=PlayerPrefs.GetInt("WindowWidth",Screen.width);int next=width<1600?1600:width<1920?1920:1280;int height=next*9/16;Screen.SetResolution(next,height,Screen.fullScreen);PlayerPrefs.SetInt("WindowWidth",next);PlayerPrefs.SetInt("WindowHeight",height);Show("Options");});
            Button("Frame limit","LIMIT / "+GameSettings.FrameLimit,830,595,315,()=>{GameSettings.Set("FrameLimit",GameSettings.FrameLimit==60?120:60);Show("Options");});
            Button("VSync","V-SYNC / "+(QualitySettings.vSyncCount>0?"ON":"OFF"),1165,595,315,()=>{GameSettings.Set("VSync",QualitySettings.vSyncCount>0?0:1);Show("Options");});
            Label("Mic help","MICROPHONE / choose device, gain gate and mute from the crew phone.\nTAB → SETTINGS / MIC. Speech is used for detection, not saved to disk.",80,671,1400,70,22,muted);Back();
        }
        public void Resume(){if(!IsPause)return;GameUi.ConsumeInput();Time.timeScale=1;AudioListener.pause=false;GameSettings.Save();if(crew)crew.SetCursor(true);Destroy(gameObject);}
        void Update()
        {
            if(loading){progress.rectTransform.sizeDelta=new Vector2(1440*SceneTravel.Progress,6);progressLabel.text=SceneTravel.Progress>=1?"READY / ENTERING":"LOADING / "+SceneTravel.Progress.ToString("P0");return;}
            if(Time.frameCount>openedFrame&&Keyboard.current?.escapeKey.wasPressedThisFrame==true){if(Page!="Home"){GameSettings.Save();Show("Home");}else if(IsPause)Resume();}
            foreach(var button in buttons)if(button)button.GetComponent<Outline>().effectColor=EventSystem.current&&EventSystem.current.currentSelectedGameObject==button.gameObject?amber:Color.clear;
        }
        void OnDestroy(){if(Active==this)Active=null;if(IsPause){Time.timeScale=1;AudioListener.pause=false;}GameSettings.Save();}
    }
}
