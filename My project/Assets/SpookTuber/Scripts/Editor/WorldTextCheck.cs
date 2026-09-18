using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpookTuber.Editor
{
    public static class WorldTextCheck
    {
        public static void Run()
        {
            string result;
            try{
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("QA camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=1;camera.nearClipPlane=.1f;
                var text=new GameObject("QA world sign").AddComponent<TextMesh>();text.transform.position=new Vector3(0,0,2);text.anchor=TextAnchor.MiddleCenter;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=.2f;text.text="EXIT";text.color=Color.white;text.font.RequestCharactersInTexture(text.text,64);text.GetComponent<Renderer>().sharedMaterial=text.font.material;text.gameObject.AddComponent<WorldText>();
                text.GetComponent<WorldText>().Configure();
                var target=new RenderTexture(256,256,24);camera.targetTexture=target;
                int Bright(){camera.Render();RenderTexture.active=target;var image=new Texture2D(256,256,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();int count=image.GetPixels().Count(c=>c.r>.5f);UnityEngine.Object.DestroyImmediate(image);return count;}
                int visible=Bright();var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=new Vector3(0,0,1);blocker.transform.localScale=new Vector3(2,2,.1f);var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",Color.black);blocker.GetComponent<Renderer>().sharedMaterial=material;int occluded=Bright();
                if(visible<20||occluded!=0)throw new Exception($"Depth text pixels: visible={visible}, occluded={occluded}");
                result=$"PASS / visible white sign pixels {visible}; opaque equipment occludes all sign pixels {occluded}";
            }catch(Exception e){result="FAIL / "+e;}
            File.WriteAllText("../QA/v6_world_text_check.txt",result);EditorApplication.Exit(result.StartsWith("PASS")?0:1);
        }
    }
}
