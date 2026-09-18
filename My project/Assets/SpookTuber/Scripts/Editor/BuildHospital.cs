using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    public static class BuildHospital
    {
        const string Root="Assets/SpookTuber/",Art=Root+"Hospital/";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static Transform world;
        static readonly Dictionary<string,Material> materials=new();
        internal static Material Mat(string name,Color color,float metal=0)
        {
            string path=Art+"Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.25f);mat.SetFloat("_Metallic",metal);
            if(name!="H_Glass"&&name!="H_Light"&&name!="H_Rubber"){
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/TEX_Hospital_"+(name=="H_Coat"||name=="H_Linen"?"Fabric":"Wear")+".png");
                if(texture)mat.SetTexture("_BaseMap",texture);
            }
            if(name=="H_Light"){mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.6f);}
            EditorUtility.SetDirty(mat);materials[name]=mat;return mat;
        }
        internal static GameObject Box(string name,Vector3 pos,Vector3 size,string mat,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);
            go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=materials[mat];return go;
        }
        internal static GameObject Prop(string name,Vector3 pos,float yaw=0,Transform parent=null,bool collision=true)
        {
            var go=new GameObject(name);go.transform.SetParent(parent?parent:world);go.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
            // Keep the FBX root's Blender-to-Unity axis conversion intact.
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Environment/Models/"+name+".fbx");if(!source)source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Models/"+name+".fbx");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(go.transform,false);
            if(collision)foreach(var mesh in go.GetComponentsInChildren<MeshFilter>())mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            return go;
        }
        internal static void Label(string text,Vector3 pos,float yaw=0,float size=.17f,Transform parent=null,string identity=null)
        {
            var go=new GameObject(identity??"Label_"+text);go.transform.SetParent(parent?parent:world);go.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size*.25f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.68f,.75f,.63f);
        }
        internal static AudioSource Sound(GameObject owner,string clip,float volume,bool loop=false)
        {
            var node=new GameObject(clip);node.transform.SetParent(owner.transform,false);var audio=node.AddComponent<AudioSource>();
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Art+"Audio/"+clip+".wav");audio.spatialBlend=1;audio.minDistance=1.5f;audio.maxDistance=18;
            audio.rolloffMode=AudioRolloffMode.Linear;audio.volume=volume;audio.loop=loop;audio.playOnAwake=loop;return audio;
        }
        static void Lamp(Vector3 pos,bool lit=true)
        {
            var fixture=Prop("H_Fluorescent",pos,0,null,false);
            if(!lit){foreach(var r in fixture.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m.name=="H_Light"?materials["H_DarkMetal"]:m).ToArray();return;}
            var light=new GameObject("PracticalLight").AddComponent<Light>();light.transform.SetParent(world);light.transform.position=pos-Vector3.up*.3f;
            light.type=LightType.Spot;light.transform.rotation=Quaternion.Euler(90,0,0);light.spotAngle=135;light.innerSpotAngle=90;
            light.range=8;light.intensity=4.8f;light.color=new Color(.81f,.86f,.73f);light.shadows=LightShadows.Soft;
            light.shadowBias=.025f;light.shadowNormalBias=.03f;
        }
        static void Door(Vector3 position,float yaw,string name)
        {
            var root=new GameObject("Door_"+name);root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            var frame=Prop("H_DoorFrame",position,yaw,root.transform);
            var hinge=new GameObject("Hinge").transform;hinge.SetParent(root.transform,false);hinge.localPosition=new Vector3(.8f,0,0);
            var leaf=Prop("H_Door",hinge.position,yaw,hinge);
            var door=root.AddComponent<HospitalDoor>();door.leaf=hinge;door.sound=Sound(root,"Door",.8f);
            var obstruction=root.AddComponent<NavMeshObstacle>();obstruction.shape=NavMeshObstacleShape.Box;obstruction.center=new Vector3(0,1.2f,0);
            obstruction.size=new Vector3(1.68f,2.4f,.18f);obstruction.carving=true;obstruction.carveOnlyStationary=false;door.obstacle=obstruction;
        }
        static void Wall(Vector3 pos,float yaw=0,float width=4)
        {
            var go=Prop("H_Wall_4m",pos,yaw);go.transform.localScale=new Vector3(width/4,1,1);
        }
        [MenuItem("SpookTuber/Build hospital and mission connection")]
        public static void Build()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try{
                Directory.CreateDirectory(Art+"Materials");AssetDatabase.Refresh();
                var colors=new Dictionary<string,Color>{
                    {"H_Plaster",new(.36f,.39f,.34f)},{"H_GreenPaint",new(.12f,.22f,.19f)},{"H_Tile",new(.38f,.43f,.39f)},
                    {"H_Grout",new(.055f,.065f,.057f)},{"H_Steel",new(.22f,.27f,.26f)},{"H_DarkMetal",new(.035f,.047f,.045f)},
                    {"H_Rust",new(.23f,.095f,.044f)},{"H_Linen",new(.43f,.42f,.31f)},{"H_Coat",new(.48f,.47f,.36f)},
                    {"H_Rubber",new(.016f,.021f,.023f)},{"H_Glass",new(.018f,.068f,.055f)},{"H_Light",new(.7f,.78f,.58f)},
                    {"H_Red",new(.36f,.035f,.018f)},{"H_Paper",new(.61f,.57f,.43f)},{"H_Amber",new(.63f,.37f,.10f)}};
                foreach(var pair in colors)Mat(pair.Key,pair.Value,pair.Key.Contains("Steel")||pair.Key.Contains("Metal")?.65f:0);
                foreach(var path in Directory.GetFiles(Art+"Models","*.fbx")){
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                    importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
                    importer.animationType=path.Contains("Surgeon")?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
                    importer.importAnimation=false;importer.importNormals=ModelImporterNormals.Import;importer.optimizeGameObjects=false;
                    foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
                    importer.SaveAndReimport();
                }
                ConnectHouse();CreateHospital();AssetDatabase.SaveAssets();
                EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/ProductionHouse.unity",true),new EditorBuildSettingsScene(Root+"Scenes/Hospital.unity",true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=Root+"Scenes/ProductionHouse.unity"&&s.path!=Root+"Scenes/Hospital.unity")).ToArray();
                File.WriteAllText(Path.Combine(QA,"hospital_asset_validation.txt"),"PASS\n"+Directory.GetFiles(Art+"Models","*.fbx").Length+" Blender assets imported\n6 medical rooms, corridor and RV forecourt\nNative NavMesh baked; every room reachable with doors open\nHouse departure and footage review connected\n");
                Debug.Log("SPOOKTUBER_HOSPITAL_BUILD_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);File.WriteAllText(Path.Combine(QA,"hospital_failure.txt"),e.ToString());if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
        internal static void ConnectHouse()
        {
            if(materials.Count==0)foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Art+"Materials"})){
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));materials[material.name]=material;
            }
            EditorSceneManager.OpenScene(Root+"Scenes/ProductionHouse.unity");
            foreach(var old in SceneManager.GetActiveScene().GetRootGameObjects().Where(g=>g.name=="MissionConnection"))Object.DestroyImmediate(old);
            var connection=new GameObject("MissionConnection");new GameObject("RunSession").AddComponent<RunSession>().transform.SetParent(connection.transform);
            var entrance=GameObject.Find("EntryDoor");var gate=entrance.GetComponent<MissionGate>();if(!gate)gate=entrance.AddComponent<MissionGate>();gate.action=MissionGate.Action.Depart;
            world=connection.transform;
            Label("HOSPITAL / RV DEPARTURE",new Vector3(3.1f,2.35f,-3.8f),180,.09f);
            var desk=Box("FootageDesk",new Vector3(3.65f,.7f,-1.6f),new Vector3(.9f,.10f,1.0f),"H_DarkMetal",world);
            Box("DeskSupport",new Vector3(3.65f,.35f,-1.6f),new Vector3(.7f,.7f,.7f),"H_GreenPaint",world);
            var monitor=Prop("H_Monitor",new Vector3(3.65f,.75f,-1.6f),-90,world);
            monitor.AddComponent<MissionGate>().action=MissionGate.Action.Edit;
            // Gate owns its hit collider, including clicks on the visible monitor mesh.
            var trigger=monitor.AddComponent<BoxCollider>();trigger.center=new Vector3(0,.27f,0);trigger.size=new Vector3(.5f,.5f,.4f);
            // Display copy can change without breaking the stable binding in existing house takes.
            Label("BOBBY / EDIT & RELEASE",new Vector3(4.3f,1.6f,-1.6f),90,.09f,identity:"Label_FOOTAGE REVIEW");
            var motor=Object.FindFirstObjectByType<CrewMotor>();
            var step=motor.transform.Find("Footstep");if(step)Object.DestroyImmediate(step.gameObject);
            motor.footsteps=Sound(motor.gameObject,"Footstep",.32f);
            var oldImpact=motor.mainCam.transform.Find("Door");if(oldImpact)Object.DestroyImmediate(oldImpact.gameObject);
            motor.mainCam.impact=Sound(motor.mainCam.gameObject,"Door",.25f);
            motor.status.rectTransform.sizeDelta=new Vector2(1400,100);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static void CreateHospital()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.065f,.083f,.074f);
            RenderSettings.skybox=null;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.028f,.046f,.043f);RenderSettings.fogStartDistance=7;RenderSettings.fogEndDistance=33;
            world=new GameObject("HospitalWorld").transform;
            for(int i=0;i<7;i++){
                var floor=Prop("H_Floor_4m",new Vector3(0,0,2+i*4));floor.transform.localScale=new Vector3(.75f,1,1);
                var ceiling=Prop("H_Ceiling_4m",new Vector3(0,3,2+i*4));ceiling.transform.localScale=new Vector3(.75f,1,1);
                Lamp(new Vector3(0,2.9f,2+i*4),i!=2&&i!=5);
            }
            Wall(new Vector3(0,0,28),0,3);
            foreach(int side in new[]{-1,1}){
                Wall(new Vector3(side*1.5f,0,1),side*90,2);Wall(new Vector3(side*1.5f,0,27),side*90,2);
                for(int room=0;room<3;room++){
                    float z=6+room*8,x=side*5.5f;
                    for(int a=0;a<2;a++)for(int b=0;b<2;b++){
                        Prop("H_Floor_4m",new Vector3(side*(3.5f+a*4),0,z-2+b*4));
                        Prop("H_Ceiling_4m",new Vector3(side*(3.5f+a*4),3,z-2+b*4));
                    }
                    for(int a=0;a<2;a++)Wall(new Vector3(side*9.5f,0,z-2+a*4),side*90);
                    Wall(new Vector3(side*1.5f,0,z-2.45f),-side*90,3.1f);
                    Wall(new Vector3(side*1.5f,0,z+2.45f),-side*90,3.1f);
                    Box("DoorHeader",new Vector3(side*1.5f,2.74f,z),new Vector3(.16f,.52f,1.8f),"H_Plaster",world);
                    Door(new Vector3(side*1.5f,0,z),side*90,(side<0?"W":"E")+(room+1));
                    Label((side<0?new[]{"WARD 01","OPERATING","STORAGE"}:new[]{"WAITING","WARD 02","MAINTENANCE"})[room],new Vector3(side*1.39f,2.67f,z),side*90,.11f);
                    Lamp(new Vector3(x,2.9f,z),room!=2||side>0);
                    Prop("H_PaperDebris",new Vector3(x+side*.5f,.015f,z-1.4f),room*53,world,false);
                    if(side<0&&room==1){
                        Prop("H_OperatingTable",new Vector3(x,0,z));Prop("H_SurgicalLamp",new Vector3(x-.8f,0,z));
                        Prop("H_MedicalCart",new Vector3(x-1.8f,0,z-.8f),30);
                        Prop("H_Monitor",new Vector3(x-1.8f,.94f,z-.8f),30,world,false);
                        var hum=Sound(world.gameObject,"SurgicalHum",.18f,true);hum.transform.position=new Vector3(x,1,z);
                        Prop("H_PrivacyScreen",new Vector3(x+1.9f,0,z+1),-20);
                    }else if(room==0&&side>0){
                        for(int b=0;b<2;b++)Prop("H_WaitingBench",new Vector3(x,0,z-2+b*4),b*180);
                        Prop("H_MedicineCabinet",new Vector3(side*8.8f,0,z),-90);
                    }else if(room<2){
                        for(int b=0;b<2;b++){
                            Prop("H_Bed",new Vector3(x+(b==0?-1.6f:1.6f),0,z+.2f));
                            Prop("H_IVStand",new Vector3(x+(b==0?-2.3f:2.3f),0,z-.5f));
                        }
                        Prop("H_MedicalCart",new Vector3(x,0,z+2.5f));Prop("H_Chair",new Vector3(x,0,z-2.6f),180);
                    }else{
                        for(int b=0;b<4;b++)Prop("H_Locker",new Vector3(x-1.5f+b*.85f,0,z+3.3f));
                        for(int b=0;b<3;b++)Prop("H_SupplyCrate",new Vector3(x-2+b*.8f,0,z-2.9f),b*12);
                        Prop("H_PrivacyScreen",new Vector3(x+1.9f,0,z),80);
                    }
                    // Two side-room connections create alternate routes around a blocked corridor.
                    if(room<2){
                        Wall(new Vector3(x-2.45f,0,z+4),0,3.1f);Wall(new Vector3(x+2.45f,0,z+4),0,3.1f);
                        Box("ConnectingHeader",new Vector3(x,2.74f,z+4),new Vector3(1.8f,.52f,.16f),"H_Plaster",world);
                        Door(new Vector3(x,0,z+4),0,"Link"+side+room);
                    }
                }
                for(int i=0;i<2;i++){
                    Wall(new Vector3(side*(3.5f+i*4),0,2),180);
                    Wall(new Vector3(side*(3.5f+i*4),0,26));
                }
            }
            // A clear, unlocked two-metre entrance remains usable during every encounter.
            Label("EXIT / RV",new Vector3(0,2.65f,.18f),180,.15f);
            Prop("H_MedicalCart",new Vector3(-.97f,0,10),-8);
            Prop("H_WaitingBench",new Vector3(1.06f,0,18),-90);
            Prop("H_PaperDebris",new Vector3(.3f,.018f,11),27,world,false);
            var outside=new GameObject("Forecourt").transform;
            Box("Concrete",new Vector3(0,-.1f,-6.5f),new Vector3(21,.2f,13),"H_Grout",outside);
            foreach(int side in new[]{-1,1})Box("RetainingWall",new Vector3(side*10.3f,1.2f,-6.5f),new Vector3(.25f,2.4f,13),"H_Plaster",outside);
            Box("RearFence",new Vector3(0,1,-12.8f),new Vector3(21,2,.2f),"H_DarkMetal",outside);
            var rv=Prop("H_CrewRV",new Vector3(0,0,-8.5f),180,outside).transform;
            var extraction=Box("RVDeparturePanel",new Vector3(0,1.1f,-6.1f),new Vector3(.55f,.3f,.1f),"H_Amber",rv);extraction.AddComponent<MissionGate>().action=MissionGate.Action.Extract;
            Label("CREW RV / RETURN HOME",new Vector3(0,2.17f,-6.12f),180,.1f,outside);
            var rvlight=new GameObject("RVLight").AddComponent<Light>();rvlight.transform.position=new Vector3(0,2.6f,-5.8f);rvlight.type=LightType.Point;rvlight.color=new Color(1,.67f,.35f);rvlight.range=9;rvlight.intensity=8;
            var moon=new GameObject("Moon").AddComponent<Light>();moon.type=LightType.Directional;moon.color=new Color(.32f,.48f,.59f);moon.intensity=.3f;moon.transform.rotation=Quaternion.Euler(52,-30,0);moon.shadows=LightShadows.Soft;
            var surface=world.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.08f;
            var doors=Object.FindObjectsByType<HospitalDoor>(FindObjectsSortMode.None);foreach(var door in doors)door.obstacle.enabled=false;
            surface.BuildNavMesh();if(!surface.navMeshData)throw new Exception("No hospital NavMesh");
            var navPath=Art+"HospitalNavMesh.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(old){EditorUtility.CopySerialized(surface.navMeshData,old);surface.RemoveData();Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=old;surface.AddData();EditorUtility.SetDirty(old);}
            else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            var diagnosis=new System.Text.StringBuilder();
            foreach(var r in world.GetComponentsInChildren<Renderer>().Take(12))diagnosis.AppendLine(r.name+" "+r.bounds);
            var triangles=NavMesh.CalculateTriangulation();diagnosis.AppendLine("Nav vertices="+triangles.vertices.Length);
            foreach(var point in new[]{new Vector3(0,.05f,1),new Vector3(-5.5f,.05f,6),new Vector3(-1.5f,.05f,6)}){
                bool found=NavMesh.SamplePosition(point,out var hit,2,NavMesh.AllAreas);diagnosis.AppendLine("Sample "+point+" found="+found+" at="+hit.position);
            }
            File.WriteAllText(Path.Combine(QA,"hospital_nav_probe.txt"),diagnosis.ToString());
            var diagnosticCamera=new GameObject("QA_Camera").AddComponent<Camera>();
            Capture(diagnosticCamera,new Vector3(0,1.6f,2),new Vector3(0,1.4f,20),"Hospital_NavProbe.png");Object.DestroyImmediate(diagnosticCamera.gameObject);
            var pathCheck=new NavMeshPath();
            foreach(int side in new[]{-1,1})for(int i=0;i<3;i++)if(!NavMesh.CalculatePath(new Vector3(0,.05f,1),new Vector3(side*3.1f,.05f,6+i*8),NavMesh.AllAreas,pathCheck)||pathCheck.status!=NavMeshPathStatus.PathComplete)throw new Exception("Unreachable room "+side+":"+i);
            foreach(var door in doors)door.obstacle.enabled=true;
            var actor=new GameObject("TheSurgeon");actor.transform.position=new Vector3(-5.5f,.05f,16.5f);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Models/CHR_Surgeon.fbx"));model.transform.SetParent(actor.transform,false);
            var animator=model.GetComponent<Animator>();if(animator)animator.enabled=false;
            foreach(var t in actor.GetComponentsInChildren<Transform>())t.gameObject.layer=10;
            var agent=actor.AddComponent<NavMeshAgent>();agent.radius=.28f;agent.height=2.3f;agent.speed=1.15f;agent.angularSpeed=120;agent.acceleration=5;agent.stoppingDistance=.28f;
            var shape=actor.AddComponent<CapsuleCollider>();shape.center=new Vector3(0,1.12f,0);shape.radius=.25f;shape.height=2.24f;
            var enemy=actor.AddComponent<Surgeon>();enemy.visual=model.transform;enemy.eye=new GameObject("SurgeonEye").transform;enemy.eye.SetParent(actor.transform,false);enemy.eye.localPosition=new Vector3(0,2.03f,.16f);enemy.eye.gameObject.layer=10;
            var route=new GameObject("SurgeonPatrol").transform;var points=new List<Transform>();
            foreach(var point in new[]{new Vector3(-5.5f,0,16.5f),new Vector3(-5.5f,0,6),new Vector3(0,0,6),new Vector3(0,0,22),new Vector3(5.5f,0,22),new Vector3(5.5f,0,14)}){
                var stop=new GameObject("PatrolStop").transform;stop.SetParent(route);stop.position=point;points.Add(stop);
            }
            enemy.patrol=points.ToArray();enemy.warning=Sound(actor,"SurgeonWarning",.8f);enemy.blade=Sound(actor,"Blade",.75f);enemy.footstep=Sound(actor,"Footstep",.7f);
            enemy.patrol=Array.Empty<Transform>();PrefabUtility.SaveAsPrefabAsset(actor,Root+"Prefabs/CHR_Surgeon.prefab");enemy.patrol=points.ToArray();
            var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/PF_CrewPlayer.prefab"));player.name="LocalCrew";player.transform.position=new Vector3(0,.08f,-3.5f);
            var motor=player.GetComponent<CrewMotor>();motor.footsteps=Sound(player,"Footstep",.35f);motor.firstPerson=true;
            var cameraItem=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/EQP_MainCam_01.prefab"));cameraItem.transform.position=new Vector3(.3f,1,-3.3f);motor.mainCam=cameraItem.GetComponent<MainCam>();
            motor.mainCam.impact=Sound(cameraItem,"Door",.25f);
            motor.viewCamera.clearFlags=CameraClearFlags.SolidColor;motor.viewCamera.backgroundColor=RenderSettings.fogColor;
            HUD(motor);new GameObject("RunSession").AddComponent<RunSession>();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/Hospital.unity");
            Capture(motor.viewCamera,new Vector3(0,1.6f,2),new Vector3(0,1.4f,20),"Hospital_Corridor.png");
            Capture(motor.viewCamera,new Vector3(-2.5f,1.6f,11.7f),new Vector3(-5.8f,1.2f,14.5f),"Hospital_OperatingRoom.png");
            Capture(motor.viewCamera,new Vector3(4.7f,2.2f,-2.4f),new Vector3(0,1.1f,-8),"Hospital_CrewRV.png");
        }
        internal static void HUD(CrewMotor motor)
        {
            var canvas=new GameObject("CrewHUD").AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            motor.status=BuildCrew.Text(canvas.transform,"Status","",23,new Vector2(0,1),new Vector2(0,1),new Vector2(42,-34),new Vector2(1400,100));
            motor.hint=BuildCrew.Text(canvas.transform,"Interaction","",25,new Vector2(.5f,.35f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1000,60));motor.hint.alignment=TextAnchor.MiddleCenter;
            var cross=BuildCrew.Text(canvas.transform,"Crosshair","·",32,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(30,40));cross.alignment=TextAnchor.MiddleCenter;
            BuildCrew.Text(canvas.transform,"Controls","WASD MOVE   SHIFT RUN   SPACE JUMP   E USE   F LIGHT   R RECORD   Q DROP   ESC CURSOR",17,Vector2.zero,Vector2.zero,new Vector2(42,24),new Vector2(1500,35));
            var review=new GameObject("RecordedTake",typeof(RectTransform),typeof(RawImage));review.transform.SetParent(canvas.transform,false);
            var rect=review.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1440,810);
            var take=motor.mainCam.GetComponent<CrewTake>();take.reviewImage=review.GetComponent<RawImage>();take.reviewImage.raycastTarget=false;review.SetActive(false);
            take.reviewLabel=BuildCrew.Text(canvas.transform,"TakeTimecode","",24,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-36),new Vector2(1000,80));take.reviewLabel.alignment=TextAnchor.UpperCenter;take.reviewLabel.gameObject.SetActive(false);
        }
        internal static void Capture(Camera camera,Vector3 position,Vector3 target,string name)
        {
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(QA,name),image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
        }
    }
}
