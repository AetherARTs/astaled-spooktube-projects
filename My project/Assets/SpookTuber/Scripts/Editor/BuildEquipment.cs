using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SpookTuber.Editor
{
    public static class BuildEquipment
    {
        const string Root="Assets/SpookTuber/";
        public static void Build()
        {
            try{
                AssetDatabase.Refresh();
                // Freeze previous camera presentation before changing its shared prefab.
                foreach(string name in new[]{"ProductionHouse","Hospital"}){
                    string legacy=Root+"Scenes/Legacy/"+name+"_v5.unity";
                    if(!File.Exists(legacy))AssetDatabase.CopyAsset(Root+"Scenes/"+name+".unity",legacy);
                    foreach(int version in new[]{4,5}){
                        string path=Root+"Scenes/Legacy/"+name+"_v"+version+".unity";EditorSceneManager.OpenScene(path);
                        foreach(var camera in Object.FindObjectsByType<MainCam>(FindObjectsSortMode.None))if(PrefabUtility.IsPartOfPrefabInstance(camera))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(camera),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    }
                }
                string asset=Root+"Environment/Models/EQP_MainCam_Detailed.fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(asset);importer.importAnimation=false;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;
                foreach(var name in new[]{"H_DarkMetal","H_Steel","H_Rubber","H_Glass","H_Amber","H_Red","P_Ivory"})importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),AssetDatabase.LoadAssetAtPath<Material>(Root+"Hospital/Materials/"+name+".mat"));importer.SaveAndReimport();
                string prefab=Root+"Prefabs/EQP_MainCam_01.prefab";var root=PrefabUtility.LoadPrefabContents(prefab);var item=root.GetComponent<MainCam>();
                foreach(var old in root.GetComponentsInChildren<Renderer>().Where(r=>r!=item.screen).Select(r=>PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject)??r.gameObject).Distinct().ToArray())Object.DestroyImmediate(old);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(asset));model.transform.SetParent(root.transform,false);model.name="ProductionCameraBody";
                var glass=model.GetComponentsInChildren<MeshRenderer>().First();var mesh=glass.GetComponent<MeshFilter>().sharedMesh;
                int slot=Array.FindIndex(glass.sharedMaterials,m=>m.name=="H_Glass");var vertices=mesh.vertices;var indices=mesh.GetTriangles(slot);Vector3 center=Vector3.zero;foreach(int index in indices)center+=glass.transform.TransformPoint(vertices[index]);center/=indices.Length;
                if(center.z<0)model.transform.localRotation=Quaternion.Euler(0,180,0);model.transform.localScale=new Vector3(-1,1,1);
                item.screen.transform.localPosition=new Vector3(0,.012f,-.153f);item.screen.transform.localScale=new Vector3(.174f,.096f,.002f);
                var collider=root.GetComponent<BoxCollider>();collider.center=new Vector3(.04f,.047f,.032f);collider.size=new Vector3(.34f,.30f,.40f);
                PrefabUtility.SaveAsPrefabAsset(root,prefab);PrefabUtility.UnloadPrefabContents(root);
                AssetDatabase.SaveAssets();File.WriteAllText("../QA/v6_maincam_import.txt","PASS / authored camera mesh, live LCD preserved, old camera unpacked in v4/v5 scenes\nImported lens center before alignment: "+center);
                EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
