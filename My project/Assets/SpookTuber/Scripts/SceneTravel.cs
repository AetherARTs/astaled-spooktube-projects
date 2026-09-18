using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpookTuber
{
    public sealed class SceneTravel : MonoBehaviour
    {
        public static string Destination {get;private set;}="ProductionHouse";
        public static float Progress {get;private set;}
        public static void Go(string destination)
        {
            Time.timeScale=1;AudioListener.pause=false;
            if(string.IsNullOrEmpty(destination)){GameSettings.Save();Application.Quit();return;}
            Destination=destination;Progress=0;
            if(Application.CanStreamedLevelBeLoaded("Loading"))SceneManager.LoadSceneAsync("Loading");
            else SceneManager.LoadSceneAsync(destination);
        }
        IEnumerator Start()
        {
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            yield return null;
            var operation=SceneManager.LoadSceneAsync(Destination);operation.allowSceneActivation=false;
            while(operation.progress<.9f){Progress=Mathf.Clamp01(operation.progress/.9f);yield return null;}
            Progress=1;yield return null;operation.allowSceneActivation=true;
        }
    }
}
