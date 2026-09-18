using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    public static class BuildCrew
    {
        const string Root="Assets/SpookTuber/";
        const string Models=Root+"Characters/Player/";
        static string QA=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA"));
        static readonly Dictionary<string,Color> Colors=new() {
            {"MAT_Ceramic_Ivory",new Color(.72f,.66f,.57f)},
            {"MAT_Edge_WarmWhite",new Color(.9f,.83f,.71f)},
            {"MAT_Chassis_Graphite",new Color(.028f,.033f,.041f)},
            {"MAT_Rubber",new Color(.012f,.015f,.021f)},
            {"MAT_Signal_Vermilion",new Color(.62f,.032f,.018f)},
            {"MAT_Hardware_Steel",new Color(.27f,.30f,.31f)},
            {"MAT_Display_Glass",new Color(.004f,.005f,.007f)},
            {"MAT_Hoodie_Chalk",new Color(.67f,.64f,.58f)},
            {"MAT_Cargo_Charcoal",new Color(.034f,.039f,.05f)},
            {"MAT_Webbing",new Color(.018f,.023f,.031f)}
        };
        static Material Material(string name,Color color,float metallic=0)
        {
            string path=Root+"Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);
            mat.SetFloat("_Smoothness",name.Contains("Glass")?.62f:name.Contains("Hardware")?.65f:name.Contains("Hoodie")||name.Contains("Cargo")?.12f:.3f);
            EditorUtility.SetDirty(mat);return mat;
        }
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        [MenuItem("SpookTuber/Build character assets and production house")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try {
                Directory.CreateDirectory(QA);
                foreach(var folder in new[]{"Materials","Prefabs","Scenes","Animations"})Directory.CreateDirectory(Root+folder);
                AssetDatabase.Refresh();
                var materials=Colors.ToDictionary(p=>p.Key,p=>Material(p.Key,p.Value,p.Key.Contains("Hardware")?.8f:p.Key.Contains("Chassis")?.45f:p.Key.Contains("Hoodie")||p.Key.Contains("Cargo")||p.Key.Contains("Webbing")?0:.1f));
                foreach(var file in new[]{"CHR_Player_Base","CHR_Player_Hoodie"}) {
                    string path=Models+file+".fbx";
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                    Require(importer!=null,"Missing "+path);
                    importer.animationType=ModelImporterAnimationType.Human;
                    importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation=true;importer.importBlendShapes=true;
                    importer.isReadable=true;importer.optimizeGameObjects=false;
                    importer.importCameras=false;importer.importLights=false;
                    importer.importNormals=ModelImporterNormals.Import;
                    importer.importTangents=ModelImporterTangents.CalculateMikk;
                    importer.meshCompression=ModelImporterMeshCompression.Off;
                    importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var transforms=model.GetComponentsInChildren<Transform>(true);
                    var names=transforms.Select(t=>t.name).ToHashSet();
                    var human=HumanTrait.BoneName.Where(n=>names.Contains(n.Replace(" ",""))).Select(n=>new HumanBone{humanName=n,boneName=n.Replace(" ",""),limit=new HumanLimit{useDefaultValues=true}}).ToArray();
                    importer.humanDescription=new HumanDescription {
                        human=human,
                        skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
                        upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false
                    };
                    foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
                    var clips=importer.defaultClipAnimations;
                    foreach(var c in clips){
                        if(c.name.Contains("Idle"))c.name="Idle";
                        else if(c.name.Contains("Walk"))c.name="Walk";
                        else if(c.name.Contains("Run"))c.name="Run";
                        c.loopTime=true;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;
                        c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.keepOriginalPositionY=true;
                    }
                    importer.clipAnimations=clips;
                    importer.SaveAndReimport();
                    var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                    Require(avatar&&avatar.isValid&&avatar.isHuman,file+" humanoid avatar invalid");
                }
                string controllerPath=Root+"Animations/CrewLocomotion.controller";
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                foreach(var layer in controller.layers)foreach(var child in layer.stateMachine.states)layer.stateMachine.RemoveState(child.state);
                controller.parameters=new[]{new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float}};
                foreach(var old in AssetDatabase.LoadAllAssetsAtPath(controllerPath).OfType<BlendTree>())Object.DestroyImmediate(old,true);
                var tree=new BlendTree{name="Locomotion",blendParameter="Speed",blendType=BlendTreeType.Simple1D,useAutomaticThresholds=false};
                AssetDatabase.AddObjectToAsset(tree,controller);
                var animations=AssetDatabase.LoadAllAssetsAtPath(Models+"CHR_Player_Hoodie.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
                for(int i=0;i<3;i++){
                    string name=new[]{"Idle","Walk","Run"}[i];
                    var clip=animations.FirstOrDefault(c=>c.name==name);
                    Require(clip,name+" missing");tree.AddChild(clip,i);
                }
                var state=controller.layers[0].stateMachine.AddState("Locomotion");
                state.motion=tree;controller.layers[0].stateMachine.defaultState=state;
                var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
                EditorUtility.SetDirty(controller);
                var blank=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var basePrefab=MakeCharacter("CHR_Player_Base",controller,false,false);
                var dressedPrefab=MakeCharacter("CHR_Player_Hoodie",controller,true,false);
                var playable=MakeCharacter("CHR_Player_Hoodie",controller,true,true);
                AssetDatabase.SaveAssets();
                BuildHouse(playable,dressedPrefab,basePrefab);
                ValidateAndCapture();
                if(File.Exists(Root+"Scenes/Hospital.unity"))BuildHospital.ConnectHouse();
                AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(QA,"unity_asset_validation.json"),"{\"status\":\"PASS\",\"avatars\":2,\"prefabs\":3,\"clips\":3,\"ragdollBodies\":11,\"scene\":\"ProductionHouse\",\"unity\":\""+Application.unityVersion+"\"}");
                Debug.Log("SPOOKTUBER_UNITY_ASSETS_PASS");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            } catch(Exception e) {
                Debug.LogException(e);
                Directory.CreateDirectory(QA);
                File.WriteAllText(Path.Combine(QA,"unity_failure.txt"),e.ToString());
                if(Application.isBatchMode)EditorApplication.Exit(1);
                else throw;
            }
        }

        static GameObject MakeCharacter(string file,AnimatorController controller,bool dressed,bool playable)
        {
            var root=new GameObject(playable?"PF_CrewPlayer":dressed?"PF_CrewHoodie":"PF_CrewBase");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+file+".fbx");
            var model=Object.Instantiate(source,root.transform);model.name="Visual";
            var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var body=root.AddComponent<CrewBody>();body.animator=animator;
            var map=model.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t.name,t=>t);
            SkinnedMeshRenderer Skin(string n)=>map.TryGetValue("CHR_Player_"+n,out var t)?t.GetComponent<SkinnedMeshRenderer>():null;
            body.chassis=Skin("Chassis");body.outfit=Skin("Outfit");body.gear=Skin("Gear");body.head=Skin("Head");
            body.hips=map["Hips"];body.headBone=map["Head"];
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){r.updateWhenOffscreen=true;r.quality=SkinQuality.Bone4;}
            body.SetOutfit(dressed);
            // Joint axes are converted from character space into the actual imported bone frame.
            var bodies=new Dictionary<string,Rigidbody>();
            var colliders=new List<Collider>();
            var spec=new[]{
                ("Hips","Spine",8f,.105f),("Chest","Neck",10f,.14f),
                ("LeftUpperArm","LeftLowerArm",2f,.055f),("LeftLowerArm","LeftHand",1.5f,.048f),
                ("RightUpperArm","RightLowerArm",2f,.055f),("RightLowerArm","RightHand",1.5f,.048f),
                ("LeftUpperLeg","LeftLowerLeg",5f,.065f),("LeftLowerLeg","LeftFoot",3f,.06f),
                ("RightUpperLeg","RightLowerLeg",5f,.065f),("RightLowerLeg","RightFoot",3f,.06f),
                ("Neck","Head",1f,.048f)
            };
            foreach(var (name,end,mass,radius) in spec) {
                var bone=map[name];var rb=bone.gameObject.AddComponent<Rigidbody>();rb.mass=mass;rb.isKinematic=true;
                rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                rb.linearDamping=.12f;rb.angularDamping=.8f;rb.solverIterations=12;rb.solverVelocityIterations=4;
                bodies[name]=rb;
                var c=bone.gameObject.AddComponent<CapsuleCollider>();
                var delta=bone.InverseTransformPoint(map[end].position);
                c.direction=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?0:1;
                if(Mathf.Abs(delta.z)>Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.y)))c.direction=2;
                c.center=delta*.5f;c.radius=radius;c.height=Mathf.Max(radius*2,delta.magnitude*.96f);
                c.enabled=false;colliders.Add(c);
            }
            foreach(var (name,rb) in bodies) {
                if(name=="Hips")continue;
                var p=rb.transform.parent;
                while(p && !p.GetComponent<Rigidbody>())p=p.parent;
                var joint=rb.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=p?p.GetComponent<Rigidbody>():bodies["Hips"];
                joint.axis=rb.transform.InverseTransformDirection(root.transform.right);
                joint.swingAxis=rb.transform.InverseTransformDirection(root.transform.forward);
                bool hinge=name.Contains("Lower");
                joint.lowTwistLimit=new SoftJointLimit{limit=hinge?-5:-40};
                joint.highTwistLimit=new SoftJointLimit{limit=hinge?105:45};
                joint.swing1Limit=new SoftJointLimit{limit=hinge?8:35};
                joint.swing2Limit=new SoftJointLimit{limit=hinge?8:35};
                joint.enableProjection=true;joint.projectionDistance=.06f;joint.projectionAngle=15;
                joint.enableCollision=false;
            }
            foreach(var side in new[]{"Left","Right"}) {
                var foot=map[side+"Foot"];
                var shoe=new GameObject(side+"ShoeCollision");
                shoe.transform.SetPositionAndRotation(foot.position+root.transform.forward*.065f-root.transform.up*.09f,root.transform.rotation);
                shoe.transform.SetParent(foot,true);
                var sole=shoe.AddComponent<BoxCollider>();sole.size=new Vector3(.16f,.11f,.28f);sole.enabled=false;colliders.Add(sole);
                var hand=map[side+"Hand"];
                var palm=new GameObject(side+"HandCollision");
                palm.transform.SetPositionAndRotation(Vector3.Lerp(hand.position,map[side+"MiddleDistal"].position,.5f),root.transform.rotation);
                palm.transform.SetParent(hand,true);
                var glove=palm.AddComponent<BoxCollider>();glove.size=new Vector3(.17f,.06f,.105f);glove.enabled=false;colliders.Add(glove);
            }
            body.ragdoll=bodies.Values.ToArray();body.ragdollColliders=colliders.ToArray();
            var secondary=root.AddComponent<CrewSecondaryMotion>();
            secondary.pendants=new[]{map["Charm"],map["Tag"]};
            if(playable) {
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
                var cc=root.AddComponent<CharacterController>();cc.height=1.48f;cc.radius=.21f;cc.center=new Vector3(0,.75f,0);
                cc.stepOffset=.22f;cc.slopeLimit=45;cc.skinWidth=.025f;
                var motor=root.AddComponent<CrewMotor>();
                var grip=model.AddComponent<CrewCameraGrip>();grip.body=body;
                var camera=new GameObject("CrewCamera");camera.transform.SetParent(root.transform);camera.tag="MainCamera";
                motor.viewCamera=camera.AddComponent<Camera>();motor.viewCamera.fieldOfView=70;motor.viewCamera.nearClipPlane=.035f;motor.viewCamera.cullingMask=~(1<<9);
                camera.AddComponent<AudioListener>();
                var lamp=new GameObject("ShoulderLight");lamp.transform.SetParent(map["Socket_ShoulderLight"],false);
                lamp.transform.rotation=root.transform.rotation;motor.shoulderLight=lamp.AddComponent<Light>();
                motor.shoulderLight.type=LightType.Spot;motor.shoulderLight.range=14;motor.shoulderLight.spotAngle=62;motor.shoulderLight.intensity=3;
                motor.shoulderLight.color=new Color(1,.89f,.72f);motor.shoulderLight.shadows=LightShadows.Soft;
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/"+root.name+".prefab");
            Object.DestroyImmediate(root);return prefab;
        }
        static GameObject Cube(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);
            go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;return go;
        }
        internal static Text Text(Transform parent,string name,string text,int size,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 box)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=box;
            var t=go.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text=text;t.fontSize=size;t.color=new Color(.88f,.85f,.77f);t.raycastTarget=false;
            return t;
        }
        static void Sign(string text,Vector3 position,float size,Color color)
        {
            var go=new GameObject("Sign_"+text);go.transform.position=position;
            var t=go.AddComponent<TextMesh>();t.text=text;t.characterSize=size*.25f;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.color=color;
            go.transform.rotation=Quaternion.identity;
        }
        static MainCam MakeCamera()
        {
            var root=new GameObject("EQP_MainCam_01");
            var body=Material("MAT_MainCam_Body",new Color(.032f,.041f,.047f),.35f);
            var metal=Material("MAT_MainCam_Metal",new Color(.18f,.22f,.24f),.8f);
            var red=Material("MAT_Signal_Vermilion",new Color(.62f,.032f,.018f),.25f);
            var glass=Material("MAT_MainCam_Lens",new Color(.012f,.036f,.045f),.4f);
            var screenPath=Root+"Materials/MAT_MainCam_Screen.mat";
            var screenMat=AssetDatabase.LoadAssetAtPath<Material>(screenPath);
            if(!screenMat){screenMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(screenMat,screenPath);}
            screenMat.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(screenMat);
            Cube("CameraBody",Vector3.zero,new Vector3(.23f,.17f,.26f),body,root.transform);
            Cube("Grip",new Vector3(.125f,-.02f,0),new Vector3(.04f,.12f,.19f),metal,root.transform);
            var display=Cube("LCD",new Vector3(0,.005f,-.135f),new Vector3(.17f,.095f,.008f),screenMat,root.transform);
            Cube("RecordButton",new Vector3(.075f,.093f,-.08f),new Vector3(.026f,.014f,.025f),red,root.transform);
            foreach(float z in new[]{-.085f,.065f})Cube("HandleMount",new Vector3(0,.125f,z),new Vector3(.033f,.095f,.03f),metal,root.transform);
            Cube("Handle",new Vector3(0,.173f,-.01f),new Vector3(.046f,.028f,.19f),body,root.transform);
            for(int i=0;i<3;i++){
                var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name=i==2?"LensGlass":"LensRing";
                ring.transform.SetParent(root.transform);ring.transform.localPosition=new Vector3(0,0,.16f+i*.022f);
                ring.transform.localRotation=Quaternion.Euler(90,0,0);ring.transform.localScale=new Vector3(.125f-i*.015f,.017f,.125f-i*.015f);
                ring.GetComponent<Renderer>().sharedMaterial=i==2?glass:i==0?metal:body;
            }
            foreach(var c in root.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            var shape=root.AddComponent<BoxCollider>();shape.center=new Vector3(0,.025f,.025f);shape.size=new Vector3(.29f,.28f,.38f);
            var rb=root.AddComponent<Rigidbody>();rb.mass=1.2f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var take=root.AddComponent<CrewTake>();var camera=root.AddComponent<MainCam>();
            camera.heldOffset=new Vector3(.20f,-.25f,.48f);
            camera.screen=display.GetComponent<Renderer>();
            camera.lens=new GameObject("MainCam_Lens").transform;camera.lens.SetParent(root.transform,false);camera.lens.localPosition=new Vector3(0,0,.24f);
            camera.rightGrip=new GameObject("Grip_RightHand").transform;camera.rightGrip.SetParent(root.transform,false);camera.rightGrip.localPosition=new Vector3(.15f,-.025f,-.10f);
            camera.leftGrip=new GameObject("Grip_LeftHand").transform;camera.leftGrip.SetParent(root.transform,false);camera.leftGrip.localPosition=new Vector3(-.15f,-.025f,-.10f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,Root+"Prefabs/EQP_MainCam_01.prefab",InteractionMode.AutomatedAction);
            root.transform.SetPositionAndRotation(new Vector3(-3.8f,1.01f,-1.43f),Quaternion.Euler(0,90,0));
            return camera;
        }
        static void BuildHouse(GameObject playerPrefab,GameObject hoodiePrefab,GameObject basePrefab)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.40f,.44f);
            var wall=Material("MAT_House_Plaster",new Color(.35f,.39f,.38f));
            var wood=Material("MAT_House_Wood",new Color(.23f,.14f,.085f));
            var trim=Material("MAT_House_Trim",new Color(.052f,.071f,.083f));
            var warm=Material("MAT_House_Paper",new Color(.76f,.7f,.57f));
            var rug=Material("MAT_House_Rug",new Color(.12f,.24f,.26f));
            Cube("ApartmentFloor",new Vector3(0,-.12f,0),new Vector3(9,.2f,8),wood);
            Cube("BackWall",new Vector3(0,1.5f,4),new Vector3(9,3,.2f),wall);
            Cube("LeftWall",new Vector3(-4.5f,1.5f,0),new Vector3(.2f,3,8),wall);
            Cube("RightWall",new Vector3(4.5f,1.5f,0),new Vector3(.2f,3,8),wall);
            Cube("FrontWall",new Vector3(0,1.5f,-4),new Vector3(9,3,.2f),wall);
            Cube("Ceiling",new Vector3(0,3.1f,0),new Vector3(9,.2f,8),warm);
            Cube("EntryDoor",new Vector3(3.1f,1.06f,-3.88f),new Vector3(1.05f,2.12f,.08f),wood);
            Cube("DoorHandle",new Vector3(2.76f,1.03f,-3.81f),new Vector3(.11f,.035f,.06f),warm);
            Cube("WindowFrame",new Vector3(-4.36f,1.8f,-.1f),new Vector3(.06f,1.4f,2.2f),trim);
            Cube("WindowGlass",new Vector3(-4.31f,1.8f,-.1f),new Vector3(.02f,1.25f,2.05f),Material("MAT_Window",new Color(.3f,.46f,.6f)));
            Cube("WindowCross",new Vector3(-4.28f,1.8f,-.1f),new Vector3(.04f,1.3f,.04f),warm);
            Cube("Rug",new Vector3(0,.004f,.5f),new Vector3(3.5f,.015f,3),rug);
            for(int i=0;i<12;i++)Cube("Floorboard",new Vector3(-4.1f+i*.72f,-.013f,0),new Vector3(.012f,.015f,8),trim);
            var wardrobe=Cube("CrewWardrobe",new Vector3(2.8f,1,3.55f),new Vector3(2.2f,2,.45f),trim);
            wardrobe.AddComponent<CrewWardrobe>();
            Cube("WardrobeBacking",new Vector3(2.8f,1.05f,3.29f),new Vector3(2.05f,1.85f,.04f),wood);
            Sign("CREW / WARDROBE",new Vector3(2.8f,2.3f,3.2f),.11f,new Color(.85f,.80f,.67f));
            var display=(GameObject)PrefabUtility.InstantiatePrefab(hoodiePrefab);
            display.name="WardrobeDisplay";display.transform.position=new Vector3(2.8f,.05f,3.05f);
            display.transform.rotation=Quaternion.Euler(0,180,0);
            display.GetComponent<CrewBody>().enabled=false;display.GetComponent<CrewSecondaryMotion>().enabled=false;
            Cube("SofaSeat",new Vector3(-2.8f,.40f,2.7f),new Vector3(2.5f,.55f,1),rug);
            Cube("SofaBack",new Vector3(-2.8f,.83f,3.1f),new Vector3(2.5f,.95f,.25f),rug);
            for(float x=-3.9f;x<-1.5f;x+=2.2f)Cube("SofaArm",new Vector3(x,.60f,2.7f),new Vector3(.2f,.65f,1.05f),trim);
            Cube("CoffeeTableTop",new Vector3(-2.5f,.45f,1.1f),new Vector3(1.5f,.09f,.7f),wood);
            for(float x=-3.1f;x<-1.8f;x+=1.2f)for(float z=.85f;z<1.4f;z+=.5f)Cube("TableLeg",new Vector3(x,.22f,z),new Vector3(.07f,.44f,.07f),trim);
            for(int i=0;i<3;i++)Cube("ProductionNotebook",new Vector3(-2.7f+i*.04f,.52f+i*.022f,1.1f),new Vector3(.32f,.02f,.24f),i%2==0?warm:trim);
            Cube("EquipmentBench",new Vector3(-3.8f,.8f,-1.4f),new Vector3(.7f,.09f,2),wood);
            for(float z=-2.15f;z<-.5f;z+=1.5f)Cube("BenchLeg",new Vector3(-3.8f,.4f,z),new Vector3(.6f,.8f,.08f),trim);
            for(int i=0;i<3;i+=2)Cube("EquipmentCase",new Vector3(-3.8f,.96f,-2+i*.57f),new Vector3(.42f,.25f,.40f),trim);
            var mainCam=MakeCamera();
            Sign("S P O O K T U B E R",new Vector3(0,2.37f,3.82f),.14f,new Color(.9f,.84f,.70f));
            Sign("PRODUCTION HOUSE  /  01",new Vector3(0,2.12f,3.82f),.055f,new Color(.68f,.72f,.72f));
            var sun=new GameObject("LateAfternoon").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.color=new Color(1,.84f,.67f);
            sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.05f;sun.shadowNormalBias=.05f;
            for(int i=-1;i<=1;i+=2) {
                var lamp=new GameObject("CeilingLamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=8;lamp.intensity=10;lamp.color=new Color(1,.82f,.6f);
                lamp.transform.position=new Vector3(i*2.5f,2.7f,1);
                Cube("LampShade",lamp.transform.position+Vector3.up*.1f,new Vector3(.45f,.12f,.45f),warm);
            }
            var player=(GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);player.name="LocalCrew";
            player.transform.position=new Vector3(0,.08f,-1.4f);
            var motor=player.GetComponent<CrewMotor>();
            motor.mainCam=mainCam;
            motor.viewCamera.transform.SetPositionAndRotation(new Vector3(0,1.7f,-3.8f),Quaternion.Euler(10,0,0));
            var canvas=new GameObject("CrewHUD").AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            motor.status=Text(canvas.transform,"Status","CREW 01 / OFF DUTY",23,new Vector2(0,1),new Vector2(0,1),new Vector2(42,-34),new Vector2(700,60));
            motor.hint=Text(canvas.transform,"Interaction","",25,new Vector2(.5f,.35f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(700,60));motor.hint.alignment=TextAnchor.MiddleCenter;
            var cross=Text(canvas.transform,"Crosshair","·",32,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(30,40));cross.alignment=TextAnchor.MiddleCenter;
            Text(canvas.transform,"Controls","WASD  MOVE   SHIFT  RUN   SPACE  JUMP   E  USE   F  LIGHT   V  VIEW   R  REC   Q  DROP   P  TAKE   ESC  CURSOR",17,new Vector2(0,0),Vector2.zero,new Vector2(42,24),new Vector2(1600,35));
            var review=new GameObject("RecordedTake",typeof(RectTransform),typeof(RawImage));review.transform.SetParent(canvas.transform,false);
            var rect=review.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1440,810);
            var take=mainCam.GetComponent<CrewTake>();take.reviewImage=review.GetComponent<RawImage>();take.reviewImage.raycastTarget=false;review.SetActive(false);
            take.reviewLabel=Text(canvas.transform,"TakeTimecode","",24,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-36),new Vector2(950,80));
            take.reviewLabel.alignment=TextAnchor.UpperCenter;take.reviewLabel.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"Scenes/ProductionHouse.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/ProductionHouse.unity",true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=Root+"Scenes/ProductionHouse.unity")).ToArray();
        }
        static void ValidateAndCapture()
        {
            var body=Object.FindObjectsByType<CrewBody>(FindObjectsSortMode.None).First(b=>b.GetComponent<CrewMotor>());
            Require(body.animator.avatar.isValid&&body.animator.avatar.isHuman,"Invalid player avatar");
            body.animator.Rebind();body.animator.Update(0);
            Require(body.head.bounds.size.y>.2f&&body.head.bounds.size.y<.8f,"Head scale outside expected range");
            Require(body.ragdoll.Length==11,"Missing ragdoll body");
            Require(!body.GetComponentsInChildren<Transform>().Any(t=>t.name=="CHR_Player_Face"),"Display must remain blank");
            Require(body.head.sharedMaterials.Any(m=>m.name=="MAT_Display_Glass"&&!m.IsKeywordEnabled("_EMISSION")),"Blank display material missing");
            foreach(var r in body.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                Require(r.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"),r.name+" material mismatch");
                Require(r.sharedMesh.vertexCount>0,r.name+" no vertices");
                Require(r.bones.All(b=>b),r.name+" missing bone");
            }
            var camera=body.GetComponent<CrewMotor>().viewCamera;
            camera.transform.position=body.transform.position+new Vector3(2.7f,1.8f,3.3f);
            camera.transform.LookAt(body.transform.position+Vector3.up*.9f);
            var rt=new RenderTexture(1400,1000,24);camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(QA,"Unity_ProductionHouse.png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            // Do not save the review-camera pose into the production scene.
        }
    }
}



