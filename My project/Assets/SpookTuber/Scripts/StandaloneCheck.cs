#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpookTuber
{
    // Explicit opt-in GPU smoke check; never touches the player's career or footage.
    public sealed class StandaloneCheck : MonoBehaviour
    {
        string output,failure;
        float started;
        readonly List<string> report=new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--spook-smoke");
            if(index<0||index+1>=args.Length)return;
            var check=new GameObject("Standalone QA").AddComponent<StandaloneCheck>();
            check.output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(check.output);DontDestroyOnLoad(check.gameObject);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;
            report.Add($"{SystemInfo.graphicsDeviceName} / {Screen.width}x{Screen.height} / {SystemInfo.graphicsDeviceType}");
            yield return new WaitForSecondsRealtime(3);
            var session=RunSession.Current;session.StorageDirectory=Path.Combine(output,"SmokeCareer-"+Guid.NewGuid().ToString("N"));
            if(!session.ReloadCareer()){Finish("Cannot isolate career");yield break;}
            yield return Sample("Home",8);
            var crew=FindFirstObjectByType<CrewMotor>();crew.GetComponent<CrewPhone>().Open("Settings");yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"v5_standalone_phone.png"));yield return new WaitForSecondsRealtime(1);crew.GetComponent<CrewPhone>().Close();
            if(!session.Depart()){Finish("Cannot depart: "+session.Notice);yield break;}
            while(session.IsBusy)yield return null;yield return new WaitForSecondsRealtime(3);
            crew=FindFirstObjectByType<CrewMotor>();var cc=crew.GetComponent<CharacterController>();cc.enabled=false;crew.transform.position=new Vector3(0,.08f,8);cc.enabled=true;crew.UpdateView();
            var camera=crew.mainCam;
            if(camera.Holder!=crew){camera.transform.position=crew.viewCamera.transform.position+crew.viewCamera.transform.forward*.8f;Physics.SyncTransforms();if(!camera.TryPickup(crew)){Finish("Cannot hold camera");yield break;}}
            camera.Take.StorageDirectory=Path.Combine(session.StorageDirectory,"Takes");yield return new WaitForSecondsRealtime(2);
            if(!camera.Take.StartRecording()){Finish("Cannot record");yield break;}
            yield return Sample("Hospital / camera held and recording",10);
            if(!camera.Take.StopRecording()||!File.Exists(camera.Take.LastSavedPath)){Finish("Cannot persist take");yield break;}
            report.Add("Standalone hospital take saved: "+new FileInfo(camera.Take.LastSavedPath).Length+" bytes");
            crew.GetComponent<CrewPhone>().Open("Map");yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"v5_standalone_map.png"));yield return new WaitForSecondsRealtime(1);
            Finish(failure);
        }
        IEnumerator Sample(string label,float seconds)
        {
            var frames=new List<float>();float end=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<end){yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
            frames.Sort();report.Add($"{label}: {frames.Count} frames, median {frames[frames.Count/2]:0.0} ms, p95 {frames[(int)((frames.Count-1)*.95f)]:0.0} ms; stationary view, warm sample");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,label=="Home"?"v5_standalone_home.png":"v5_standalone_hospital.png"));yield return new WaitForSecondsRealtime(1);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Assert)failure=message+"\n"+trace;}
        void Update(){if(started>0&&Time.realtimeSinceStartup-started>120)Finish("Smoke check timed out");}
        void Finish(string error){Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"v5_standalone_check.txt"),(error==null?"PASS":"FAIL / "+error)+"\n"+string.Join("\n",report));Application.Quit(error==null?0:1);enabled=false;}
    }
}
#endif
