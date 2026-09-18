using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using Object=UnityEngine.Object;

namespace SpookTuber
{
    public static class GameSettings
    {
        static UniversalRenderPipelineAsset graphics;
        static bool displayApplied;
        static float Read(string key,float fallback,float min,float max){float v=PlayerPrefs.GetFloat(key,fallback);return float.IsFinite(v)?Mathf.Clamp(v,min,max):fallback;}
        public static float Volume=>Read("Volume",1,0,1);
        public static float Look=>Read("LookSensitivity",.10f,.025f,.35f);
        public static float FieldOfView=>Read("FieldOfView",70,60,95);
        public static int Quality=>Mathf.Clamp(PlayerPrefs.GetInt("GraphicsQuality",1),0,2);
        public static int FrameLimit=>PlayerPrefs.GetInt("FrameLimit",60)==120?120:60;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            if(!displayApplied&&!Application.isEditor){displayApplied=true;if(PlayerPrefs.HasKey("WindowWidth")&&Array.IndexOf(Environment.GetCommandLineArgs(),"-screen-width")<0)Screen.SetResolution(Mathf.Clamp(PlayerPrefs.GetInt("WindowWidth"),960,3840),Mathf.Clamp(PlayerPrefs.GetInt("WindowHeight"),540,2160),PlayerPrefs.GetInt("Fullscreen",0)==1);}
            AudioListener.volume=Volume;Application.targetFrameRate=FrameLimit;QualitySettings.vSyncCount=PlayerPrefs.GetInt("VSync",0)==1?1:0;
            if(!graphics&&UniversalRenderPipeline.asset){graphics=Object.Instantiate(UniversalRenderPipeline.asset);graphics.name="SpookTuber runtime graphics";QualitySettings.renderPipeline=graphics;}
            if(graphics){int q=Quality;graphics.renderScale=q==0?.75f:q==1?.85f:1;graphics.shadowDistance=q==0?20:q==1?32:48;graphics.shadowCascadeCount=q==2?2:1;graphics.mainLightShadowmapResolution=q==2?2048:1024;graphics.additionalLightsShadowmapResolution=1024;graphics.msaaSampleCount=2;}
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(renderer.gameObject.layer!=9)renderer.renderingLayerMask=renderer.gameObject.layer==8?1u:3u;
            foreach(var crew in Object.FindObjectsByType<CrewMotor>(FindObjectsSortMode.None)){crew.mouseSensitivity=Look;crew.viewCamera.fieldOfView=FieldOfView;if(crew.shoulderLight)crew.shoulderLight.GetUniversalAdditionalLightData().renderingLayers=2u;}
        }
        public static void Set(string key,float value){if(!float.IsFinite(value))return;PlayerPrefs.SetFloat(key,value);Apply();}
        public static void Set(string key,int value){PlayerPrefs.SetInt(key,value);Apply();}
        public static void Save()=>PlayerPrefs.Save();
    }
}
