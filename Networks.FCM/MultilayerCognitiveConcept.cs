// MIT License

// Copyright(c) 2017 - 2019 Stephen Mohr 

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Networks.Core;

namespace Networks.FCM
{
    public class MultilayerCognitiveConcept : CognitiveConcept, IMultilayerCognitiveConcept
    {
        // map of layer coordinates to the activation level of the concept in that elementary layer
        private Dictionary<List<string>, float> layerActivationLevels;
        public MultilayerCognitiveConcept(string name, float initialValue = 0.0F, float level = 0.0F) : base(name, initialValue, level)
        {
            layerActivationLevels = new Dictionary<List<string>, float>(new UnresolvedCoordinateTupleEqualityComparer());
        }

        public IEnumerable<ILayerActivationLevel> LayerActivationLevels
        {
            get
            {
                List<ILayerActivationLevel> retVal = new List<ILayerActivationLevel>();
                foreach (List<string> coords in layerActivationLevels.Keys)
                {
                    string layerCoords = string.Join(",", coords.ToArray());
                    float value = GetLayerActivationLevel(coords);
                    retVal.Add(new layerActivationLevel(layerCoords, value));
                }
                return retVal.ToArray<ILayerActivationLevel>();
            }
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

        public float GetLayerActivationLevel(string coords)
        {
            char[] sep = { ',' };
            List<string> layerCoords = new List<string>(coords.Split(sep));
            return GetLayerActivationLevel(layerCoords);
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

        public void Reset()
        {
            List<string>[] keys = new List<string>[layerActivationLevels.Keys.Count];
            layerActivationLevels.Keys.CopyTo(keys, 0);
            foreach(List<string> coords in keys)
            {
                SetLayerLevel(coords, InitialValue);
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
