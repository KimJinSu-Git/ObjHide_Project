using UnityEngine;

namespace Bird.Network.Data
{
    [CreateAssetMenu(fileName = "NewProp", menuName = "Bird/Prop Data") ]
    public class BirdPropData : ScriptableObject
    {
        public int PropID; // 고유 사물 번호
        public string PropName; // 사물 이름 (UI 표시용)
        public GameObject PropPrefab; // 실제 사물 모델링 

        public int MaxHP; // 사물에 따라 적용될 체력
        public Vector3 Center; // CharacterController의 중심점 보정값
        public float Radius; // 사물의 가로 폭 (충돌용)
        public float Height; // 사물의 세로 폭 (충돌용)
    }
}
