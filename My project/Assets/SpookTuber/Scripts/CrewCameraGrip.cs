using UnityEngine;
using System.Collections.Generic;

namespace SpookTuber
{
    public sealed class CrewCameraGrip : MonoBehaviour
    {
        public CrewBody body;
        CrewMotor motor;
        Quaternion leftBasis,rightBasis;
        readonly List<(Transform bone,Quaternion rest,Vector3 axis,float angle)> fingers=new();
        void Awake()
        {
            motor=body.GetComponent<CrewMotor>();
            leftBasis=HandBasis(HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftLittleProximal);
            rightBasis=HandBasis(HumanBodyBones.RightHand,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightLittleProximal);
            foreach(var side in new[]{"Left","Right"})foreach(var finger in new[]{"Index","Middle","Ring","Little"}){
                var proximal=body.animator.GetBoneTransform(System.Enum.Parse<HumanBodyBones>(side+finger+"Proximal"));
                var tip=body.animator.GetBoneTransform(System.Enum.Parse<HumanBodyBones>(side+finger+"Distal"));
                var axis=Vector3.Cross((tip.position-proximal.position).normalized,-body.transform.up).normalized;
                foreach(var joint in new[]{"Proximal","Intermediate","Distal"}){
                    var bone=body.animator.GetBoneTransform(System.Enum.Parse<HumanBodyBones>(side+finger+joint));
                    fingers.Add((bone,bone.localRotation,bone.InverseTransformDirection(axis),joint=="Proximal"?38:joint=="Intermediate"?52:25));
                }
            }
        }
        void LateUpdate()
        {
            if(!motor||!motor.mainCam||motor.mainCam.Holder!=motor||body.IsDowned)return;
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
            var item=motor?motor.mainCam:null;
            bool holding=item&&item.Holder==motor&&!body.IsDowned;
            var animator=body.animator;
            foreach(var hand in new[]{AvatarIKGoal.LeftHand,AvatarIKGoal.RightHand}){
                animator.SetIKPositionWeight(hand,holding?1:0);animator.SetIKRotationWeight(hand,holding?1:0);
            }
            if(!holding)return;
            motor.UpdateView();item.UpdateHeldPose();
            animator.SetIKPosition(AvatarIKGoal.RightHand,item.rightGrip.position);
            animator.SetIKPosition(AvatarIKGoal.LeftHand,item.leftGrip.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand,Quaternion.LookRotation(item.transform.forward,item.transform.right)*rightBasis);
            animator.SetIKRotation(AvatarIKGoal.LeftHand,Quaternion.LookRotation(item.transform.forward,-item.transform.right)*leftBasis);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,1);animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,1);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow,body.transform.TransformPoint(new Vector3(.38f,1.0f,.04f)));
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow,body.transform.TransformPoint(new Vector3(-.35f,.99f,.10f)));
        }
    }
}
