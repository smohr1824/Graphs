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
using System.Threading.Tasks;
using Networks.Core;

namespace Networks.FCM
{
    public class MultilayerFuzzyCognitiveMap
    {
        private MultilayerNetwork model;
        public delegate float threshold(float f);
        private bool modifiedKosko;
        private threshold tfunc;
        private uint nextNodeId = 0;
        private bool dirty = false;
        public Dictionary<uint, MultilayerCognitiveConcept> Concepts { get; private set; }
        private Dictionary<string, uint> reverseLookup;

        public MultilayerFuzzyCognitiveMap(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions)
        {
            Concepts = new Dictionary<uint, MultilayerCognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new MultilayerNetwork(dimensions, true);
            tfunc = new threshold(bivalent);
            modifiedKosko = false;
        }

        public MultilayerFuzzyCognitiveMap(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, bool useModifiedKosko)
        {
            Concepts = new Dictionary<uint, MultilayerCognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new MultilayerNetwork(dimensions, true);
            tfunc = new threshold(bivalent);
            modifiedKosko = useModifiedKosko;
        }

        public MultilayerFuzzyCognitiveMap(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, threshold func)
        {
            Concepts = new Dictionary<uint, MultilayerCognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new MultilayerNetwork(dimensions, true);
            tfunc = func;
            modifiedKosko = false;
        }

        public MultilayerFuzzyCognitiveMap(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, threshold func, bool useModifiedKosko)
        {
            Concepts = new Dictionary<uint, MultilayerCognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new MultilayerNetwork(dimensions, true);
            tfunc = func;
            modifiedKosko = useModifiedKosko;
        }

        public List<string> ListConcepts()
        {
            return reverseLookup.Keys.ToList<string>();
        }

        public MLConceptState ReportConceptState(string conceptName)
        {
            uint id;
            MLConceptState retState = new MLConceptState();
            if (reverseLookup.TryGetValue(conceptName, out id))
            {
                MultilayerCognitiveConcept concept = Concepts[id];
                int dim = concept.LayerCount;
                List<string>[] retLayers = new List<string>[dim];
                float[] retLevels = new float[dim];

                int i = 0;
                foreach (List<string> layer in concept.GetLayers())
                {
                    retLayers[i] = layer;
                    retLevels[i] = concept.GetLayerActivationLevel(layer);
                    i++;
                }
                retState.AggregateLevel = concept.GetAggregateActivationLevel();
                retState.LayerLevels = retLevels;
                retState.Layers = retLayers;
            }
            return retState;
        }

        public FCMState ReportLayerState(List<string> layerCoords)
        {
            FCMState retVal = new FCMState();
            List<string> retConcepts = new List<string>();
            List<float> retLevels = new List<float>();

            foreach (KeyValuePair<uint, MultilayerCognitiveConcept> kvp in Concepts)
            {
                // add the activation level if found; if the concept does not participate in the layer, GetLayerActivationLevel throws an exception
                try
                {
                    float level = kvp.Value.GetLayerActivationLevel(layerCoords);
                    retConcepts.Add(kvp.Value.Name);
                    retLevels.Add(level);
                }
                catch (Exception)
                {
                    
                }
            }

            retVal.ConceptNames = retConcepts.ToArray();
            retVal.ActivationValues = retLevels.ToArray();
            return retVal;
        }

        // Adds a concept to an elementary layer
        // If it does not currently appear anywhere in the ML FCM, a new concept is added
        public bool AddConcept(string conceptName, List<string> coords, float level = 0.0F, float initial = 0.0F)
        {
            if (!reverseLookup.ContainsKey(conceptName))
            {
                MultilayerCognitiveConcept concept = new MultilayerCognitiveConcept(conceptName, initial, level);
                concept.SetLayerLevel(coords, level);
                Concepts.Add(nextNodeId, concept);
                reverseLookup.Add(conceptName, nextNodeId);
                NodeLayerTuple tuple = new NodeLayerTuple(nextNodeId, coords);
                if (!model.HasElementaryLayer(coords))
                    model.AddElementaryLayer(coords, new Network(true));
                model.AddVertex(tuple);
                nextNodeId++;
                dirty = true;
                return true;
            }
            else
            {
                uint existingKey = reverseLookup[conceptName];
                NodeLayerTuple tuple = new NodeLayerTuple(existingKey, coords);
                if (!model.HasVertex(tuple))
                {
                    if (!model.HasElementaryLayer(coords))
                        model.AddElementaryLayer(coords, new Network(true));

                    MultilayerCognitiveConcept concept = Concepts[existingKey];
                    concept.SetLayerLevel(coords, level);
                    model.AddVertex(tuple);
                    dirty = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        // delete a concept from the ML FCM entirely (i.e., from all elementary layers)
        public void DeleteConcept(string conceptName, List<string> coords)
        {
            uint id;
            if (reverseLookup.TryGetValue(conceptName, out id))
            {
                reverseLookup.Remove(conceptName);
                List<List<string>> layers = Concepts[id].GetLayers();
                foreach (List<string> layerList in layers)
                {
                    NodeLayerTuple tuple = new NodeLayerTuple(id, layerList);
                    model.RemoveVertex(tuple);
                }
                Concepts.Remove(id);
                dirty = true;
            }
        }

        public void AddInfluence(string influences, List<string> influencesCoords, string influenced, List<string> influencedCoords, float weight)
        {
            uint from, to;
            if (reverseLookup.TryGetValue(influences, out from) && reverseLookup.TryGetValue(influenced, out to))
            {
                NodeLayerTuple influencesTuple = new NodeLayerTuple(from, influencesCoords);
                NodeLayerTuple influencedTuple = new NodeLayerTuple(to, influencedCoords);

                try
                {
                    model.AddEdge(influencesTuple, influencedTuple, weight);
                }
                catch (Exception)
                {
                    return;
                }
                dirty = true;
            }
        }

        public void DeleteInfluence(string influences, List<string> influencesCoords, string influenced, List<string> influencedCoords)
        {
            uint from, to;
            if (reverseLookup.TryGetValue(influences, out from) && reverseLookup.TryGetValue(influenced, out to))
            {
                NodeLayerTuple influencesTuple = new NodeLayerTuple(from, influencesCoords);
                NodeLayerTuple influencedTuple = new NodeLayerTuple(to, influencedCoords);

                try
                {
                    model.RemoveEdge(influencesTuple, influencedTuple);
                }
                catch (Exception)
                {
                    return;
                }

                dirty = true;
            }
        }

        public float GetActivationLevel(string conceptName)
        {
            uint id;
            if (!reverseLookup.TryGetValue(conceptName, out id))
            {
                throw new ArgumentException($"Concept {conceptName} not found in network.");
            }

            // concept found, return value
            return Concepts[id].GetAggregateActivationLevel();
        }

        public float GetConceptLayerActivationLevel(string conceptName, List<string> coords)
        {
            uint id;
            if (!reverseLookup.TryGetValue(conceptName, out id))
            {
                throw new ArgumentException($"Concept {conceptName} not found in network.");
            }

            MultilayerCognitiveConcept concept = Concepts[id];

            // GetLayerActivationLevel throws an ArgumentException if the concept is not found in that layer
            float retVal = concept.GetLayerActivationLevel(coords);
            return retVal;
        }

        public void StepWalk()
        {
            Dictionary<uint, MultilayerCognitiveConcept> nextConcepts = new Dictionary<uint, MultilayerCognitiveConcept>();
            
            foreach (KeyValuePair<uint, MultilayerCognitiveConcept> kvp in Concepts)
            {
                MultilayerCognitiveConcept concept = kvp.Value;
                MultilayerCognitiveConcept next = new MultilayerCognitiveConcept(concept.Name);
                uint conceptId = kvp.Key;
                List<List<string>> layers = concept.GetLayers();
                foreach (List<string> layer in layers)
                {
                    NodeLayerTuple instance = new NodeLayerTuple(conceptId, layer);
                    Dictionary<NodeLayerTuple, float> sources = model.GetSources(instance, false);
                    float sum = 0.0F;
                    foreach (KeyValuePair<NodeLayerTuple, float> kvpT in sources)
                    {
                        sum += Concepts[kvpT.Key.nodeId].GetLayerActivationLevel(kvpT.Key.coordinates) * kvpT.Value;
                    }
                    if (modifiedKosko)
                    {
                        next.SetLayerLevel(layer, tfunc(Concepts[conceptId].GetLayerActivationLevel(layer) + sum));
                    }
                    else
                    {
                        next.SetLayerLevel(layer, tfunc(sum));
                    }

                }

                // finished all layer instances of a single concept, so now calculate the aggregate activation level and add the concept to the next gen dictionary
                float total = 0.0F;
                foreach (List<string> layer in next.GetLayers())
                {
                    total += next.GetLayerActivationLevel(layer);
                }

                if (modifiedKosko)
                {
                    next.ActivationLevel = tfunc(next.ActivationLevel + total);
                }
                else
                {
                    next.ActivationLevel = tfunc(total);
                }
                nextConcepts.Add(conceptId, next);
            }

            // argh! iterate through the partials and set the new values
            foreach (KeyValuePair<uint, MultilayerCognitiveConcept> kvp in nextConcepts)
            {
                Concepts[kvp.Key].ActivationLevel = kvp.Value.ActivationLevel;
                foreach (List<string> layer in kvp.Value.GetLayers())
                {
                    Concepts[kvp.Key].SetLayerLevel(layer, kvp.Value.GetLayerActivationLevel(layer));
                }
            }

        }
        private float bivalent(float f)
        {
            if (f > 0.0F)
            {
                return 1.0F;
            }
            else
            {
                return 0.0F;
            }
        }

        private float trivalent(float f)

        {
            if (f < -0.5F)
            {
                return -1.0F;
            }

            if (f >= 0.5F)
            {
                return 1.0F;
            }

            return 0.0F;
        }

        private float logistic(float f)
        {
            return (float)(1 / (1 + Math.Exp(-5 * f)));
        }
    }
}
