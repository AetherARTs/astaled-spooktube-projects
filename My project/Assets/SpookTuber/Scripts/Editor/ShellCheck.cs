using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    [InitializeOnLoad] public static class ShellCheck
    {
        const string Flag="SpookTuber.ShellCheck";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static IEnumerator<float> routine;
        static double resume,started;
        static Keyboard keyboard;
        static readonly List<string> checks=new(),visited=new();
        static bool hadVolume;
        static float volume;
        static ShellCheck(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
        public static void Run(){SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/MainMenu.unity");EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();}
        static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
        static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static void Select(string name)=>EventSystem.current.SetSelectedGameObject(GameShell.Active.GetComponentsInChildren<Button>().First(b=>b.name==name).gameObject);
        static IEnumerator<float> Exercise()
        {
            yield return 1;
            Check(GameShell.Active&&!RunSession.Current,"Title opens before creating a career or mission");
            var floor=GameObject.Find("H_Floor_4m").GetComponentInChildren<Renderer>().bounds;Check(floor.size.x>3&&floor.size.z>3&&floor.size.y<.5f,"Menu FBX floor keeps Blender axis conversion");
            Check(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"Title has one presentation camera");Capture("v6_Menu.png");Capture("v6_Menu_16x10.png",1440,900);
            Select("Options");Keys(Key.Enter);yield return .3f;Keys();Check(GameShell.Active.Page=="Options","Enter opens Options");
            Select("Master volume down");Keys(Key.Enter);yield return .3f;Keys();Check(Mathf.Abs(GameSettings.Volume-Mathf.Max(0,volume-.1f))<.001f,"Volume button applies real audio preference");
            Check(EventSystem.current.currentSelectedGameObject.name=="Master volume down","Adjusting an option preserves keyboard focus");Capture("v6_Options.png");
            Keys(Key.Escape);yield return .2f;Keys();Check(GameShell.Active.Page=="Home","Escape returns Options to title");
            Select("Controls");Keys(Key.Enter);yield return .2f;Keys();Check(GameShell.Active.Page=="Controls","Controls opens from title");Capture("v6_Controls.png");
            Keys(Key.Escape);yield return .2f;Keys();Select("Play");Keys(Key.Enter);yield return .2f;Keys();
            while(!RunSession.Current||RunSession.Current.IsBusy)yield return .1f;yield return 1;
            Check(visited.Contains("Loading")&&SceneManager.GetActiveScene().name=="ProductionHouse","Play passes through the real Loading scene into home");
            var session=RunSession.Current;string storage=Path.Combine(QA,"ShellCareer-"+Guid.NewGuid().ToString("N"));session.StorageDirectory=storage;Check(session.ReloadCareer(),"Shell test uses an isolated career");
            var crew=Object.FindFirstObjectByType<CrewMotor>();Vector3 before=crew.transform.position;
            Keys(Key.Escape);yield return .3f;Keys();Check(GameShell.Active&&GameShell.Active.IsPause&&Time.timeScale==0,"Escape opens a real solo pause menu");
            Keys(Key.W,Key.R);yield return .4f;Keys();Check(Vector3.Distance(before,crew.transform.position)<.05f&&!crew.mainCam.Take.Recording,"Pause blocks movement and recording input");Capture("v6_Pause.png");
            int noise=0;Action<Vector3,float,string> hear=(p,r,k)=>noise++;WorldNoise.Emitted+=hear;WorldNoise.Emit(crew.transform.position,20,"Pause probe");WorldNoise.Emitted-=hear;Check(noise==0,"Paused microphone/world noise does not alert AI");
            Select("Play");Keys(Key.Enter);yield return .3f;Keys();Check(!GameShell.Active&&Time.timeScale==1&&!AudioListener.pause,"Resume restores simulation and audio");
            Keys(Key.Escape);yield return .2f;Keys();Select("Leave");Keys(Key.Enter);yield return .2f;Keys();Check(GameShell.Active.Page=="Leave expedition","Returning to title explains the save/expedition consequence");
            Directory.CreateDirectory(storage);string blocked=Path.Combine(storage,"blocked");File.WriteAllText(blocked,"file");session.StorageDirectory=blocked;
            Select("Confirm leave");Keys(Key.Enter);yield return .3f;Keys();Check(GameShell.Active&&GameShell.Active.IsPause&&SceneManager.GetActiveScene().name=="ProductionHouse","Save failure keeps player in the current scene");
            session.StorageDirectory=storage;yield return .2f;Select("Confirm leave");Keys(Key.Enter);yield return .3f;Keys();
            while(SceneManager.GetActiveScene().name!="MainMenu")yield return .1f;yield return .5f;
            Check(!RunSession.Current&&GameShell.Active&&!GameShell.Active.IsPause&&Time.timeScale==1,"Saved return restores title and removes old session");Check(File.Exists(Path.Combine(storage,"HospitalRun.json")),"Return to title persisted the isolated career");
        }
        static void Scene(Scene scene,LoadSceneMode mode){visited.Add(scene.name);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{
                if(routine==null){started=EditorApplication.timeSinceStartup;hadVolume=PlayerPrefs.HasKey("Volume");volume=GameSettings.Volume;SceneManager.sceneLoaded+=Scene;Application.runInBackground=true;InputSystem.settings=Object.Instantiate(InputSystem.settings);InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();routine=Exercise();}
                if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Timed out after "+checks.LastOrDefault());
                if(EditorApplication.timeSinceStartup<resume)return;if(routine.MoveNext())resume=EditorApplication.timeSinceStartup+routine.Current;else Finish(null);
            }catch(Exception e){Finish(e.ToString());}
        }
        static void Capture(string name,int width=1600,int height=900)
        {
            var camera=Camera.main?Camera.main:Object.FindFirstObjectByType<CrewMotor>().viewCamera;var target=new RenderTexture(width,height,24);camera.targetTexture=target;
            var canvas=GameShell.Active.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.12f;
            foreach(var text in canvas.GetComponentsInChildren<Text>()){text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);text.SetAllDirty();}
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(QA,name),texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;Object.Destroy(texture);Object.Destroy(target);
        }
        static void Finish(string error){SceneManager.sceneLoaded-=Scene;SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;if(hadVolume)PlayerPrefs.SetFloat("Volume",volume);else PlayerPrefs.DeleteKey("Volume");PlayerPrefs.Save();Time.timeScale=1;AudioListener.pause=false;File.WriteAllText(Path.Combine(QA,"v6_shell_check.txt"),(error==null?"PASS":"FAIL / "+error)+"\n"+string.Join("\n",checks));EditorApplication.Exit(error==null?0:1);}
    }
}
