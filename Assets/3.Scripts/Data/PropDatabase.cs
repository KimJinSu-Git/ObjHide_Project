using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bird.Network.Data
{
    [CreateAssetMenu(fileName = "PropDatabase", menuName = "Bird/Prop Database")]
    public class PropDatabase : ScriptableObject
    {
        public List<BirdPropData> AllProps;
        
        public BirdPropData GetPropByID(int id) => AllProps.FirstOrDefault(p => p.PropID == id);
        
        public BirdPropData GetRandomProp() => AllProps[Random.Range(0, AllProps.Count)];
    }
}
