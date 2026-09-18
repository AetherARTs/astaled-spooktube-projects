using UnityEngine;

namespace SpookTuber
{
    public sealed class MissionGate : MonoBehaviour
    {
        public enum Action { Depart, Extract, Review, Edit }
        public Action action;
        public string Prompt=>action==Action.Depart?"E / Depart for hospital":action==Action.Extract?"E / Return home with the RV":action==Action.Edit?"E / Bobby's editing desk":"E / Review hospital footage";
        public bool Use(CrewMotor crew)
        {
            if(!RunSession.Current||!crew||crew.GetComponent<CrewBody>().IsDowned)return false;
            var point=GetComponent<Collider>().ClosestPoint(crew.GetComponent<CrewBody>().headBone.position);
            if(Vector3.Distance(point,crew.GetComponent<CrewBody>().headBone.position)>3.5f)return false;
            return action==Action.Depart?RunSession.Current.Depart():action==Action.Extract?RunSession.Current.BeginExtraction():action==Action.Edit?RunSession.Current.OpenStudio():RunSession.Current.ReviewLastMission();
        }
    }
}
