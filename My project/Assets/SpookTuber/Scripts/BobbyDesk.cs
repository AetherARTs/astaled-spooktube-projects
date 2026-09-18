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
    // The workstation edits an EDL; it never changes the source recording or runs gameplay in replay.
    [DefaultExecutionOrder(250)]
    public sealed class BobbyDesk : MonoBehaviour
    {
        public bool IsOpen {get;private set;}
        public BobbyEpisode Episode {get;private set;}
        public float Playhead {get;private set;}
        public bool RawPlayback {get;private set;}
        public bool AwaitingChoice {get;private set;}
        public CrewTake Source=>take;
        public string Message {get;private set;}="";
        RunSession session;
        CrewTake take;
        GameObject canvasObject,page,eventObject;
        RectTransform layout;
        RawImage preview;
        Text timecode,playLabel;
        Image progress;
        readonly List<Button> buttons=new();
        readonly Stack<string> undo=new(),redo=new();
        int selected,libraryPage;
        readonly Color background=new(.035f,.05f,.065f),panel=new(.074f,.095f,.11f),ink=new(.91f,.88f,.79f),muted=new(.65f,.70f,.68f),accent=new(.94f,.64f,.27f);
        Font font;
        public void Open(CrewTake source)
        {
            session=RunSession.Current;take=source;IsOpen=true;RawPlayback=false;Playhead=0;selected=0;
            AwaitingChoice=take&&!session.Publications.Any(p=>p.episode.runId==take.RunId);
            undo.Clear();redo.Clear();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(take){
                take.Paused=true;take.ExternalPlayback=true;
                if(take.reviewImage)take.reviewImage.gameObject.SetActive(false);
                if(take.reviewLabel)take.reviewLabel.gameObject.SetActive(false);
                var draft=session.Draft;
                var published=session.Publications.FirstOrDefault(p=>p.episode.takeId==take.TakeId)?.episode;
                Episode=published!=null&&published.Validate(take,out _)?Copy(published):draft!=null&&draft.Validate(take,out _)?Copy(draft):BobbyEpisode.Build(take,"Documentary",session.BobbyLevel);
                if(Episode.Validate(take,out _))session.SaveDraft(Episode,take);
                Seek(0);
            }else Episode=null;
            Message=take?(Episode.cuts.Any(c=>c.momentId!="")?"I found something on the tape. Let's see it.":"No readable subject confirmed. You can still cut and publish the footage."):"Bring back a recording from the hospital. I'll be here.";
            if(take&&take.Duration<1.5f)Message="This tape is too short to cut. Record at least 1.5 seconds on the next take.";
            BuildUI();
        }
        public void Close(bool returnHome=true)
        {
            if(!IsOpen)return;GameUi.ConsumeInput();IsOpen=false;
            if(take)take.EndReview();
            if(canvasObject){canvasObject.SetActive(false);Destroy(canvasObject);}
            if(eventObject)Destroy(eventObject);canvasObject=null;eventObject=null;
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;if(returnHome)session.CloseStudio();
        }
        public bool SelectTake(RecoveredTake source)
        {
            if(!IsOpen||!take||!source.recovered)return false;
            if(CrewTake.SceneForTake(source.path)!=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)return session.SwitchStudioTake(source.path);
            take.EndReview();
            bool loaded=take.LoadTake(source.path);
            if(!take.BeginReview()){Message=take.Message;BuildUI();return false;}
            if(!loaded){Message=take.Message;take.Paused=true;take.ExternalPlayback=true;if(take.reviewImage)take.reviewImage.gameObject.SetActive(false);if(take.reviewLabel)take.reviewLabel.gameObject.SetActive(false);BuildUI();return false;}
            Open(take);return true;
        }
        static BobbyEpisode Copy(BobbyEpisode value)=>JsonUtility.FromJson<BobbyEpisode>(JsonUtility.ToJson(value));
        public bool ChooseEditing(bool automatic)
        {
            if(!take||!AwaitingChoice)return false;
            AwaitingChoice=false;
            if(automatic){
                if(!AutoEdit("Horror")||!Publish()){AwaitingChoice=true;BuildUI();return false;}
                Seek(0);take.Paused=false;Message="Done. I cut and uploaded the episode. Let's watch it.";BuildUI();return true;
            }
            Episode=new BobbyEpisode{takeId=take.TakeId,runId=take.RunId,sourcePath=take.LastSavedPath,preset="Documentary",editorLevel=session.BobbyLevel};
            AddManualCut(0);Message="Your edit / view the source, add shots, trim and reorder, then upload.";BuildUI();return true;
        }
        public bool AddManualCut(float sourceTime)
        {
            if(!take||Episode==null||!float.IsFinite(sourceTime))return false;
            float start=Mathf.Clamp(sourceTime,0,take.Duration),end=Mathf.Min(take.Duration,start+8);
            var cut=new EpisodeCut{start=start,end=end,reason="Manual source selection"};
            var used=Episode.cuts.SelectMany(c=>c.EvidenceIds).ToHashSet();
            var moments=take.Moments.Where(m=>!used.Contains(m.id)&&Mathf.Min(end,m.end)-Mathf.Max(start,m.start)>=.5f).Select(m=>m.id).ToArray();
            if(moments.Length>0){cut.momentId=moments[0];cut.supportingIds=moments.Skip(1).ToArray();}
            var candidate=Copy(Episode);candidate.cuts.Add(cut);
            if(!candidate.Validate(take,out var reason)){Message=reason;BuildUI();return false;}
            Remember();Episode=candidate;RawPlayback=false;selected=Episode.cuts.Count-1;Seek(Episode.Duration-(end-start));SaveEdit();BuildUI();return true;
        }
        public bool AutoEdit(string preset)
        {
            if(!take)return false;
            Remember();Episode=BobbyEpisode.Build(take,preset,session.BobbyLevel);selected=0;RawPlayback=false;Seek(0);
            Message="Bobby selected recorded moments with their surrounding footage.";bool saved=SaveEdit();BuildUI();return saved;
        }
        void Remember(){if(Episode!=null){if(undo.Count>=20)undo.Clear();undo.Push(JsonUtility.ToJson(Episode));}redo.Clear();}
        bool SaveEdit()
        {
            if(!Episode.Validate(take,out var reason)){Message=reason;return false;}
            if(!session.SaveDraft(Episode,take)){Message=session.Notice;return false;}return true;
        }
        public bool EditCut(int index,float startDelta,float endDelta,int move=0,bool remove=false)
        {
            if(Episode==null||index<0||index>=Episode.cuts.Count)return false;
            var candidate=Copy(Episode);var cut=candidate.cuts[index];
            cut.start=Mathf.Max(0,cut.start+startDelta);cut.end=Mathf.Min(take.Duration,cut.end+endDelta);
            if(remove)candidate.cuts.RemoveAt(index);
            else if(move!=0){int target=Mathf.Clamp(index+move,0,candidate.cuts.Count-1);candidate.cuts.RemoveAt(index);candidate.cuts.Insert(target,cut);}
            if(!candidate.Validate(take,out var reason)){Message=reason;BuildUI();return false;}
            Remember();Episode=candidate;selected=Mathf.Clamp(index+move,0,Episode.cuts.Count-1);RawPlayback=false;Seek(0);
            Message="Cut updated / original footage kept.";SaveEdit();BuildUI();return true;
        }
        public bool Undo(bool forwards=false)
        {
            var from=forwards?redo:undo;var to=forwards?undo:redo;if(from.Count==0)return false;
            to.Push(JsonUtility.ToJson(Episode));Episode=JsonUtility.FromJson<BobbyEpisode>(from.Pop());selected=Mathf.Min(selected,Episode.cuts.Count-1);Seek(0);SaveEdit();BuildUI();return true;
        }
        public void Seek(float time)
        {
            if(!take||!take.Reviewing||Episode==null)return;
            Playhead=Mathf.Clamp(time,0,RawPlayback?take.Duration:Episode.Duration);
            float sourceTime=RawPlayback?Playhead:Episode.SourceTime(Playhead,out _);take.Seek(sourceTime);
        }
        public bool Publish()
        {
            bool result=session.Publish(Episode,take,out _);Message=session.Notice;if(take)take.Paused=true;BuildUI();return result;
        }
        public bool ViewPublished()
        {
            var publication=Episode==null?null:session.Publications.FirstOrDefault(p=>p.episode.runId==Episode.runId);
            if(publication==null)return false;
            var source=session.Takes.FirstOrDefault(t=>t.id==publication.episode.takeId);
            if(source==null||source.id!=take.TakeId&&!SelectTake(source))return false;
            if(!IsOpen)return true; // Selecting a source in an archived scene completes after the load.
            Episode=Copy(publication.episode);RawPlayback=false;take.Paused=true;Seek(0);Message="Playing the exact episode that was published.";BuildUI();return true;
        }
        void Update()
        {
            if(!IsOpen)return;
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){Close();return;}
            if(Keyboard.current?.tabKey.wasPressedThisFrame==true&&EventSystem.current){
                var available=buttons.Where(b=>b&&b.IsInteractable()).ToList();int current=available.FindIndex(b=>b.gameObject==EventSystem.current.currentSelectedGameObject);
                int step=Keyboard.current.shiftKey.isPressed?-1:1;if(available.Count>0)EventSystem.current.SetSelectedGameObject(available[(current+step+available.Count)%available.Count].gameObject);
            }
            foreach(var button in buttons)if(button){var outline=button.GetComponent<Outline>();outline.effectColor=EventSystem.current&&EventSystem.current.currentSelectedGameObject==button.gameObject?accent:new Color(.22f,.27f,.28f);}
            if(!take||!take.Reviewing||Episode==null)return;
            float length=RawPlayback?take.Duration:Episode.Duration;
            if(!take.Paused){Seek(Playhead+Time.unscaledDeltaTime);if(Playhead>=length)take.Paused=true;}
            if(preview)preview.texture=take.ReviewTexture;
            if(timecode)timecode.text=$"{(RawPlayback?"SOURCE TAPE":"EPISODE PREVIEW")}   {Playhead:00.0} / {length:00.0}s     /     MAIN-01";
            if(playLabel)playLabel.text=take.Paused?"PLAY":"PAUSE";
            if(progress)progress.rectTransform.sizeDelta=new Vector2(864*(length>0?Playhead/length:0),4);
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
        }
        Image Box(string name,float x,float y,float w,float h,Color color)
        {
            var image=Rect(name,layout,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;return image;
        }
        Text Label(string name,string value,float x,float y,float w,float h,int size=20,Color? color=null)
        {
            var text=Rect(name,layout,x,y,w,Mathf.Max(h,size*1.5f)).gameObject.AddComponent<Text>();text.font=size>=26?GameUi.Heading:font;text.fontSize=size;text.color=color??ink;text.text=value;text.supportRichText=false;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;return text;
        }
        Button Button(string name,string label,float x,float y,float w,Action action,bool enabled=true,int size=18,float height=46)
        {
            var image=Box(name,x,y,w,height,panel);var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.interactable=enabled;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.4f,1.45f,1.45f);colors.selectedColor=new Color(1.8f,1.65f,1.2f);colors.pressedColor=new Color(.8f,.8f,.8f);colors.disabledColor=new Color(.65f,.65f,.65f);button.colors=colors;
            var outline=image.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(1,-1);
            var text=Rect("Label",image.transform,9,3,w-18,height-6).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.color=enabled?ink:muted;text.alignment=TextAnchor.MiddleCenter;text.text=label;text.supportRichText=false;text.raycastTarget=false;
            button.onClick.AddListener(()=>action());buttons.Add(button);return button;
        }
        void BuildUI()
        {
            string focus=EventSystem.current&&EventSystem.current.currentSelectedGameObject?EventSystem.current.currentSelectedGameObject.name:"Play";
            if(!canvasObject){
                font=GameUi.Body;canvasObject=new GameObject("BobbyWorkstation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(transform,false);
                var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                var scale=canvasObject.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var backdrop=Rect("Backdrop",canvasObject.transform,0,0,0,0);backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;backdrop.gameObject.AddComponent<Image>().color=background;
            }
            if(!EventSystem.current){eventObject=new GameObject("StudioInput",typeof(EventSystem),typeof(InputSystemUIInputModule));eventObject.transform.SetParent(transform);eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            if(page){page.SetActive(false);Destroy(page);}buttons.Clear();
            layout=Rect("Workspace",canvasObject.transform,0,0,1600,900);layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);page=layout.gameObject;
            Label("Brand","SPOOKTUBER / PRODUCTION",24,24,530,34,28);
            Label("Balance",$"CREDIT  {session.Credits/100m:0.00}     {session.QuotaStatus}",590,28,750,32,19,accent);
            Button("Close","BACK TO HOUSE",1380,22,196,()=>Close());
            Box("HeaderLine",24,79,1552,2,new Color(.23f,.28f,.28f));
            if(AwaitingChoice){
                Label("Handoff","THE TAPE IS HOME.",330,195,940,65,42,accent);
                Label("HandoffNote","Who should edit this episode?\n\nBobby selects recorded moments and uploads automatically.\nManual editing lets you choose shots, trim and reorder before uploading.",330,290,940,180,25);
                Button("Automatic","BOBBY / EDIT + UPLOAD",330,530,440,()=>ChooseEditing(true),take&&take.Duration>=1.5f,25,80);
                Button("Manual","I WILL EDIT IT",800,530,440,()=>ChooseEditing(false),take&&take.Duration>=1.5f,25,80);
                Label("SourceInfo",$"MAIN-01  /  {take.Duration:0.0}s  /  {take.Moments.Count} confirmed moments\n"+(take.Duration<1.5f?"Record at least 1.5 seconds to edit this tape.":Message),330,668,940,100,21,muted);
                if(EventSystem.current)EventSystem.current.SetSelectedGameObject((buttons.FirstOrDefault(b=>b.name=="Automatic"&&b.IsInteractable())??buttons.First(b=>b.IsInteractable())).gameObject);return;
            }
            Label("Library","RECOVERED TAPES",24,100,240,28,22);
            Label("LibraryNote","One episode per expedition\nTeam 60% / Crew 40%",24,136,240,52,17,muted);
            var library=session.Takes.Where(t=>t.recovered).Reverse().ToArray();int pages=Mathf.Max(1,(library.Length+5)/6);libraryPage=Mathf.Clamp(libraryPage,0,pages-1);
            for(int i=0;i<6&&libraryPage*6+i<library.Length;i++){
                var item=library[libraryPage*6+i];string flag=session.Publications.Any(p=>p.episode.runId==item.runId)?"PUBLISHED":"UNRELEASED";
                Button("Tape"+i,$"MAIN-01 / {item.duration:0.0}s\n{flag}",24,208+i*66,240,()=>SelectTake(item),take,17,54);
            }
            Button("PreviousTapes","<",24,622,54,()=>{libraryPage--;BuildUI();},libraryPage>0);
            Label("Page",$"{libraryPage+1} / {pages}",96,634,105,28,18,muted);
            Button("NextTapes",">",210,622,54,()=>{libraryPage++;BuildUI();},libraryPage+1<pages);
            Label("BobbyLabel","BOBBY / EDITOR "+(session.BobbyLevel+1).ToString("00"),24,707,240,30,21,accent);
            Label("BobbyMessage",Message,24,748,240,100,19);
            timecode=Label("Timecode",take?$"{(RawPlayback?"SOURCE TAPE":"EPISODE PREVIEW")}   {Playhead:00.0} / {(RawPlayback?take.Duration:Episode.Duration):00.0}s     /     MAIN-01":"NO FOOTAGE LOADED",288,100,864,28,21,muted);
            Box("PreviewBack",288,132,864,486,Color.black);
            preview=Rect("RecordedImage",layout,288,132,864,486).gameObject.AddComponent<RawImage>();preview.raycastTarget=false;preview.texture=take?take.ReviewTexture:null;preview.color=take?Color.white:Color.clear;
            if(!take)Label("EmptyPreview","YOUR NEXT EPISODE STARTS IN THE HOSPITAL.\n\nPick up MainCam. Press R to record.\nReturn to the RV and bring the tape home.",420,318,600,160,25,muted);
            progress=Box("Progress",288,614,0,4,accent);progress.raycastTarget=false;
            playLabel=Button("Play","PLAY",288,634,126,()=>{if(Playhead>=(RawPlayback?take.Duration:Episode.Duration))Seek(0);take.Paused=!take.Paused;},take&&Episode!=null&&Episode.cuts.Count>0).GetComponentInChildren<Text>();
            Button("BackFive","- 3s",426,634,86,()=>{take.Paused=true;Seek(Playhead-3);},take);
            Button("ForwardFive","+ 3s",524,634,86,()=>{take.Paused=true;Seek(Playhead+3);},take);
            Button("SourceToggle",RawPlayback?"VIEW EPISODE":"VIEW SOURCE",622,634,178,()=>{RawPlayback=!RawPlayback;take.Paused=true;Seek(0);BuildUI();},take&&Episode!=null);
            Button("Undo","UNDO",814,634,100,()=>Undo(),undo.Count>0);
            Button("Redo","REDO",926,634,100,()=>Undo(true),redo.Count>0);
            Button("Save",RawPlayback?"ADD SHOT":"SAVE CUT",1038,634,114,()=>{if(RawPlayback)AddManualCut(Playhead);else if(SaveEdit())Message="Draft saved";BuildUI();},take&&Episode!=null);
            Label("TimelineHeading","EPISODE CUTS / select a shot to trim or reorder",288,696,864,26,18,muted);
            if(Episode!=null)for(int i=0;i<Episode.cuts.Count;i++){
                int index=i;var cut=Episode.cuts[i];string kind=cut.momentId==""?"Unmarked":take.Moments.First(m=>m.id==cut.momentId).kind;
                var button=Button("Cut"+i,$"{i+1:00}  {kind}\n{cut.start:0.0}-{cut.end:0.0}s",288+i*108,730,100,()=>{selected=index;RawPlayback=false;take.Paused=true;Seek(Episode.cuts.Take(index).Sum(c=>c.end-c.start));BuildUI();},true,15,54);
                if(i==selected)button.GetComponent<Image>().color=new Color(.24f,.19f,.12f);
            }
            bool editable=take&&Episode!=null&&Episode.cuts.Count>0;
            Button("Earlier","IN -0.5",288,802,100,()=>EditCut(selected,-.5f,0),editable,16);
            Button("TrimStart","IN +0.5",396,802,100,()=>EditCut(selected,.5f,0),editable,16);
            Button("TrimEnd","OUT -0.5",504,802,100,()=>EditCut(selected,0,-.5f),editable,16);
            Button("Later","OUT +0.5",612,802,100,()=>EditCut(selected,0,.5f),editable,16);
            Button("MoveEarlier","MOVE <",720,802,100,()=>EditCut(selected,0,0,-1),editable&&selected>0,16);
            Button("MoveLater","MOVE >",828,802,100,()=>EditCut(selected,0,0,1),editable&&selected<Episode.cuts.Count-1,16);
            Button("Remove","REMOVE",936,802,100,()=>EditCut(selected,0,0,0,true),editable,16);
            Label("Help","TAB / ARROWS  SELECT     ENTER  USE     ESC  BACK     /     Original tapes are kept",288,863,864,25,16,muted);
            Label("EditingHeading","EDIT & RELEASE",1176,100,400,28,22);
            Label("EditingInfo",$"Bobby's cut / {(session.BobbyLevel>0?36:24)}s budget\nContext editor {(session.BobbyLevel>0?"installed":"available below")}",1176,140,400,58,20,muted);
            Button("Documentary","DOCUMENTARY",1176,210,192,()=>AutoEdit("Documentary"),take);
            Button("Horror","HORROR",1380,210,196,()=>AutoEdit("Horror"),take);
            var published=Episode==null?null:session.Publications.FirstOrDefault(p=>p.episode.runId==Episode.runId);
            bool valid=take&&Episode!=null&&Episode.Validate(take,out _);
            Button("Upload",published!=null?"ALREADY PUBLISHED":"UPLOAD EPISODE",1176,282,400,()=>Publish(),valid&&published==null,22,54);
            if(published!=null&&published.episode.id==Episode.id){
                Label("Performance",$"{published.views:N0} VIEWS\n+{published.subscribers:N0} SUBSCRIBERS\nCREDIT +{published.revenueMinor/100m:0.00}\nDAY {session.QuotaDay}/3 / {session.QuotaViews:N0}/{session.QuotaTarget:N0} VIEWS",1176,364,400,132,24,accent);
                Label("AudienceHeading","AUDIENCE / FROM YOUR EPISODE",1176,518,400,30,18,muted);
                Label("Audience",string.Join("\n\n",published.comments.Take(3)),1176,562,400,178,20);
            }else if(published!=null){
                Label("AlreadyReleased","This expedition already has a published episode.\n\nThis draft has different cuts. The published episode and its audience response are kept in the archive.",1176,367,400,220,22);
                Button("ViewPublished","VIEW PUBLISHED EPISODE",1176,630,400,()=>ViewPublished(),true,20,54);
            }else{
                Label("BeforeUpload","Preview your cut, then release it on SpookTuber TV.\n\nViews depend on what the camera could actually see.\n\nEach expedition pays once.",1176,367,400,250,22);
                Label("FilmedFacts",take?$"CONFIRMED MOMENTS  {take.Moments.Count}\nEPISODE LENGTH  {Episode?.Duration:0.0}s":"NO RECOVERED FOOTAGE",1176,634,400,62,20,accent);
            }
            Button("Upgrade",session.BobbyLevel>0?"CONTEXT EDITOR INSTALLED":"UPGRADE BOBBY / 25.00 CREDIT",1176,766,400,()=>{session.UpgradeBobby();Message=session.Notice;BuildUI();},session.BobbyLevel==0,19,54);
            Label("UpgradeInfo","More context, stronger framing selection.\nRe-cut a tape to use the upgrade.",1176,841,400,50,17,muted);
            var target=buttons.FirstOrDefault(b=>b.name==focus&&b.IsInteractable())??buttons.FirstOrDefault(b=>b.IsInteractable());if(target&&EventSystem.current)EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
