using System;
using UnityEngine;

namespace SpookTuber
{
    public static class WorldNoise
    {
        public static event Action<Vector3,float,string> Emitted;
        public static void Emit(Vector3 position,float radius,string kind,Surgeon source=null)
        {
            if(radius<=0||!float.IsFinite(radius)||RunSession.Current&&RunSession.Current.Phase==RunSession.RunPhase.Review)return;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Surgeon>(FindObjectsSortMode.None))if(enemy!=source)enemy.Hear(position,Mathf.Min(radius,35));
            Emitted?.Invoke(position,radius,kind);
        }
    }
}
