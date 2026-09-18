using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    [InitializeOnLoad]
    public static class SoloPlayCheck
    {
        const string Flag="SpookTuber.SoloCheck";
        static CrewMotor motor;
        static int stage;
        static double stamp;
        static float peakAngle;
        static int lastFrame;
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static SoloPlayCheck(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
        public static void CameraRepro()
        {
            SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/SpookTuber/Scenes/ProductionHouse.unity");
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{
                if(!motor){motor=Object.FindFirstObjectByType<CrewMotor>();stamp=EditorApplication.timeSinceStartup;stage=0;}
                if(stage==2){
                    if(lastFrame!=Time.frameCount){
                        lastFrame=Time.frameCount;
                        peakAngle=Mathf.Max(peakAngle,Quaternion.Angle(motor.mainCam.transform.rotation,motor.viewCamera.transform.rotation));
                        typeof(CrewMotor).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,(Time.time*180)%360);
                        typeof(CrewMotor).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,Mathf.Sin(Time.time*3)*40);
                    }
                    if(EditorApplication.timeSinceStartup-stamp<3)return;
                }
                if(EditorApplication.timeSinceStartup-stamp<.4)return;
                var item=motor.mainCam;var body=motor.GetComponent<CrewBody>();
                if(stage==0){
                    var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=new Vector3(-2.8f,.1f,-1.43f);cc.enabled=true;
                    Physics.SyncTransforms();if(!item.TryPickup(motor))throw new Exception("Repro setup could not pick up camera");
                    stage=1;stamp=EditorApplication.timeSinceStartup;
                }else if(stage==1){
                    typeof(CrewMotor).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,70f);
                    typeof(CrewMotor).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,-25f);
                    stage=2;stamp=EditorApplication.timeSinceStartup;
                }else{
                    float angle=Quaternion.Angle(item.transform.rotation,motor.viewCamera.transform.rotation);
                    File.WriteAllText(Path.Combine(QA,"v5_camera_probe.txt"),$"angle={angle}\npeak moving angle={peakAngle}\nitem={item.transform.eulerAngles}\nview={motor.viewCamera.transform.eulerAngles}\nrb={item.GetComponent<Rigidbody>().rotation.eulerAngles}\ninterpolation={item.GetComponent<Rigidbody>().interpolation}\nleft hand={body.animator.GetBoneTransform(HumanBodyBones.LeftHand).position}\nleft grip={item.leftGrip.position}\n");
                    if(peakAngle>1)throw new Exception("Held camera diverges during continuous turn: "+peakAngle);
                    Finish(null);
                }
            }catch(Exception e){Finish(e.ToString());}
        }
        static void Finish(string failure)
        {
            SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;
            File.WriteAllText(Path.Combine(QA,"v5_camera_repro.txt"),failure??"PASS: held camera follows rotated view");
            EditorApplication.Exit(failure==null?0:1);
        }
    }
}
