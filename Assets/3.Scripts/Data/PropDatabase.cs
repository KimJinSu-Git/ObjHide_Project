using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bird.Network.Data
{
    [CreateAssetMenu(fileName = "PropDatabase", menuName = "Bird/Prop Database")]
    public class PropDatabase : ScriptableObject
    {
        public List<BirdPropData> AllProps;
        
        public BirdPropData GetPropByID(int id) => AllProps.FirstOrDefault(p => p.PropID == id);
        
        public BirdPropData GetRandomProp() => AllProps[Random.Range(0, AllProps.Count)];

        public List<BirdPropData> GetRandomUniqueProps(int count, List<int> excludeIDs = null)
        {
            var query = AllProps.AsEnumerable();

            if (excludeIDs != null && excludeIDs.Count > 0)
            {
                query = query.Where(p => !excludeIDs.Contains(p.PropID));
            }
            
            return query.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
        }
    }
}
