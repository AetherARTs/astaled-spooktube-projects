using System;
using UnityEngine;

namespace SpookTuber
{
    public sealed class CrewVoice : MonoBehaviour
    {
        public bool Listening {get;private set;}
        public float Level {get;private set;}
        public float Threshold {get;private set;}=.025f;
        public string Device {get;private set;}="";
        public string Status {get;private set;}="MIC OFF";
        public string[] Devices=>Microphone.devices;
        AudioClip capture;
        readonly float[] samples=new float[512];
        float nextSample,nextNoise,startedAt;
        int previousPosition=-1;
        CrewBody body;
        void Awake(){body=GetComponent<CrewBody>();float saved=PlayerPrefs.GetFloat("VoiceThreshold",.025f);Threshold=float.IsFinite(saved)?Mathf.Clamp(saved,.005f,.2f):.025f;}
        void Start(){if(!Application.isBatchMode&&PlayerPrefs.GetInt("VoiceEnabled",1)==1)Enable(PlayerPrefs.GetString("VoiceDevice",""));}
        public bool Enable(string device)
        {
            StopCapture();
            if(Devices.Length==0){Status="NO MICROPHONE / connect a device";return false;}
            Device=Array.IndexOf(Devices,device)>=0?device:Devices[0];
            try{
                capture=Microphone.Start(Device,true,1,16000);Listening=capture;
                if(!Listening){Status="MIC START FAILED";return false;}
                startedAt=Time.unscaledTime;previousPosition=-1;Status="MIC STARTING";
                PlayerPrefs.SetInt("VoiceEnabled",1);PlayerPrefs.SetString("VoiceDevice",Device);PlayerPrefs.Save();return true;
            }catch(Exception e){Status="MIC UNAVAILABLE / "+e.GetType().Name;StopCapture();return false;}
        }
        public void Mute(){StopCapture();Status="MIC OFF";PlayerPrefs.SetInt("VoiceEnabled",0);PlayerPrefs.Save();}
        public void SetThreshold(float value){if(!float.IsFinite(value))return;Threshold=Mathf.Clamp(value,.005f,.2f);PlayerPrefs.SetFloat("VoiceThreshold",Threshold);PlayerPrefs.Save();}
        public static float Rms(float[] data)
        {
            if(data==null||data.Length==0)return 0;double sum=0;
            foreach(float sample in data){if(!float.IsFinite(sample))return 0;sum+=sample*sample;}
            return (float)Math.Sqrt(sum/data.Length);
        }
        public void ProcessLevel(float rms)
        {
            Level=float.IsFinite(rms)?Mathf.Clamp01(rms):0;
            if(!Listening||Level<Threshold||body.IsDowned||Time.unscaledTime<nextNoise)return;
            nextNoise=Time.unscaledTime+.22f;
            WorldNoise.Emit(body.headBone.position,Mathf.Lerp(4,24,Mathf.Clamp01((Level-Threshold)*8)),"Voice");
        }
        void Update()
        {
            if(!Listening||Time.unscaledTime<nextSample)return;nextSample=Time.unscaledTime+.035f;
            int position=Microphone.GetPosition(Device);
            if(position<0||!Microphone.IsRecording(Device)){StopCapture();Status="MIC DISCONNECTED / reconnect in Phone Settings";return;}
            if(position==previousPosition){if(Time.unscaledTime-startedAt>3){Level=0;Status="MIC NO SIGNAL / check Windows input permissions";}return;}
            previousPosition=position;startedAt=Time.unscaledTime;
            int offset=(position-samples.Length+capture.samples)%capture.samples;
            if(capture.GetData(samples,offset)){ProcessLevel(Rms(samples));Status=Level>=Threshold?"MIC / SOUND DETECTED":"MIC LIVE";}
        }
        void StopCapture(){if(capture){Microphone.End(Device);Destroy(capture);capture=null;}Listening=false;Level=0;}
        void OnDisable(){StopCapture();}
    }
}
