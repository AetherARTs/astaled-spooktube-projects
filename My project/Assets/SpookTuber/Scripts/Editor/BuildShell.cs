using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    public static class BuildShell
    {
        const string Root="Assets/SpookTuber/";
        static GameObject Model(string asset,Vector3 pos,float yaw=0)
        {
            var go=new GameObject(System.IO.Path.GetFileNameWithoutExtension(asset));go.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+asset));model.transform.SetParent(go.transform,false);return go;
        }
        static Camera Camera()
        {
            var camera=new GameObject("Menu camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.009f,.014f,.019f);camera.fieldOfView=44;camera.nearClipPlane=.05f;camera.farClipPlane=60;
            camera.transform.position=new Vector3(-.2f,1.4f,-3.8f);camera.transform.LookAt(new Vector3(0,1.05f,0));return camera;
        }
        static void Light(string name,Vector3 pos,Vector3 target,Color color,float intensity,float angle)
        {
            var lamp=new GameObject(name).AddComponent<Light>();lamp.type=LightType.Spot;lamp.color=color;lamp.intensity=intensity;lamp.spotAngle=angle;lamp.innerSpotAngle=angle*.65f;lamp.range=10;lamp.shadows=LightShadows.Soft;lamp.transform.position=pos;lamp.transform.LookAt(target);
        }
        public static void Build()
        {
            try{
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.025f,.033f,.043f);
                Camera();
                var player=Model("Prefabs/PF_CrewHoodie.prefab",new Vector3(.8f,0,0),-150);player.transform.localScale=Vector3.one*.88f;
                // Menu set uses only the existing visible actor; no gameplay, mic, ragdoll or mission state.
                PrefabUtility.UnpackPrefabInstance(player.transform.GetChild(0).gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var script in player.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
                foreach(var collider in player.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                foreach(var joint in player.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(joint);
                foreach(var body in player.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
                Model("Environment/Models/P_EditDesk.fbx",new Vector3(.7f,0,-.2f),180);
                var gear=Model("Environment/Models/EQP_MainCam_Detailed.fbx",new Vector3(.65f,.81f,-.28f),-145);gear.transform.localScale=new Vector3(-1,1,1);
                Model("Environment/Models/EQP_GravityGlove.fbx",new Vector3(1.2f,.84f,-.38f),20);
                Model("Hospital/Models/H_Floor_4m.fbx",Vector3.zero);Model("Hospital/Models/H_Wall_4m.fbx",new Vector3(0,0,2),180);
                Light("Cold equipment rim",new Vector3(2.2f,2.9f,.6f),new Vector3(.8f,1,0),new Color(.26f,.50f,.60f),30,64);
                Light("Warm practical",new Vector3(-.7f,2,-1.1f),new Vector3(.8f,1,0),new Color(1,.58f,.24f),18,58);
                new GameObject("Main menu").AddComponent<GameShell>();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/MainMenu.unity");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Camera();var loading=new GameObject("Loading screen");loading.AddComponent<GameShell>().loading=true;loading.AddComponent<SceneTravel>();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/Loading.unity");
                var names=new[]{Root+"Scenes/MainMenu.unity",Root+"Scenes/Loading.unity"};EditorBuildSettings.scenes=names.Select(n=>new EditorBuildSettingsScene(n,true)).Concat(EditorBuildSettings.scenes.Where(s=>!names.Contains(s.path))).ToArray();AssetDatabase.SaveAssets();EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
