using System;
using UnityEngine;

namespace SpookTuber
{
    [DefaultExecutionOrder(190)]
    public sealed class CrewInventory : MonoBehaviour
    {
        readonly CarryItem[] slots=new CarryItem[3];
        public CrewMotor Motor {get;private set;}
        public int Selected {get;private set;}
        public bool Suspended {get;private set;}
        public CarryItem Active=>slots[Selected];
        public CarryItem this[int index]=>index>=0&&index<3?slots[index]:null;
        public string Notice {get;private set;}="";
        public int Count=>Array.FindAll(slots,item=>item).Length;
        CrewBody body;
        void Awake(){Motor=GetComponent<CrewMotor>();body=GetComponent<CrewBody>();}
        public bool TryPickup(CarryItem item)
        {
            if(!item||item.Owner||body.IsDowned||Suspended)return false;
            if(RunSession.Current&&!RunSession.Current.OwnsGear(item.kind)){Notice="Purchase this gear at Supply Mart first";return false;}
            int index=Array.FindIndex(slots,s=>!s);if(index<0){Notice="All 3 slots are full / Q to drop selected gear";return false;}
            var origin=Motor.viewCamera.transform.position;var delta=item.transform.position-origin;
            if(delta.magnitude>2.2f)return false;
            if(Physics.Raycast(origin,delta.normalized,out var hit,delta.magnitude,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CarryItem>()!=item)return false;
            if(Active){var recording=Active.GetComponent<MainCam>();if(recording&&recording.Take.Recording&&!recording.Take.StopRecording())return false;}
            slots[index]=item;item.Claim(this);Select(index);Notice="Picked up "+item.displayName;return true;
        }
        public bool Select(int index)
        {
            if(index<0||index>=3||body.IsDowned||Suspended)return false;
            if(Active&&Active!=slots[index]){
                var camera=Active.GetComponent<MainCam>();if(camera&&camera.Take.Recording&&!camera.Take.StopRecording()){Notice="Save the current tape before changing gear";return false;}
            }
            var glove=GetComponent<GravityGlove>();if(glove)glove.Release();Selected=index;
            foreach(var item in slots)if(item)item.Present(item==Active);
            if(Active)Motor.firstPerson=true;UpdateHeldPose();return true;
        }
        public void Suspend(bool value)
        {
            if(value&&Active){var camera=Active.GetComponent<MainCam>();if(camera&&camera.Take.Recording)camera.Take.StopRecording();}
            Suspended=value;var glove=GetComponent<GravityGlove>();if(glove)glove.Release();
            foreach(var item in slots)if(item)item.Present(!value&&item==Active);
        }
        public bool Drop(CarryItem item=null)
        {
            item=item?item:Active;int index=Array.IndexOf(slots,item);if(!item||index<0)return false;
            var glove=GetComponent<GravityGlove>();if(glove)glove.Release();
            var forward=Motor.viewCamera.transform.forward;var origin=Motor.viewCamera.transform.position;
            float distance=.55f;if(Physics.SphereCast(origin,.16f,forward,out var hit,distance,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))distance=Mathf.Max(.03f,hit.distance-.04f);
            item.Release(origin+forward*distance,Motor.PlanarVelocity+forward*.65f);slots[index]=null;
            Notice="Put down "+item.displayName;return true;
        }
        public void UpdateHeldPose()
        {
            if(!Active||Suspended||body.IsDowned)return;
            if(Active.kind==CarryItem.Kind.Camera){Active.GetComponent<MainCam>().UpdateHeldPose();return;}
            Active.transform.localPosition=Active.heldOffset;Active.transform.localRotation=Quaternion.identity;
        }
        void LateUpdate(){if(body.IsDowned){foreach(var item in slots)if(item)Drop(item);}else UpdateHeldPose();}
    }
}
