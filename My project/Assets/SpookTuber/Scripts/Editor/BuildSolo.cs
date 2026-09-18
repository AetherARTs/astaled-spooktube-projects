using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    public static class BuildSolo
    {
        const string Root="Assets/SpookTuber/",Env=Root+"Environment/";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static Transform world;
        static readonly List<Vector3> navigationChecks=new();
        static readonly Dictionary<string,Material> palette=new();
        static int gearIndex;
        public static void LightingProbe()
        {
            foreach(string scene in new[]{"ProductionHouse","Hospital"}){
                EditorSceneManager.OpenScene(Root+"Scenes/"+scene+".unity");
                var cam=Object.FindFirstObjectByType<CrewMotor>().viewCamera;
                Vector3 pos=scene=="Hospital"?new(1.5f,1.5f,9):new(1.9f,1.8f,3.6f),target=scene=="Hospital"?new(0,1.4f,34):new(-.2f,1.2f,13);
                BuildHospital.Capture(cam,pos,target,"probe_"+scene+"_base.png");
                foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))light.shadows=LightShadows.None;
                BuildHospital.Capture(cam,pos,target,"probe_"+scene+"_no_shadows.png");
                cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                BuildHospital.Capture(cam,pos,target,"probe_"+scene+"_no_grade.png");
                foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{if(!m)return m;var clone=new Material(m);clone.DisableKeyword("_NORMALMAP");clone.DisableKeyword("_METALLICSPECGLOSSMAP");clone.SetTexture("_BaseMap",null);clone.SetFloat("_Metallic",0);return clone;}).ToArray();
                BuildHospital.Capture(cam,pos,target,"probe_"+scene+"_flat.png");
                foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type!=LightType.Directional){light.intensity*=15;light.renderMode=LightRenderMode.ForcePixel;}
                BuildHospital.Capture(cam,pos,target,"probe_"+scene+"_strong.png");
                var diagnostic=new GameObject("ProbeRed").AddComponent<Light>();diagnostic.transform.position=pos+cam.transform.forward*3;diagnostic.type=LightType.Point;diagnostic.range=15;diagnostic.intensity=100;diagnostic.color=Color.red;
                BuildHospital.Capture(cam,pos,target,"probe_"+scene+"_red.png");
            }
            EditorApplication.Exit(0);
        }
        static GameObject Prop(string name,Vector3 pos,float yaw=0,bool collision=true)=>BuildHospital.Prop(name,pos,yaw,world,collision);
        static GameObject Box(string name,Vector3 pos,Vector3 size,string mat)
        {
            var go=BuildHospital.Box(name,pos,size,mat,world);var mesh=Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);mesh.name="MetreUV_"+name;
            var uv=mesh.uv;var vertices=mesh.vertices;var normals=mesh.normals;
            for(int i=0;i<uv.Length;i++){var p=Vector3.Scale(vertices[i],size);var n=normals[i];uv[i]=(Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y))*.5f;}
            mesh.uv=uv;mesh.RecalculateTangents();go.GetComponent<MeshFilter>().sharedMesh=mesh;return go;
        }
        static void Sign(string text,Vector3 pos,float yaw=0,float size=.15f)
        {
            BuildHospital.Label(text,pos,yaw,size,world);var rotation=Quaternion.Euler(0,yaw,0);var lines=text.Split('\n');
            var plate=Box("Sign backing",pos+rotation*Vector3.forward*.035f,new Vector3(lines.Max(l=>l.Length)*size*.78f+.15f,lines.Length*size*2.3f+.1f,.04f),"H_DarkMetal");plate.transform.rotation=rotation;
        }
        [MenuItem("SpookTuber/Build Solo production scenes")]
        public static void Build()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try{
                AssetDatabase.Refresh();Materials();PlayerPrefab();House();Hospital();
                var paths=new[]{"ProductionHouse.unity","Hospital.unity","Legacy/ProductionHouse_v4.unity","Legacy/Hospital_v4.unity"}.Select(n=>Root+"Scenes/"+n).ToArray();
                EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();
                AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(QA,"v5_scene_build.txt"),"PASS / enlarged home and hospital / native NavMesh routes / archived v4 replay scenes\n");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);File.WriteAllText(Path.Combine(QA,"v5_scene_build.txt"),e.ToString());if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
        static void Materials()
        {
            Directory.CreateDirectory(Env+"Materials");
            foreach(var path in Directory.GetFiles(Env+"Textures","*.png")){
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;
                if(path.Contains("Normal")){importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;}
                if(path.Contains("Mask"))importer.sRGBTexture=false;importer.SaveAndReimport();
            }
            var colors=new Dictionary<string,Color>{
                {"H_Plaster",new(.46f,.47f,.40f)},{"H_GreenPaint",new(.15f,.25f,.22f)},{"H_Tile",new(.45f,.49f,.45f)},
                {"H_Grout",new(.075f,.09f,.085f)},{"H_Steel",new(.29f,.33f,.33f)},{"H_DarkMetal",new(.045f,.057f,.06f)},
                {"H_Rust",new(.29f,.12f,.055f)},{"H_Linen",new(.53f,.52f,.43f)},{"H_Coat",new(.5f,.49f,.41f)},
                {"H_Rubber",new(.019f,.023f,.028f)},{"H_Glass",new(.015f,.04f,.042f)},{"H_Light",new(.77f,.84f,.7f)},
                {"H_Red",new(.40f,.055f,.027f)},{"H_Paper",new(.72f,.68f,.55f)},{"H_Amber",new(.78f,.47f,.13f)},
                {"P_Wood",new(.29f,.18f,.095f)},{"P_Fabric",new(.085f,.16f,.19f)},{"P_Plaster",new(.61f,.55f,.44f)},
                {"P_Ivory",new(.77f,.72f,.61f)},{"P_Brass",new(.49f,.32f,.12f)},{"P_Blue",new(.14f,.28f,.34f)},
                {"P_Leaf",new(.12f,.22f,.12f)},{"P_Concrete",new(.26f,.29f,.28f)},{"P_Screen",new(.008f,.012f,.016f)}};
            foreach(var pair in colors){
                string name=pair.Key;var mat=BuildHospital.Mat(name,pair.Value,name.Contains("Steel")||name.Contains("Metal")||name.Contains("Brass")?.6f:0);palette[name]=mat;
                string type=name.Contains("Wood")?"Wood":name.Contains("Fabric")||name.Contains("Linen")||name.Contains("Coat")?"Fabric":name.Contains("Steel")||name.Contains("Metal")||name.Contains("Rust")?"Metal":name.Contains("Tile")?"Tile":name.Contains("Concrete")||name.Contains("Grout")?"Concrete":"Plaster";
                if(type=="Plaster"&&name.StartsWith("P_"))type="CleanPlaster";
                if(name.Contains("Glass")||name.Contains("Screen")||name.Contains("Light")||name.Contains("Rubber")){mat.SetTexture("_BaseMap",null);mat.SetTexture("_BumpMap",null);mat.DisableKeyword("_NORMALMAP");}
                else{
                    mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Env+"Textures/TEX_"+type+"_Albedo.png"));
                    mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Env+"Textures/TEX_"+type+"_Normal.png"));mat.SetFloat("_BumpScale",.5f);mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Env+"Textures/TEX_"+type+"_Mask.png"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                if(name=="P_Screen"){mat.SetFloat("_Smoothness",.72f);mat.SetColor("_BaseColor",new Color(.005f,.008f,.011f));}
                EditorUtility.SetDirty(mat);
            }
            foreach(var path in Directory.GetFiles(Env+"Models","*.fbx")){
                var importer=(ModelImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.importNormals=ModelImporterNormals.Import;
                foreach(var pair in palette)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);importer.SaveAndReimport();
            }
        }
        static void PlayerPrefab()
        {
            string path=Root+"Prefabs/PF_CrewPlayer.prefab";var player=PrefabUtility.LoadPrefabContents(path);
            player.transform.localScale=Vector3.one*.88f;
            var motor=player.GetComponent<CrewMotor>();motor.walkSpeed=2.65f;motor.sprintSpeed=5.25f;motor.jumpHeight=.95f;
            var cc=player.GetComponent<CharacterController>();cc.radius=.20f;cc.stepOffset=.23f;cc.slopeLimit=48;cc.skinWidth=.018f;
            foreach(var rb in player.GetComponent<CrewBody>().ragdoll)rb.interpolation=RigidbodyInterpolation.None;
            var phone=player.GetComponent<CrewPhone>();if(!phone)phone=player.AddComponent<CrewPhone>();
            var wrapper=new GameObject("EQP_Phone");var phoneModel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Env+"Models/EQP_Phone.fbx"));phoneModel.transform.SetParent(wrapper.transform,false);phoneModel.transform.localRotation*=Quaternion.Euler(0,0,180);
            phone.phoneModel=PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"Prefabs/EQP_Phone.prefab");Object.DestroyImmediate(wrapper);
            PrefabUtility.SaveAsPrefabAsset(player,path);PrefabUtility.UnloadPrefabContents(player);
            path=Root+"Prefabs/EQP_MainCam_01.prefab";var camera=PrefabUtility.LoadPrefabContents(path);var item=camera.GetComponent<MainCam>();item.heldOffset=new Vector3(.14f,-.18f,.35f);item.aimedOffset=new Vector3(.02f,-.07f,.33f);
            item.rightGrip.localPosition=new Vector3(.18f,-.055f,.005f);item.leftGrip.localPosition=new Vector3(-.055f,-.15f,.02f);
            PrefabUtility.SaveAsPrefabAsset(camera,path);PrefabUtility.UnloadPrefabContents(camera);
        }
        static void Begin(string name,bool hospital)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);world=new GameObject(name).transform;gearIndex=0;navigationChecks.Clear();
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=hospital?new Color(.16f,.19f,.20f):new Color(.36f,.34f,.30f);
            RenderSettings.fog=hospital;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=14;RenderSettings.fogEndDistance=60;RenderSettings.fogColor=new Color(.035f,.050f,.055f);
            var sun=new GameObject(hospital?"Moon":"Dusk").AddComponent<Light>();sun.type=LightType.Directional;sun.color=hospital?new Color(.37f,.50f,.63f):new Color(1,.74f,.49f);sun.intensity=hospital?.28f:.65f;sun.transform.rotation=Quaternion.Euler(47,-28,0);sun.shadows=LightShadows.Soft;
            var volume=new GameObject("ProductionGrade").AddComponent<Volume>();volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);var color=profile.Add<ColorAdjustments>(true);color.contrast.Override(3);color.saturation.Override(-6);color.postExposure.Override(.35f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.17f);vignette.smoothness.Override(.65f);
            string profilePath=Env+"Materials/"+(hospital?"Hospital":"Home")+"Grade.asset";var existing=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(existing){foreach(var component in existing.components)Object.DestroyImmediate(component,true);existing.components.Clear();foreach(var component in profile.components){AssetDatabase.AddObjectToAsset(component,existing);existing.components.Add(component);}Object.DestroyImmediate(profile);profile=existing;}else{AssetDatabase.CreateAsset(profile,profilePath);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);}
            volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
        }
        static void Zone(string name,Vector3 center,Vector3 size){var go=new GameObject("Zone_"+name);go.transform.SetParent(world);go.transform.position=center;var zone=go.AddComponent<MapZone>();zone.label=name;zone.size=size;}
        static void Practical(Vector3 pos,bool warm=false,bool lit=true)
        {
            var fixture=Prop(warm?"P_PendantLamp":"H_Fluorescent",pos,0,false);
            if(!lit){foreach(var renderer in fixture.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m.name=="H_Light"?palette["H_DarkMetal"]:m).ToArray();return;}
            var lamp=new GameObject("Practical_"+pos).AddComponent<Light>();lamp.transform.SetParent(world);lamp.transform.position=pos-Vector3.up*.3f;lamp.transform.rotation=Quaternion.Euler(90,0,0);lamp.type=LightType.Spot;lamp.spotAngle=135;lamp.innerSpotAngle=95;lamp.range=warm?11:12;lamp.intensity=warm?70:90;
            lamp.color=warm?new Color(1,.74f,.44f):new Color(.72f,.83f,.81f);lamp.shadows=LightShadows.Soft;lamp.shadowBias=.02f;lamp.shadowNormalBias=.04f;lamp.cullingMask=~(1<<9);
        }
        static void Floor(Vector3 center,float width,float depth,bool house=false)
        {
            Box("Floor_"+center,center-Vector3.up*.12f,new Vector3(width,.24f,depth),house?"P_Wood":"H_Grout");
            // One surface per 4 m module, deliberately no colliders on tile bevels.
            if(!house)for(float x=center.x-width/2+2;x<center.x+width/2;x+=4)for(float z=center.z-depth/2+2;z<center.z+depth/2;z+=4){
                var floor=Prop("H_Floor_4m",new Vector3(x,center.y,z),0,false);floor.transform.localScale=new Vector3(Mathf.Min(4,center.x+width/2-(x-2))/4,1,Mathf.Min(4,center.z+depth/2-(z-2))/4);
            }
        }
        static void Ceiling(Vector3 center,float width,float depth,float height=3.6f)
        {
            for(float x=center.x-width/2+2;x<center.x+width/2;x+=4)for(float z=center.z-depth/2+2;z<center.z+depth/2;z+=4){var tile=Prop("H_CeilingGrid",new Vector3(x,center.y+height,z),0,false);tile.transform.localScale=new Vector3(Mathf.Min(4,center.x+width/2-(x-2))/4,1,Mathf.Min(4,center.z+depth/2-(z-2))/4);}
        }
        static void Wall(Vector3 center,float width,float yaw=0,bool house=false)
        {
            if(house){var wall=Box("Plaster wall",center+Vector3.up*1.8f,new Vector3(width,3.6f,.23f),"P_Plaster");wall.transform.rotation=Quaternion.Euler(0,yaw,0);
                var kick=Box("Skirting",center+Vector3.up*.11f,new Vector3(width,.20f,.28f),"P_Wood");kick.transform.rotation=wall.transform.rotation;return;}
            int modules=Mathf.CeilToInt(width/4);float span=width/modules;var right=Quaternion.Euler(0,yaw,0)*Vector3.right;
            for(int i=0;i<modules;i++){var go=Prop("H_Wall_Detailed",center+right*(-width/2+span*(i+.5f)),yaw);go.transform.localScale=new Vector3(span/4,1,1);}
        }
        static void Doorway(Vector3 position,float yaw,string name,bool open=false)
        {
            var root=new GameObject("Door_"+name);root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            BuildHospital.Prop("H_DoorPocket",position,yaw,root.transform);
            var leaf=BuildHospital.Prop("H_SlidingLeaf",position,yaw,root.transform);var door=root.AddComponent<HospitalDoor>();door.leaf=leaf.transform;door.slideOffset=new Vector3(-2.1f,0,0);door.sound=BuildHospital.Sound(root,"Door",.6f);
            var obstacle=root.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0,1.2f,0);obstacle.size=new Vector3(2,2.4f,.2f);obstacle.carving=true;obstacle.carveOnlyStationary=false;door.obstacle=obstacle;
            var header=Box("DoorHeader",position+Vector3.up*3.10f,new Vector3(2.2f,1,.26f),"H_Plaster");header.transform.rotation=root.transform.rotation;
        }
        static void SplitWall(Vector3 center,float width,float yaw,string name,bool sliding=true,bool house=false)
        {
            float gap=sliding?2.2f:3;var right=Quaternion.Euler(0,yaw,0)*Vector3.right;
            Wall(center-right*(width+gap)/4,(width-gap)/2,yaw,house);Wall(center+right*(width+gap)/4,(width-gap)/2,yaw,house);
            if(sliding)Doorway(center,yaw,name);else{var header=Box("Open portal",center+Vector3.up*3.27f,new Vector3(gap,.66f,.25f),house?"P_Plaster":"H_Plaster");header.transform.rotation=Quaternion.Euler(0,yaw,0);}
        }
        static PhysicsProp Movable(string name,Vector3 pos,float yaw,float mass,bool climb=false)
        {
            var go=Prop(name,pos,yaw,false);var bounds=new Bounds();bool first=true;
            foreach(var mesh in go.GetComponentsInChildren<MeshFilter>()){
                var b=mesh.sharedMesh.bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){
                    var point=go.transform.InverseTransformPoint(mesh.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            var shape=go.AddComponent<BoxCollider>();shape.center=bounds.center;shape.size=bounds.size;
            var rb=go.AddComponent<Rigidbody>();rb.mass=mass;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearDamping=.35f;rb.angularDamping=.8f;
            var prop=go.AddComponent<PhysicsProp>();prop.mantle=climb;prop.weight=mass<=5?PhysicsProp.WeightClass.Light:mass<=18?PhysicsProp.WeightClass.Medium:PhysicsProp.WeightClass.Heavy;prop.impact=BuildHospital.Sound(go,"Door",.4f);
            return prop;
        }
        static CarryItem Gear(CarryItem.Kind kind,Vector3 pos)
        {
            string model=kind==CarryItem.Kind.GravityGlove?"EQP_GravityGlove":kind==CarryItem.Kind.ProductionLight?"EQP_ProductionLight":"EQP_Noisemaker";
            var prop=Movable(model,pos,0,1);var go=prop.gameObject;Object.DestroyImmediate(prop);
            var gear=go.AddComponent<CarryItem>();gear.kind=kind;gear.itemId=SceneManager.GetActiveScene().name+"-"+model+"-"+gearIndex++;gear.displayName=kind==CarryItem.Kind.GravityGlove?"Gravity Glove":kind==CarryItem.Kind.ProductionLight?"Work Light":"Noisemaker";
            gear.rightGrip=new GameObject("RightGrip").transform;gear.rightGrip.SetParent(go.transform,false);gear.rightGrip.localPosition=new Vector3(.04f,-.035f,-.045f);
            if(kind==CarryItem.Kind.ProductionLight){var light=new GameObject("WorkLight").AddComponent<Light>();light.transform.SetParent(go.transform,false);light.transform.localPosition=new Vector3(0,.18f,.12f);light.type=LightType.Spot;light.spotAngle=85;light.range=15;light.intensity=7;light.shadows=LightShadows.Soft;light.enabled=false;gear.workLight=light;}
            if(kind==CarryItem.Kind.Noisemaker){var source=go.AddComponent<AudioSource>();source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Hospital/Audio/SurgeonWarning.wav");source.spatialBlend=1;source.volume=.65f;source.playOnAwake=false;}
            return gear;
        }
        static CrewMotor Crew(Vector3 position,Vector3 cameraPosition)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/PF_CrewPlayer.prefab"));go.name="LocalCrew";go.transform.position=position;
            var motor=go.GetComponent<CrewMotor>();motor.footsteps=BuildHospital.Sound(go,"Footstep",.3f);motor.viewCamera.clearFlags=CameraClearFlags.SolidColor;motor.viewCamera.backgroundColor=RenderSettings.fog?RenderSettings.fogColor:new Color(.13f,.17f,.23f);
            motor.viewCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var camera=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/EQP_MainCam_01.prefab"));camera.transform.position=cameraPosition;motor.mainCam=camera.GetComponent<MainCam>();motor.mainCam.impact=BuildHospital.Sound(camera,"Door",.3f);
            BuildHospital.HUD(motor);new GameObject("RunSession").AddComponent<RunSession>();return motor;
        }
        static void Npc(string model,Vector3 pos,float yaw,HomeNpc.Role role)
        {
            var go=Prop(model,pos,yaw,false);var npc=go.AddComponent<HomeNpc>();npc.role=role;npc.displayName=role==HomeNpc.Role.Bobby?"Bobby / footage handoff":"Supply Mart / buy and sell";
            var shape=go.AddComponent<CapsuleCollider>();shape.height=role==HomeNpc.Role.Bobby?1.9f:1.65f;shape.radius=.38f;shape.center=Vector3.up*shape.height/2;
            var nodes=go.GetComponentsInChildren<Transform>();npc.head=nodes.First(t=>t.name==model+"_BobbyHead");npc.leftArm=nodes.First(t=>t.name==model+"_BobbyLeftArm");npc.rightArm=nodes.First(t=>t.name==model+"_BobbyRightArm");
        }
        static void House()
        {
            Begin("ProductionHome",false);
            Floor(new Vector3(0,0,3),30,30,true);Ceiling(new Vector3(0,0,3),30,30);
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())if(renderer.GetComponentInParent<MeshFilter>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m.name=="H_Plaster"?palette["P_Plaster"]:m).ToArray();
            foreach(float x in new[]{-15f,15f})Wall(new Vector3(x,0,3),30,90,true);
            Wall(new Vector3(0,0,18),30,0,true);Wall(new Vector3(0,0,-12),30,0,true);
            foreach(var pos in new[]{new Vector3(-9,0,17.75f),new Vector3(9,0,17.75f),new Vector3(-11,0,-11.75f),new Vector3(6,0,-11.75f)}){
                var window=Prop("H_WindowBay",pos,pos.z<0?180:0,false);foreach(var renderer in window.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m.name=="H_Plaster"?palette["P_Plaster"]:m).ToArray();
            }
            // Central 6 m hall links the living apartment, private rooms and workshop.
            foreach(int side in new[]{-1,1}){
                SplitWall(new Vector3(side*3,0,10),16,90,"HomeNorth"+side,false,true);
                SplitWall(new Vector3(side*3,0,-5),14,90,"HomeSouth"+side,false,true);
                SplitWall(new Vector3(side*9,0,2),12,0,"HomeCross"+side,false,true);
            }
            SplitWall(new Vector3(9,0,-4),12,0,"BathEntry",false,true);
            Zone("Living / kitchen",new Vector3(0,1.6f,10),new Vector3(6,3.5f,16));Zone("Bedroom",new Vector3(-9,1.6f,10),new Vector3(12,3.5f,16));
            Zone("Equipment",new Vector3(9,1.6f,10),new Vector3(12,3.5f,16));Zone("Bobby / lounge",new Vector3(-9,1.6f,-5),new Vector3(12,3.5f,14));
            Zone("Supply Mart",new Vector3(9,1.6f,-8),new Vector3(12,3.5f,8));Zone("Bathroom",new Vector3(9,1.6f,-1),new Vector3(12,3.5f,6));Zone("Main hall",new Vector3(0,1.6f,0),new Vector3(6,3.5f,8));
            Prop("P_KitchenRun",new Vector3(0,0,17.45f));Prop("P_Fridge",new Vector3(2.4f,0,17.4f));Prop("P_DiningTable",new Vector3(0,0,13.5f));
            foreach(float x in new[]{-1.35f,1.35f})for(int j=0;j<2;j++)Prop("P_DiningChair",new Vector3(x,0,13.15f+j*.8f),x<0?90:-90);
            Prop("P_Sofa",new Vector3(0,0,8.7f));Prop("P_CoffeeTable",new Vector3(0,0,6.7f));
            var rug=Box("Living rug",new Vector3(0,.016f,7),new Vector3(4.8f,.025f,4.4f),"P_Fabric");Object.DestroyImmediate(rug.GetComponent<Collider>());
            for(int i=0;i<3;i++){Prop("P_BunkBed",new Vector3(-12+i*3,0,15.8f));Prop("H_Locker",new Vector3(-12+i*1.15f,0,3.0f),180);}
            Prop("P_CoffeeTable",new Vector3(-8,0,9));Prop("P_Sofa",new Vector3(-8,0,11));Prop("P_Bookshelf",new Vector3(-14.6f,0,7),90);
            var wardrobe=Box("CrewWardrobe",new Vector3(-4.4f,1,15.6f),new Vector3(1.4f,2,.6f),"H_DarkMetal");wardrobe.AddComponent<CrewWardrobe>();Sign("CREW / WARDROBE",new Vector3(-4.4f,2.3f,15.2f));
            Prop("P_EditDesk",new Vector3(-11,0,-2.7f));Prop("P_OfficeChair",new Vector3(-11,0,-4.1f));Npc("NPC_Bobby",new Vector3(-9,0,-3),180,HomeNpc.Role.Bobby);
            Prop("P_Bookshelf",new Vector3(-14.5f,0,-2),90);Prop("P_Sofa",new Vector3(-11,0,-8.5f),180);Prop("P_CoffeeTable",new Vector3(-11,0,-6.5f));
            Sign("BOBBY / EDIT & RELEASE",new Vector3(-11,2.35f,-2.5f));
            for(int i=0;i<3;i++)Prop("P_EquipmentRack",new Vector3(6+i*3.2f,0,17.4f));
            Prop("P_EditDesk",new Vector3(10,0,10));Prop("P_DiningTable",new Vector3(7,0,6));
            Gear(CarryItem.Kind.GravityGlove,new Vector3(7,.98f,6));Gear(CarryItem.Kind.ProductionLight,new Vector3(7.6f,1.05f,6));Gear(CarryItem.Kind.Noisemaker,new Vector3(6.4f,1.05f,6));
            Sign("EQUIPMENT / 3 QUICK SLOTS",new Vector3(9,2.65f,17.5f));
            for(int i=0;i<3;i++){Prop("H_Sink",new Vector3(6+i*2.5f,0,1.4f));Prop("H_Toilet",new Vector3(6+i*2.5f,0,-3.1f),180);}
            Prop("H_ReceptionDesk",new Vector3(9,0,-8.3f));Npc("NPC_SupplyClerk",new Vector3(9,0,-9.55f),180,HomeNpc.Role.Supply);
            for(int i=0;i<3;i++)Prop("P_EquipmentRack",new Vector3(6+i*3.2f,0,-11.4f));Sign("SUPPLY MART / CREDIT ONLY",new Vector3(9,2.65f,-11.2f),180);
            // Garage extension has its own full-height shell and a broad connection.
            Floor(new Vector3(-23,0,-5),16,14);Ceiling(new Vector3(-23,0,-5),16,14,4.5f);
            // Replace the relevant west wall span with an open garage portal.
            foreach(var go in world.Cast<Transform>().Where(t=>(t.name=="Plaster wall"||t.name=="Skirting")&&Mathf.Abs(t.position.x+15)<.1f).ToArray())Object.DestroyImmediate(go.gameObject);
            Wall(new Vector3(-15,0,10),16,90,true);SplitWall(new Vector3(-15,0,-5),14,90,"Garage",false,true);
            Wall(new Vector3(-31,0,-5),14,90);Wall(new Vector3(-23,0,-12),16);Wall(new Vector3(-23,0,2),16);
            var rv=Prop("H_CrewRV",new Vector3(-24,0,-5),90);var panel=Box("EntryDoor",new Vector3(-20.7f,1.2f,-4.5f),new Vector3(.18f,.42f,.65f),"H_Amber");panel.AddComponent<MissionGate>().action=MissionGate.Action.Depart;
            Sign("CREW RV / HOSPITAL",new Vector3(-20.4f,2.4f,-5),90);Zone("Garage",new Vector3(-23,2,-5),new Vector3(16,4,14));
            for(int i=0;i<4;i++)Movable("H_SupplyCrate",new Vector3(-29+i*.9f,.02f,.8f),i*9,4);
            Prop("P_EquipmentRack",new Vector3(-30.5f,0,-8),90);
            foreach(var pos in new[]{new Vector3(0,3.1f,5),new Vector3(0,3.1f,13),new Vector3(-9,3.1f,10),new Vector3(-9,3.1f,-5),new Vector3(9,3.1f,10),new Vector3(9,3.1f,-8),new Vector3(9,3.1f,-1),new Vector3(0,3.1f,-6)})Practical(pos,true);
            Practical(new Vector3(-23,4.1f,-5));Practical(new Vector3(-28,4.1f,-5));
            foreach(var pos in new[]{new Vector3(-2.4f,0,3.8f),new Vector3(2.3f,0,16),new Vector3(-13,0,-10),new Vector3(13,0,4),new Vector3(13,0,-6)})Prop("P_Plant",pos);
            Sign("S P O O K T U B E R\nPRODUCTION HOUSE",new Vector3(0,2.4f,17.5f),0,.18f);
            var crew=Crew(new Vector3(0,.06f,2.5f),new Vector3(6.8f,.94f,5.6f));
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/ProductionHouse.unity");
            BuildHospital.Capture(crew.viewCamera,new Vector3(1.9f,1.8f,3.6f),new Vector3(-.2f,1.2f,13),"v5_Home_Living.png");
            BuildHospital.Capture(crew.viewCamera,new Vector3(-5.2f,1.7f,-7.2f),new Vector3(-11,1.1f,-2.3f),"v5_Home_Bobby.png");
            BuildHospital.Capture(crew.viewCamera,new Vector3(-16.3f,2.0f,0),new Vector3(-25,1.3f,-5),"v5_Home_Garage.png");
        }
        static void Hospital()
        {
            Begin("HospitalWorld",true);
            Floor(new Vector3(0,0,34),37,68);Ceiling(new Vector3(0,0,34),37,68);
            Zone("Reception",new Vector3(0,1.5f,6),new Vector3(37,3,12));
            SplitWall(new Vector3(0,0,0),37,0,"MainEntrance",false);Wall(new Vector3(0,0,68),37);
            foreach(int side in new[]{-1,1})Wall(new Vector3(side*18.5f,0,34),68,90);
            var names=new[]{"WARD A","OPERATING THEATRE","PHARMACY","MORGUE","WAITING / TRIAGE","RADIOLOGY","ADMINISTRATION","MAINTENANCE"};
            var centers=new[]{18f,30f,46f,58f};
            foreach(int side in new[]{-1,1}){
                float x=side*8.5f;
                for(int i=0;i<4;i++){
                    float z=centers[i];string name=names[(side<0?0:4)+i];
                    SplitWall(new Vector3(side*2.5f,0,z),12,90,"Spine"+side+i);
                    SplitWall(new Vector3(side*14.5f,0,z),12,90,"Outer"+side+i);
                    SplitWall(new Vector3(x,0,z-6),12,0,"Cross"+side+i);
                    if(i==1||i==3)SplitWall(new Vector3(x,0,z+6),12,0,"Rear"+side+i);
                    Zone(name,new Vector3(x,1.5f,z),new Vector3(12,3,12));navigationChecks.Add(new Vector3(x,.05f,z));
                    Sign(name,new Vector3(side*2.29f,2.9f,z),side*90,.13f);
                    Practical(new Vector3(x-2,3.35f,z-2));Practical(new Vector3(x+2.5f,3.35f,z+2),false,i!=2);
                    Prop("H_PipeRun",new Vector3(x,3.25f,z-5),0,false);Prop("H_Radiator",new Vector3(x+4.6f,0,z+5.45f));
                    Prop("H_PaperDebris",new Vector3(x-.9f,.025f,z+1.4f),i*47,false);
                    if(side<0&&i==0){
                        for(int bed=0;bed<4;bed++){float bx=x+(bed<2?-3.6f:3.6f),bz=z+(bed%2==0?-3:3);Prop("H_Bed",new Vector3(bx,0,bz),90);Prop("H_IVStand",new Vector3(bx+.9f,0,bz+.7f));Prop("H_PrivacyScreen",new Vector3(bx,0,bz+1.65f),90);}
                        Movable("H_MedicalCart",new Vector3(x+1.7f,0,z-4.3f),25,7);
                    }else if(side<0&&i==1){
                        Prop("H_OperatingTable",new Vector3(x,0,z));Prop("H_SurgicalLamp",new Vector3(x-1.2f,0,z));Prop("H_Sink",new Vector3(x-3.8f,0,z+5.4f));
                        for(int j=0;j<3;j++)Prop("H_MedicineCabinet",new Vector3(x+2+j,0,z+5.5f));
                        var cart=Movable("H_MedicalCart",new Vector3(x+2,0,z-1),0,9);var monitor=Prop("H_Monitor",new Vector3(x+2,.94f,z-1),0,false);monitor.transform.SetParent(cart.transform,true);
                        var hum=BuildHospital.Sound(world.gameObject,"SurgicalHum",.12f,true);hum.transform.position=new Vector3(x,1,z);
                    }else if(side<0&&i==2){
                        for(int j=0;j<4;j++){Prop("H_MedicineCabinet",new Vector3(x-4+j*2.4f,0,z+5.4f));Prop("P_EquipmentRack",new Vector3(x-4+j*2.4f,0,z-4));}
                        for(int j=0;j<4;j++)Movable("H_SupplyCrate",new Vector3(x-3+j*1.1f,0,z+1.5f),j*12,3);
                    }else if(side<0){
                        Prop("H_MorgueBank",new Vector3(x-3.7f,0,z+5.45f));Prop("H_MorgueBank",new Vector3(x+2.3f,0,z+5.45f));Prop("H_OperatingTable",new Vector3(x,0,z-2));Prop("H_Sink",new Vector3(x-5.4f,0,z),90);
                        Movable("H_Wheelchair",new Vector3(x+3.6f,0,z-3.2f),-30,12);
                    }else if(i==0){
                        for(int j=0;j<3;j++){Prop("H_WaitingBench",new Vector3(x-2+j*2.4f,0,z+3.8f));Prop("H_WaitingBench",new Vector3(x-2+j*2.4f,0,z-3.8f),180);}
                        Movable("H_Wheelchair",new Vector3(x+3.7f,0,z),-45,12);Prop("H_MedicineCabinet",new Vector3(x-5.4f,0,z),90);
                    }else if(i==1){
                        Prop("H_Radiology",new Vector3(x-2,0,z));Prop("H_Radiology",new Vector3(x+3,0,z));Prop("H_PrivacyScreen",new Vector3(x+.4f,0,z+1));Prop("H_Monitor",new Vector3(x-5,.8f,z+3),90);Prop("H_MedicalCart",new Vector3(x-5,0,z+3),90);
                    }else if(i==2){
                        for(int j=0;j<3;j++){Prop("P_EditDesk",new Vector3(x-3+j*3.1f,0,z+4));Prop("P_OfficeChair",new Vector3(x-3+j*3.1f,0,z+2.6f),j*15);}
                        Prop("P_Bookshelf",new Vector3(x+5.5f,0,z),-90);var ledge=Prop("H_MantleLedge",new Vector3(x-2,0,z-3));ledge.AddComponent<MantleSurface>();Prop("H_Rubble",new Vector3(x-2,0,z-4),0,false);
                    }else{
                        Prop("P_EquipmentRack",new Vector3(x-4,0,z+4.8f));for(int j=0;j<4;j++)Movable("H_SupplyCrate",new Vector3(x-4+j*1.2f,0,z-3.9f),j*18,5);
                        var ledge=Prop("H_MantleLedge",new Vector3(x+1,0,z));ledge.AddComponent<MantleSurface>();Prop("H_Rubble",new Vector3(x+1,0,z+1),35,false);
                    }
                }
                Zone("Outer corridor "+side,new Vector3(side*16.5f,1.5f,38),new Vector3(4,3,52));
                for(int i=0;i<7;i++){Practical(new Vector3(side*16.5f,3.35f,15+i*8),false,i%3!=1);Prop("H_PipeRun",new Vector3(side*17.5f,3.12f,16+i*8),90,false);}
            }
            Zone("Main corridor",new Vector3(0,1.5f,38),new Vector3(5,3,52));Zone("Cross passage",new Vector3(0,1.5f,38),new Vector3(37,3,4));Zone("Rear passage",new Vector3(0,1.5f,66),new Vector3(37,3,4));
            for(int i=0;i<8;i++)Practical(new Vector3(0,3.35f,10+i*7),false,i!=4&&i!=6);
            for(int j=0;j<3;j++){Practical(new Vector3(-10+j*10,3.35f,5));Prop("H_PaperDebris",new Vector3(-9+j*8,.02f,5),j*27,false);}
            Prop("H_ReceptionDesk",new Vector3(-8,0,5),180);Prop("H_Monitor",new Vector3(-8,1.1f,5),180,false);Prop("P_OfficeChair",new Vector3(-8,0,6.6f));
            for(int i=0;i<3;i++)Prop("H_WaitingBench",new Vector3(7+i*3.5f,0,8));Movable("H_Wheelchair",new Vector3(13,0,3),35,12);
            Sign("ST. MERIDIAN HOSPITAL\nRECEPTION",new Vector3(-8,2.6f,11.7f),0,.19f);Sign("WARD A  /  THEATRE  /  RADIOLOGY  >",new Vector3(0,3.0f,11.7f),0,.13f);Sign("EXIT / RV",new Vector3(0,2.8f,.25f),180,.18f);
            // A real low ledge route in the service room supports mantle, jump and crouch escape.
            SplitWall(new Vector3(10,0,57),5,0,"Service mantle",false);var low=Box("Service low opening",new Vector3(10,.4f,57),new Vector3(2,.8f,1),"P_Concrete");low.AddComponent<MantleSurface>();
            // Exterior approach and a readable hospital facade replace the enclosed tiny forecourt.
            Floor(new Vector3(0,0,-12),47,24);Zone("Ambulance court",new Vector3(0,2,-12),new Vector3(47,4,24));
            foreach(int side in new[]{-1,1}){Wall(new Vector3(side*23.5f,0,-12),24,90);Prop("P_Plant",new Vector3(side*4.5f,0,-2));}
            Wall(new Vector3(0,0,-24),47);var rv=Prop("H_CrewRV",new Vector3(0,0,-16),180);
            var extraction=Box("RVDeparturePanel",new Vector3(0,1.1f,-13.6f),new Vector3(.65f,.4f,.14f),"H_Amber");extraction.AddComponent<MissionGate>().action=MissionGate.Action.Extract;
            Sign("CREW RV / RETURN + RECHARGE",new Vector3(0,2.4f,-13.6f),180,.12f);
            var warm=new GameObject("RVSafetyLamp").AddComponent<Light>();warm.type=LightType.Point;warm.transform.position=new Vector3(0,3,-12);warm.range=12;warm.intensity=7;warm.color=new Color(1,.67f,.36f);
            for(int level=1;level<=3;level++)for(int bay=0;bay<8;bay++)Prop("H_WindowBay",new Vector3(-16+bay*4.5f,level*3.6f,0));
            Box("UpperFacade",new Vector3(0,8,1),new Vector3(37,9,1.2f),"H_Plaster");Sign("S T .  M E R I D I A N",new Vector3(0,3.45f,-.25f),0,.28f);
            // Bake only static architecture; movable props retain physical gameplay collisions.
            BakeNavigation();
            var enemy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/CHR_Surgeon.prefab"));enemy.name="TheSurgeon";enemy.transform.position=new Vector3(-8.5f,.05f,30);
            var surgeon=enemy.GetComponent<Surgeon>();surgeon.sightRange=15;var route=new GameObject("SurgeonPatrol").transform;
            surgeon.patrol=new[]{new Vector3(-8.5f,0,30),new Vector3(0,0,18),new Vector3(8.5f,0,18),new Vector3(16.5f,0,38),new Vector3(8.5f,0,46),new Vector3(0,0,58),new Vector3(-8.5f,0,58),new Vector3(-16.5f,0,38)}.Select(p=>{var point=new GameObject("PatrolStop").transform;point.SetParent(route);point.position=p;return point;}).ToArray();
            var crew=Crew(new Vector3(0,.06f,-10.8f),new Vector3(.2f,1,-10.5f));Gear(CarryItem.Kind.GravityGlove,new Vector3(-.4f,.20f,-10.2f));Gear(CarryItem.Kind.ProductionLight,new Vector3(.7f,.20f,-10.2f));Gear(CarryItem.Kind.Noisemaker,new Vector3(1.1f,.20f,-10.2f));
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/Hospital.unity");
            int tracks=SceneManager.GetActiveScene().GetRootGameObjects().Where(g=>!g.GetComponent<Canvas>()).Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
            if(tracks>2700)throw new Exception("Hospital recording budget exceeded: "+tracks);
            File.WriteAllText(Path.Combine(QA,"v5_hospital_metrics.txt"),$"Building 37 x 68 m / 2516 square metres\nExterior 47 x 24 m\nCentral corridor 5 m / outer corridors 4 m\n8 clinical rooms, reception, loop passages\nTransforms before runtime UI: {tracks}\nPlayer scale 0.88\n");
            BuildHospital.Capture(crew.viewCamera,new Vector3(1.5f,1.5f,9),new Vector3(0,1.4f,34),"v5_Hospital_Corridor.png");
            BuildHospital.Capture(crew.viewCamera,new Vector3(-4,1.65f,25.9f),new Vector3(-8.5f,1.1f,31),"v5_Hospital_Theatre.png");
            BuildHospital.Capture(crew.viewCamera,new Vector3(9,2,-18),new Vector3(0,4,1),"v5_Hospital_Exterior.png");
        }
        static void BakeNavigation()
        {
            var dynamicBodies=world.GetComponentsInChildren<Rigidbody>();var dynamicShapes=dynamicBodies.SelectMany(b=>b.GetComponentsInChildren<Collider>()).Distinct().ToArray();foreach(var shape in dynamicShapes)shape.enabled=false;
            var doors=Object.FindObjectsByType<HospitalDoor>(FindObjectsSortMode.None);foreach(var door in doors)door.obstacle.enabled=false;
            var surface=world.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.09f;surface.BuildNavMesh();
            if(!surface.navMeshData)throw new Exception("Hospital NavMesh not generated");
            string path=Env+"HospitalSoloNavMesh.asset";var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if(existing){EditorUtility.CopySerialized(surface.navMeshData,existing);surface.RemoveData();Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=existing;surface.AddData();EditorUtility.SetDirty(existing);}else AssetDatabase.CreateAsset(surface.navMeshData,path);
            var route=new NavMeshPath();foreach(var point in navigationChecks){if(!NavMesh.SamplePosition(point,out var destination,2,NavMesh.AllAreas)||!NavMesh.CalculatePath(new Vector3(0,.05f,5),destination.position,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)throw new Exception("Unreachable hospital zone: "+point);}
            foreach(var shape in dynamicShapes)shape.enabled=true;foreach(var door in doors)door.obstacle.enabled=true;
        }
    }
}
