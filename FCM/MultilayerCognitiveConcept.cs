using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networks.Core;

namespace Networks.FCM
{
    public class MultilayerCognitiveConcept : CognitiveConcept
    {
        // map of layer coordinates to the activation level of the concept in that elementary layer
        Dictionary<List<string>, float> layerActivationLevels;
        public MultilayerCognitiveConcept(string name, float initialValue = 0.0F, float level = 0.0F) : base(name, initialValue, level)
        {
            layerActivationLevels = new Dictionary<List<string>, float>(new UnresolvedCoordinateTupleEqualityComparer());
        }

        public int LayerCount { get { return layerActivationLevels.Keys.Count; } }
        public List<List<string>> GetLayers()
        {
            return layerActivationLevels.Keys.ToList();
        }

        public float GetAggregateActivationLevel()
        {
            return ActivationLevel;
        }

        public float GetLayerActivationLevel(List<string> coords)
        {
            float retVal;
            if (layerActivationLevels.TryGetValue(coords, out retVal))
            {
                return retVal;
            }
            else
            {
                throw new ArgumentException($"Concept {Name} not found in layer {string.Join(",", coords)}.");
            }
                
        }
        internal void SetLayerLevel(List<string> layerCoords, float level)
        {
            if (layerActivationLevels.ContainsKey(layerCoords))
            {
                layerActivationLevels[layerCoords] = level;
            }
            else
            {
                layerActivationLevels.Add(layerCoords, level);
            }
        }




    }
}
