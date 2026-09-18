using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SpookTuber
{
    public sealed class CrewPhone : MonoBehaviour
    {
        public GameObject phoneModel;
        public bool IsOpen {get;private set;}
        public float Battery {get;private set;}=100;
        public Transform RightGrip {get;private set;}
        public string CurrentApp {get;private set;}="Home";
        public string Message {get;private set;}="";
        CrewMotor motor;
        CrewInventory inventory;
        CrewVoice voice;
        GameObject device,page,inputObject;
        Canvas screen;
        RectTransform layout,mapContent;
        Text readout,topbar,micLabel,feedback;
        Image micMeter,quotaMeter;
        Dropdown inputMenu;
        GameObject hudSlots;
        readonly Text[] slotNames=new Text[3];
        readonly Image[] slotPanels=new Image[3];
        Light emergency;
        Font font;
        float refreshAt;
        readonly List<Button> buttons=new();
        readonly HashSet<MapZone> discovered=new();
        MapZone[] zones;
        string lastNoise="No recent local sounds";
        readonly Color ink=new(.90f,.89f,.81f),muted=new(.60f,.67f,.65f),accent=new(.96f,.66f,.28f),panel=new(.08f,.11f,.13f);
        void Awake(){motor=GetComponent<CrewMotor>();inventory=GetComponent<CrewInventory>();voice=GetComponent<CrewVoice>();font=GameUi.Body;}
        void Start()
        {
            // CrewMotor adds the inventory and microphone in Awake; all Awakes finish before Start.
            inventory=GetComponent<CrewInventory>();voice=GetComponent<CrewVoice>();
            zones=FindObjectsByType<MapZone>(FindObjectsSortMode.None);BuildDevice();BuildHUD();WorldNoise.Emitted+=Heard;
            AudioListener.volume=PlayerPrefs.GetFloat("Volume",1);
        }
        void Heard(Vector3 point,float radius,string kind){if(Vector3.Distance(transform.position,point)<=radius)lastNoise=kind+" / "+(Vector3.Distance(transform.position,point)<3?"nearby":"in the building");}
        void OnDestroy(){WorldNoise.Emitted-=Heard;if(inputObject)Destroy(inputObject);}
        void BuildDevice()
        {
            device=new GameObject("CrewPhoneDevice");device.transform.SetParent(motor.viewCamera.transform,false);device.transform.localPosition=new Vector3(.09f,-.035f,.30f);device.transform.localScale=Vector3.one*.60f;
            device.transform.localRotation=Quaternion.Euler(0,-3,-2);
            if(phoneModel)Instantiate(phoneModel,device.transform);
            else{
                var shell=GameObject.CreatePrimitive(PrimitiveType.Cube);shell.name="PhoneShell";shell.transform.SetParent(device.transform,false);shell.transform.localScale=new Vector3(.32f,.49f,.026f);Destroy(shell.GetComponent<Collider>());
                var material=motor.GetComponent<CrewBody>().head.sharedMaterials.FirstOrDefault(m=>m.name.Contains("Display"));if(material)shell.GetComponent<Renderer>().sharedMaterial=material;
            }
            RightGrip=new GameObject("PhoneGrip").transform;RightGrip.SetParent(device.transform,false);RightGrip.localPosition=new Vector3(.245f,-.17f,.035f);
            var root=new GameObject("PhoneScreen",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));root.transform.SetParent(device.transform,false);root.transform.localPosition=new Vector3(0,0,-.024f);root.transform.localScale=Vector3.one*.00045f;
            screen=root.GetComponent<Canvas>();screen.renderMode=RenderMode.WorldSpace;screen.worldCamera=motor.viewCamera;root.GetComponent<RectTransform>().sizeDelta=new Vector2(600,940);
            var background=root.AddComponent<Image>();background.color=new Color(.026f,.043f,.055f);background.raycastTarget=true;
            foreach(var node in device.GetComponentsInChildren<Transform>())node.gameObject.layer=8;
            emergency=new GameObject("PhoneEmergencyLight").AddComponent<Light>();emergency.transform.SetParent(motor.viewCamera.transform,false);emergency.type=LightType.Spot;emergency.spotAngle=48;emergency.intensity=1.4f;emergency.range=6;emergency.enabled=false;emergency.color=new Color(.72f,.82f,1);
            device.SetActive(false);
        }
        void BuildHUD()
        {
            var hud=GameObject.Find("CrewHUD");if(!hud)return;
            var existing=hud.transform.Find("Controls");if(existing)existing.gameObject.SetActive(false);
            var holder=new GameObject("QuickEquipment",typeof(RectTransform)).GetComponent<RectTransform>();holder.SetParent(hud.transform,false);holder.anchorMin=holder.anchorMax=holder.pivot=new Vector2(1,0);holder.anchoredPosition=new Vector2(-35,25);holder.sizeDelta=new Vector2(720,72);
            hudSlots=holder.gameObject;
            for(int i=0;i<3;i++){
                var panelRect=Rect("Slot"+(i+1),holder,i*240,0,224,64);slotPanels[i]=panelRect.gameObject.AddComponent<Image>();slotPanels[i].color=panel;slotPanels[i].raycastTarget=false;
                var label=Rect("SlotName",panelRect,16,9,192,48).gameObject.AddComponent<Text>();label.font=GameUi.Heading;label.fontSize=22;label.color=ink;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;slotNames[i]=label;
                var border=panelRect.gameObject.AddComponent<Outline>();border.effectDistance=new Vector2(1,-1);border.effectColor=muted;
            }
            if(motor.hint){motor.hint.font=GameUi.Heading;motor.hint.rectTransform.anchorMin=motor.hint.rectTransform.anchorMax=new Vector2(.5f,.18f);motor.hint.fontSize=23;}
            if(motor.status){motor.status.font=GameUi.Heading;motor.status.fontSize=25;}
            var mic=new GameObject("VoiceIndicator",typeof(RectTransform)).GetComponent<RectTransform>();mic.SetParent(hud.transform,false);mic.anchorMin=mic.anchorMax=mic.pivot=Vector2.zero;mic.anchoredPosition=new Vector2(35,28);mic.sizeDelta=new Vector2(600,35);
            micLabel=mic.gameObject.AddComponent<Text>();micLabel.font=font;micLabel.fontSize=18;micLabel.color=muted;micLabel.raycastTarget=false;
        }
        public void Open(string app="Home")
        {
            if(!device||GetComponent<CrewBody>().IsDowned||RunSession.Current&&RunSession.Current.Studio.IsOpen)return;
            if(motor.mainCam&&motor.mainCam.Take.Recording&&!motor.mainCam.Take.StopRecording())return;
            IsOpen=true;inventory.Suspend(true);motor.firstPerson=true;motor.SetCursor(false);device.SetActive(true);
            if(!EventSystem.current){inputObject=new GameObject("PhoneInput",typeof(EventSystem),typeof(InputSystemUIInputModule));inputObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            CurrentApp=app;BuildPage();
        }
        public void Close(){if(!IsOpen)return;IsOpen=false;device.SetActive(false);inventory.Suspend(false);motor.SetCursor(true);if(inputObject)Destroy(inputObject);inputObject=null;}
        public void Show(string app){if(Battery<=0&&app!="Settings"&&app!="Home"&&app!="Controls"){Message="Battery empty / recharge at the RV";Refresh();return;}CurrentApp=app;Message="";BuildPage();}
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);rt.gameObject.layer=8;return rt;
        }
        Text Label(string name,string value,float x,float y,float w,float h,int size=28,Color? color=null)
        {
            var label=Rect(name,layout,x,y,w,h).gameObject.AddComponent<Text>();label.font=size>=32?GameUi.Heading:font;label.fontSize=size;label.color=color??ink;label.text=value;label.supportRichText=false;label.raycastTarget=false;return label;
        }
        Button Button(string name,string title,float x,float y,float width,Action action,float height=66,bool enabled=true)
        {
            var box=Rect(name,layout,x,y,width,height).gameObject.AddComponent<Image>();box.color=panel;
            var button=box.gameObject.AddComponent<Button>();button.targetGraphic=box;button.interactable=enabled;
            var colors=button.colors;colors.highlightedColor=new Color(1.7f,1.6f,1.35f);colors.selectedColor=new Color(1.8f,1.7f,1.35f);button.colors=colors;
            var text=Rect("Label",box.transform,10,5,width-20,height-10).gameObject.AddComponent<Text>();text.font=font;text.fontSize=25;text.color=ink;text.text=title;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            var focus=box.gameObject.AddComponent<Outline>();focus.effectDistance=new Vector2(2,-2);button.onClick.AddListener(()=>action());buttons.Add(button);return button;
        }
        void InputDeviceMenu()
        {
            var names=voice.Devices;var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.name="InputDevice";go.transform.SetParent(layout,false);
            var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(28,-513);rt.sizeDelta=new Vector2(542,66);
            var menu=go.GetComponent<Dropdown>();inputMenu=menu;menu.ClearOptions();menu.AddOptions(names.Length>0?names.ToList():new List<string>{"No input device connected"});menu.value=Mathf.Max(0,Array.IndexOf(names,voice.Device));menu.interactable=names.Length>0;
            go.GetComponent<Image>().color=panel;menu.template.sizeDelta=new Vector2(0,330);menu.itemText.rectTransform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(0,66);
            var toggle=menu.itemText.GetComponentInParent<Toggle>(true);toggle.targetGraphic.color=panel;toggle.graphic.color=accent;((RectTransform)toggle.graphic.transform).sizeDelta=new Vector2(8,8);
            var colors=toggle.colors;colors.highlightedColor=colors.selectedColor=new Color(2.4f,2.4f,2.1f);toggle.colors=colors;menu.colors=colors;
            menu.template.GetComponent<ScrollRect>().content.sizeDelta=new Vector2(0,74);
            menu.template.GetComponent<ScrollRect>().verticalScrollbar.targetGraphic.color=muted;
            go.transform.Find("Arrow").gameObject.SetActive(false);
            var arrow=Rect("Expand",go.transform,509,19,22,30).gameObject.AddComponent<Text>();arrow.text="v";arrow.raycastTarget=false;
            foreach(var label in go.GetComponentsInChildren<Text>(true)){label.font=font;label.fontSize=21;label.color=ink;label.alignment=TextAnchor.MiddleLeft;}
            foreach(var node in go.GetComponentsInChildren<Transform>(true))node.gameObject.layer=8;
            menu.template.GetComponent<Image>().color=panel;menu.captionText.rectTransform.offsetMax=new Vector2(-32,-5);menu.captionText.rectTransform.offsetMin=new Vector2(12,5);
            menu.onValueChanged.AddListener(index=>{voice.Enable(names[index]);BuildPage();});
        }
        void BuildPage()
        {
            if(page){page.SetActive(false);Destroy(page);}buttons.Clear();mapContent=null;micMeter=null;quotaMeter=null;feedback=null;inputMenu=null;
            layout=Rect("PhonePage",screen.transform,0,0,600,940);page=layout.gameObject;
            topbar=Label("TopBar","S.T. / CREW PHONE",28,25,544,52,25,accent);
            Label("Title",CurrentApp.ToUpperInvariant(),28,92,544,58,42);
            var divider=Rect("Divider",layout,28,155,544,2).gameObject.AddComponent<Image>();divider.color=new Color(.26f,.33f,.33f);divider.raycastTarget=false;
            Button("Home",CurrentApp=="Home"?"CONTROLS":"< APPS",28,832,260,()=>Show(CurrentApp=="Home"?"Controls":"Home"));Button("Close","PUT AWAY",310,832,260,Close);
            readout=Label("Readout","",28,174,544,250,27);
            if(CurrentApp=="Home"){
                var track=Rect("QuotaTrack",layout,28,380,544,10).gameObject.AddComponent<Image>();track.color=panel;track.raycastTarget=false;
                quotaMeter=Rect("QuotaProgress",layout,28,380,0,10).gameObject.AddComponent<Image>();quotaMeter.color=accent;quotaMeter.raycastTarget=false;
                Button("Mission","MISSION",28,450,260,()=>Show("Mission"));Button("Map","MAP / GPS",310,450,260,()=>Show("Map"));
                Button("Equipment","EQUIPMENT",28,536,260,()=>Show("Equipment"));Button("Harmony","HARMONY",310,536,260,()=>Show("Harmony"));
                Button("Settings","SETTINGS / MIC",28,622,542,()=>Show("Settings"));
                feedback=Label("Feedback","",28,722,542,85,22,muted);
            }else if(CurrentApp=="Mission"){
                Button("Contract","RETRY MISSED CONTRACT",28,645,542,()=>{RunSession.Current.RetryContract();Message=RunSession.Current.Notice;Refresh();},76,RunSession.Current&&RunSession.Current.QuotaFailed);
            }else if(CurrentApp=="Map"){
                readout.rectTransform.sizeDelta=new Vector2(544,115);mapContent=Rect("DiscoveredMap",layout,28,310,544,420);
                Button("Locate","LOCATE MAINCAM / RV",28,746,542,()=>{Message="GPS markers show your equipment and extraction";Refresh();});
            }else if(CurrentApp=="Equipment"){
                for(int i=0;i<3;i++){int index=i;Button("Slot"+i,(i+1)+" / "+(inventory[i]?inventory[i].displayName:"EMPTY"),28,456+i*80,542,()=>{Close();inventory.Select(index);});}
                Button("Emergency","PHONE LIGHT",28,720,542,()=>{if(Battery>0)emergency.enabled=!emergency.enabled;Refresh();});
            }else if(CurrentApp=="Harmony"){
                Button("Call","CHECK IN WITH BOBBY",28,542,542,()=>{var s=RunSession.Current;Message=s&&s.Phase==RunSession.RunPhase.Hospital?(motor.mainCam.Take.Moments.Count>0?"Bobby: You have something on that tape. Get back to the RV in one piece.":"Bobby: Keep some distance. Light the subject. Roll before it sees you."):"Bobby: The desk is ready. Bring me a tape or make your own cut.";Refresh();},86);
                Button("Ping","FIND THE RV",28,660,542,()=>Show("Map"));
            }else if(CurrentApp=="Supply"){
                foreach(var pair in new[]{(CarryItem.Kind.ProductionLight,"PRODUCTION LIGHT",440),(CarryItem.Kind.Noisemaker,"NOISEMAKER",596)}){
                    var kind=pair.Item1;bool own=RunSession.Current&&RunSession.Current.OwnsGear(kind);long cost=RunSession.GearPrice(kind);
                    Label(kind+"Label",pair.Item2,28,pair.Item3-45,544,40,27,accent);
                    Button(kind.ToString(),(own?"SELL / +":"BUY / -")+$"{(own?cost/2m:cost)/100m:0.00} CREDIT",28,pair.Item3,542,()=>{RunSession.Current.TradeGear(kind,own);Message=RunSession.Current.Notice;BuildPage();},70,RunSession.Current&&RunSession.Current.Phase==RunSession.RunPhase.House);
                }
            }else if(CurrentApp=="Settings"){
                readout.rectTransform.sizeDelta=new Vector2(544,240);
                micMeter=Rect("MicMeter",layout,28,397,1,12).gameObject.AddComponent<Image>();micMeter.color=accent;micMeter.raycastTarget=false;
                Button("Microphone",voice.Listening?"MUTE MICROPHONE":"ENABLE MICROPHONE",28,433,542,()=>{if(voice.Listening)voice.Mute();else voice.Enable(voice.Device);BuildPage();});
                InputDeviceMenu();
                Button("GateDown","GATE -",28,593,260,()=>{voice.SetThreshold(voice.Threshold-.005f);Refresh();});Button("GateUp","GATE +",310,593,260,()=>{voice.SetThreshold(voice.Threshold+.005f);Refresh();});
                Button("VolumeDown","VOLUME -",28,673,260,()=>{AudioListener.volume=Mathf.Clamp01(AudioListener.volume-.1f);PlayerPrefs.SetFloat("Volume",AudioListener.volume);Refresh();});Button("VolumeUp","VOLUME +",310,673,260,()=>{AudioListener.volume=Mathf.Clamp01(AudioListener.volume+.1f);PlayerPrefs.SetFloat("Volume",AudioListener.volume);Refresh();});
                Button("LookDown","LOOK -",28,748,260,()=>SetSensitivity(-.015f));Button("LookUp","LOOK +",310,748,260,()=>SetSensitivity(.015f));
            }
            Refresh();if(EventSystem.current&&buttons.Count>0)EventSystem.current.SetSelectedGameObject((buttons.FirstOrDefault(b=>b.name==(CurrentApp=="Home"?"Mission":"Microphone")&&b.IsInteractable())??buttons.FirstOrDefault(b=>b.IsInteractable()))?.gameObject);
        }
        void SetSensitivity(float delta){motor.mouseSensitivity=Mathf.Clamp(motor.mouseSensitivity+delta,.025f,.35f);PlayerPrefs.SetFloat("LookSensitivity",motor.mouseSensitivity);PlayerPrefs.Save();Refresh();}
        void Refresh()
        {
            if(!readout)return;var s=RunSession.Current;
            topbar.text=$"S.T.   {Battery:0}%     "+(s&&s.AtRV()?"CHARGING":"SOLO");
            string value="";
            switch(CurrentApp){
                case "Home":value=$"DAY {s?.QuotaDay} / 3\nVIEWS  {s?.QuotaViews:N0} / {s?.QuotaTarget:N0}\n\nCREDIT  {(s?s.Credits/100m:0):0.00}";break;
                case "Controls":value="WASD move / Shift run\nSpace jump or mantle a low ledge\nCtrl crouch / C slide while running\nZ / X lean around corners\n\nE interact / F shoulder light\nR record / RMB close camera view\n1-3 or wheel swap equipment\nQ drop / Tab put phone away\n\nGlove: LMB pull / RMB push\nWheel adjusts hold distance\n\nMenus: arrows + Enter / Esc back";readout.rectTransform.sizeDelta=new Vector2(544,620);readout.fontSize=24;break;
                case "Mission":value=$"ABANDONED HOSPITAL\n{s?.QuotaStatus}\n\nFilm readable encounters. Return to the RV. Release one episode per expedition.\n\nViews fill the 3-day quota.\nCredit buys equipment.\nSubscribers grow your audience.\n\n{s?.Notice}";readout.rectTransform.sizeDelta=new Vector2(544,420);break;
                case "Map":value="EXPLORED AREAS ONLY\n+ YOU    C CAMERA    R RV";DrawMap();break;
                case "Equipment":value=$"3 QUICK SLOTS / 1, 2, 3 or wheel\nQ puts the selected item down.\n\nGlove energy {GetComponent<GravityGlove>().energy:0}%\nPhone {Battery:0}% / charge at RV\nEmergency light {(emergency.enabled?"ON":"OFF")}";break;
                case "Harmony":value="BOBBY / SOLO CHECK-IN\n\n"+lastNoise+"\n\n"+Message;break;
                case "Supply":value=$"SUPPLY MART\nCREDIT {(s?s.Credits/100m:0):0.00}\n\nPurchased gear stays yours.\nCollect it from the equipment bench.\n\n"+Message;break;
                case "Settings":value=$"{voice.Status}\n{(voice.Device.Length>36?voice.Device.Substring(0,33)+"...":voice.Device)}\nLEVEL {voice.Level:0.000} / GATE {voice.Threshold:0.000}\nVOLUME {AudioListener.volume:P0} / LOOK {motor.mouseSensitivity:0.000}\nSound alerts nearby threats.\nMic audio is not saved to disk.";break;
            }
            readout.text=value;if(micMeter)micMeter.rectTransform.sizeDelta=new Vector2(Mathf.Clamp01(voice.Level*8)*544,12);
            if(feedback)feedback.text=string.IsNullOrEmpty(Message)?"The world stays live.\nArrows + Enter / Tab put away":Message;
            if(quotaMeter&&s)quotaMeter.rectTransform.sizeDelta=new Vector2(544*Mathf.Clamp01((float)s.QuotaViews/s.QuotaTarget),10);
        }
        void DrawMap()
        {
            if(!mapContent)return;foreach(Transform child in mapContent)Destroy(child.gameObject);
            if(zones.Length==0)return;
            var bounds=new Bounds(zones[0].transform.position,Vector3.one);foreach(var zone in zones)bounds.Encapsulate(new Bounds(zone.transform.position,zone.size));
            float factor=Mathf.Min(520/Mathf.Max(1,bounds.size.x),390/Mathf.Max(1,bounds.size.z));
            Vector2 Project(Vector3 p)=>new(272+(p.x-bounds.center.x)*factor,210-(p.z-bounds.center.z)*factor);
            foreach(var zone in discovered){if(!zone)continue;var p=Project(zone.transform.position);var rect=Rect(zone.label,mapContent,p.x-zone.size.x*factor/2,p.y-zone.size.z*factor/2,zone.size.x*factor,zone.size.z*factor);rect.gameObject.AddComponent<Image>().color=new Color(.14f,.24f,.25f);
                if(rect.sizeDelta.x>60&&rect.sizeDelta.y>42){var label=Rect("AreaName",rect,3,3,rect.sizeDelta.x-6,rect.sizeDelta.y-6).gameObject.AddComponent<Text>();label.font=font;label.fontSize=17;label.color=muted;label.text=zone.label;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;}}
            void Marker(string name,Vector3 position,Color color){var p=Project(position);var label=Rect(name,mapContent,p.x-25,p.y-26,50,52).gameObject.AddComponent<Text>();label.font=font;label.fontSize=32;label.color=color;label.text=name;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;}
            Marker("+",transform.position,accent);if(motor.mainCam)Marker("C",motor.mainCam.transform.position,ink);if(RunSession.Current&&RunSession.Current.Phase!=RunSession.RunPhase.House)Marker("R",RunSession.Current.RVPosition,new Color(.4f,.8f,.7f));
        }
        void Update()
        {
            var s=RunSession.Current;
            if(s&&(s.Phase==RunSession.RunPhase.House||s.AtRV()))Battery=Mathf.Min(100,Battery+Time.deltaTime*3);
            else Battery=Mathf.Max(0,Battery-Time.deltaTime*((IsOpen?.045f:0)+(emergency&&emergency.enabled?.14f:0)));
            if(Battery<=0&&emergency)emergency.enabled=false;
            if(zones!=null)foreach(var zone in zones)if(zone&&new Bounds(zone.transform.position,zone.size+Vector3.up*4).Contains(transform.position))discovered.Add(zone);
            if(IsOpen&&Keyboard.current?.escapeKey.wasPressedThisFrame==true){if(inputMenu&&inputMenu.transform.Find("Dropdown List"))inputMenu.Hide();else if(CurrentApp=="Home")Close();else Show("Home");}
            if(IsOpen&&GetComponent<CrewBody>().IsDowned)Close();
            foreach(var button in buttons)if(button)button.GetComponent<Outline>().effectColor=EventSystem.current&&EventSystem.current.currentSelectedGameObject==button.gameObject?accent:Color.clear;
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.15f;
            if(IsOpen)Refresh();
            if(motor.hint)motor.hint.rectTransform.anchorMin=motor.hint.rectTransform.anchorMax=new Vector2(motor.Aiming?.36f:.5f,motor.Aiming?.09f:.18f);
            bool showHud=!IsOpen&&!(s&&s.Studio.IsOpen)&&!(motor.mainCam&&motor.mainCam.Take.Reviewing);
            if(hudSlots){hudSlots.SetActive(showHud);for(int i=0;i<3;i++){slotNames[i].text=(i+1)+"  /  "+(inventory[i]?inventory[i].kind==CarryItem.Kind.GravityGlove?"GLOVE":inventory[i].displayName.ToUpperInvariant():"EMPTY");slotPanels[i].GetComponent<Outline>().effectColor=i==inventory.Selected?accent:new Color(.22f,.28f,.29f);slotPanels[i].color=i==inventory.Selected?new Color(.14f,.17f,.17f):new Color(.045f,.064f,.074f,.88f);}}
            if(micLabel)micLabel.enabled=showHud;
            if(micLabel)micLabel.text=voice.Status+(voice.Listening?$"  {Mathf.Clamp01(voice.Level*8):P0}":"");
        }
    }
}
