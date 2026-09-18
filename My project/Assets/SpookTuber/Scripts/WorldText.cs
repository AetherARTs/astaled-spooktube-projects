using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpookTuber
{
    // World signs obey geometry depth; the default GUI font shader draws through equipment.
    [RequireComponent(typeof(TextMesh))]
    public sealed class WorldText : MonoBehaviour
    {
        Material material,atlas;
        TextMesh text;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name.Contains("_v"))return;
            foreach(var root in scene.GetRootGameObjects())foreach(var label in root.GetComponentsInChildren<TextMesh>(true))
                if(!label.GetComponent<WorldText>())label.gameObject.AddComponent<WorldText>();
        }
        void OnEnable()=>Configure();
        public void Configure()
        {
            if(material)return;
            text=GetComponent<TextMesh>();var renderer=GetComponent<MeshRenderer>();atlas=renderer.sharedMaterial;
            material=new Material(Resources.Load<Shader>("WorldText"));renderer.sharedMaterial=material;
            Refresh(null);Font.textureRebuilt+=Refresh;
        }
        void Refresh(Font font){if(material)material.mainTexture=text.font?text.font.material.mainTexture:atlas.mainTexture;}
        void OnDisable(){Font.textureRebuilt-=Refresh;if(material){GetComponent<MeshRenderer>().sharedMaterial=atlas;Destroy(material);material=null;}}
    }
}
