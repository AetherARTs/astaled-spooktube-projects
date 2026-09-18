using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpookTuber
{
    public sealed class CrewBody : MonoBehaviour
    {
        public Animator animator;
        public SkinnedMeshRenderer chassis, outfit, gear, head;
        public Rigidbody[] ragdoll;
        public Collider[] ragdollColliders;
        public Transform headBone, hips;
        public float detachedEnergy = 60, followRadius = 5;
        public bool IsDowned { get; private set; }
        public bool WearsHoodie { get; private set; } = true;
        public float Energy { get; private set; }
        public Transform ViewTarget => IsDowned ? detachedHead.transform : headBone;
        GameObject detachedHead;
        readonly List<Mesh> bakedMeshes = new();
        Transform[] bones;
        Vector3[] restPositions;
        Quaternion[] restRotations;
        CharacterController controller;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            bones = animator.GetComponentsInChildren<Transform>();
            restPositions = new Vector3[bones.Length];
            restRotations = new Quaternion[bones.Length];
            for (int i=0; i<bones.Length; i++) {
                restPositions[i]=bones[i].localPosition;
                restRotations[i]=bones[i].localRotation;
            }
            SetRagdoll(false);
            SetOutfit(outfit && outfit.enabled);
            // Keep this presentation object stable so recorded takes can track head separation.
            detachedHead=new GameObject("DetachedHead_"+name);
            detachedHead.transform.SetPositionAndRotation(headBone.position,headBone.rotation);
            var baked=new Mesh {name=head.name+"_Detached"};
            head.BakeMesh(baked);bakedMeshes.Add(baked);
            var part=new GameObject(head.name);part.layer=gameObject.layer;
            part.transform.SetPositionAndRotation(head.transform.position,head.transform.rotation);
            part.transform.localScale=head.transform.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh=baked;
            part.AddComponent<MeshRenderer>().sharedMaterials=head.sharedMaterials;
            part.transform.SetParent(detachedHead.transform,true);
            detachedHead.SetActive(false);
        }

        public void SetOutfit(bool hoodie)
        {
            if (IsDowned) return;
            WearsHoodie=hoodie;
            if (chassis) chassis.enabled=!hoodie;
            if (outfit) outfit.enabled=hoodie;
            if (gear) gear.enabled=hoodie;
        }

        public void SetRagdoll(bool active)
        {
            foreach (var c in ragdollColliders) c.enabled=active;
            // Animation or teleport can move the skeleton after the last physics step.
            if(active) Physics.SyncTransforms();
            foreach (var b in ragdoll) {
                b.isKinematic=!active;
                if (active) { b.linearVelocity=Vector3.zero; b.angularVelocity=Vector3.zero; }
            }
        }

        [ContextMenu("Play Mode / Knock down")]
        public void KnockDown()
        {
            if (!Application.isPlaying || IsDowned) return;
            IsDowned=true; Energy=detachedEnergy;
            animator.enabled=false;
            if (controller) controller.enabled=false;
            detachedHead.transform.SetPositionAndRotation(headBone.position,headBone.rotation);
            detachedHead.SetActive(true);head.enabled=false;
            SetRagdoll(true);
        }

        public void MoveHead(Vector3 velocity, float dt)
        {
            if (!IsDowned || !detachedHead || dt<=0) return;
            Energy=Mathf.Max(0,Energy-dt);
            var anchor=hips.position+Vector3.up*.8f;
            var next=detachedHead.transform.position+(Energy>0 ? velocity*dt : Vector3.down*.2f*dt);
            next=anchor+Vector3.ClampMagnitude(next-anchor,followRadius);
            next.y=Mathf.Clamp(next.y,.4f,2.6f);
            var delta=next-detachedHead.transform.position;
            if(delta.sqrMagnitude>0 && Physics.SphereCast(detachedHead.transform.position,.19f,delta.normalized,out var hit,delta.magnitude,~(1<<8),QueryTriggerInteraction.Ignore))
                next=detachedHead.transform.position+delta.normalized*Mathf.Max(0,hit.distance-.01f);
            detachedHead.transform.position=next;
        }

        [ContextMenu("Play Mode / Repair")]
        public void Repair()
        {
            if (!Application.isPlaying || !IsDowned) return;
            var pos=hips.position; pos.y=transform.position.y;
            SetRagdoll(false);
            transform.position=pos;
            for(int i=0;i<bones.Length;i++) {
                bones[i].localPosition=restPositions[i];
                bones[i].localRotation=restRotations[i];
            }
            detachedHead.SetActive(false);
            head.enabled=true;
            animator.enabled=true;animator.Rebind();animator.Update(0);
            if(controller)controller.enabled=true;
            IsDowned=false;
        }

        void OnDestroy()
        {
            if(detachedHead)Destroy(detachedHead);
            foreach(var m in bakedMeshes)Destroy(m);
        }
    }
}




