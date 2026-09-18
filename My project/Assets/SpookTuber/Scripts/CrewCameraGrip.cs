using UnityEngine;
using System.Collections.Generic;

namespace SpookTuber
{
    public sealed class CrewCameraGrip : MonoBehaviour
    {
        public CrewBody body;
        CrewMotor motor;
        CrewInventory inventory;
        CrewPhone phone;
        Quaternion leftBasis,rightBasis;
        readonly List<(Transform bone,Quaternion rest,Vector3 axis,float angle)> fingers=new();
        void Awake()
        {
            motor=body.GetComponent<CrewMotor>();
            inventory=body.GetComponent<CrewInventory>();phone=body.GetComponent<CrewPhone>();
            leftBasis=HandBasis(HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftLittleProximal);
            rightBasis=HandBasis(HumanBodyBones.RightHand,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightLittleProximal);
            foreach(var side in new[]{"Left","Right"})foreach(var finger in new[]{"Thumb","Index","Middle","Ring","Little"}){
                var proximal=body.animator.GetBoneTransform(System.Enum.Parse<HumanBodyBones>(side+finger+"Proximal"));
                var tip=body.animator.GetBoneTransform(System.Enum.Parse<HumanBodyBones>(side+finger+"Distal"));
                var axis=Vector3.Cross((tip.position-proximal.position).normalized,body.transform.up).normalized;
                foreach(var joint in new[]{"Proximal","Intermediate","Distal"}){
                    var bone=body.animator.GetBoneTransform(System.Enum.Parse<HumanBodyBones>(side+finger+joint));
                    fingers.Add((bone,bone.localRotation,bone.InverseTransformDirection(axis),finger=="Thumb"?25:joint=="Proximal"?45:joint=="Intermediate"?68:30));
                }
            }
        }
        void LateUpdate()
        {
            if(!motor||body.IsDowned||!(inventory&&inventory.Active&&!inventory.Suspended||phone&&phone.IsOpen))return;
            foreach(var f in fingers)f.bone.localRotation=f.rest*Quaternion.AngleAxis(f.angle,f.axis);
        }
        Quaternion HandBasis(HumanBodyBones handId,HumanBodyBones middleId,HumanBodyBones indexId,HumanBodyBones littleId)
        {
            var hand=body.animator.GetBoneTransform(handId);
            var forward=(body.animator.GetBoneTransform(middleId).position-hand.position).normalized;
            var across=body.animator.GetBoneTransform(indexId).position-body.animator.GetBoneTransform(littleId).position;
            var normal=Vector3.Cross(forward,across).normalized;
            if(Vector3.Dot(normal,body.transform.up)<0)normal=-normal;
            return Quaternion.Inverse(Quaternion.LookRotation(hand.InverseTransformDirection(forward),hand.InverseTransformDirection(normal)));
        }
        void OnAnimatorIK(int layer)
        {
            if(!inventory)inventory=body.GetComponent<CrewInventory>();if(!phone)phone=body.GetComponent<CrewPhone>();
            var item=inventory&&!inventory.Suspended?inventory.Active:null;
            bool holding=(item||phone&&phone.IsOpen)&&!body.IsDowned;
            var animator=body.animator;
            if(motor&&!body.IsDowned){
                float crouch=motor.CrouchAmount;animator.bodyPosition-=body.transform.up*(crouch*.42f*body.transform.lossyScale.y);
                foreach(var foot in new[]{AvatarIKGoal.LeftFoot,AvatarIKGoal.RightFoot}){
                    animator.SetIKPositionWeight(foot,crouch);var target=animator.GetIKPosition(foot);target.y=body.transform.position.y+.09f*body.transform.lossyScale.y;animator.SetIKPosition(foot,target);
                }
            }
            foreach(var hand in new[]{AvatarIKGoal.LeftHand,AvatarIKGoal.RightHand}){
                bool useHand=holding&&(hand==AvatarIKGoal.RightHand||item&&item.leftGrip);
                animator.SetIKPositionWeight(hand,useHand?1:0);animator.SetIKRotationWeight(hand,useHand?1:0);
            }
            if(!holding)return;
            motor.UpdateView();inventory.UpdateHeldPose();
            var targetRight=item?item.rightGrip:phone.RightGrip;
            if(!targetRight)return;
            var basis=item?item.transform:phone.RightGrip;
            animator.SetIKPosition(AvatarIKGoal.RightHand,targetRight.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand,(phone&&phone.IsOpen?Quaternion.LookRotation(-basis.right,basis.forward):Quaternion.LookRotation(basis.forward,basis.right))*rightBasis);
            if(item&&item.leftGrip){
                animator.SetIKPosition(AvatarIKGoal.LeftHand,item.leftGrip.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand,Quaternion.LookRotation(basis.right,-basis.up)*leftBasis);
            }
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,1);animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,1);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow,body.transform.TransformPoint(new Vector3(.38f,1.0f,.04f)));
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow,body.transform.TransformPoint(new Vector3(-.35f,.99f,.10f)));
        }
    }
}
