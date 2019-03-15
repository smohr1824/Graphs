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
using System.Threading.Tasks;
using System.Collections.Generic;
using Networks.Core;

namespace Networks.FCM
{
    public enum thresholdType
    {
        BIVALENT = 0,
        TRIVALENT = 1,
        LOGISTIC = 2
    } ;
    public class FuzzyCognitiveMap
    {
        public Dictionary<uint, CognitiveConcept> Concepts { get; private set; }
        private Dictionary<string, uint> reverseLookup;
        private Network model;
        private uint nextNodeId = 0;
        private bool dirty = false;
        private float[,] adjacencyMatrix = null;
        private uint[] currentKeys = null;
        public delegate float threshold(float f);
        private bool modifiedKosko;
        private threshold tfunc;

        #region constructors
        public FuzzyCognitiveMap()
        {
            Concepts = new Dictionary<uint, CognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new Network(true);
            tfunc = new threshold(bivalent);
            modifiedKosko = false;
        }

        public FuzzyCognitiveMap(bool useModifiedKosko)
        {
            Concepts = new Dictionary<uint, CognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new Network(true);
            tfunc = new threshold(bivalent);
            modifiedKosko = useModifiedKosko;
        }

        public FuzzyCognitiveMap(threshold func)
        {
            Concepts = new Dictionary<uint, CognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new Network(true);
            tfunc = func;
            modifiedKosko = false;
        }

        public FuzzyCognitiveMap(threshold func, bool useModifiedKosko)
        {
            Concepts = new Dictionary<uint, CognitiveConcept>();
            reverseLookup = new Dictionary<string, uint>();
            model = new Network(true);
            tfunc = func;
            modifiedKosko = useModifiedKosko;
        }
        #endregion

        #region public methods
        public bool AddConcept(string conceptName, float initial = 0.0F, float level = 0.0F)
        {
            if (!reverseLookup.ContainsKey(conceptName))
            {
                Concepts.Add(nextNodeId, new CognitiveConcept(conceptName, initial, level));
                reverseLookup.Add(conceptName, nextNodeId);
                model.AddVertex(nextNodeId);
                nextNodeId++;
                dirty = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void DeleteConcept(string conceptName)
        {
            uint id;
            if (reverseLookup.TryGetValue(conceptName, out id))
            {
                reverseLookup.Remove(conceptName);
                Concepts.Remove(id);
                model.RemoveVertex(id);
                dirty = true;
            }
        }

        public void AddInfluence(string influences, string influenced, float weight)
        {
            uint from, to;
            if (reverseLookup.TryGetValue(influences, out from) && reverseLookup.TryGetValue(influenced, out to))
            {
                model.AddEdge(from, to, weight);
                dirty = true;
            }
        }

        public void DeleteInfluence(string influences, string influenced)
        {
            uint from, to;
            if (reverseLookup.TryGetValue(influences, out from) && reverseLookup.TryGetValue(influenced, out to))
            {
                model.RemoveEdge(from, to);
                dirty = true;
            }
        }

        public float GetActivationLevel(string conceptName)
        {
            uint key;
            float retVal = 0.0F;
            if (reverseLookup.TryGetValue(conceptName, out key))
            {
                retVal = Concepts[key].ActivationLevel;
            }
            else
            {
                throw new Exception(conceptName + " not found in map");
            }
            return retVal;
        }

        public void Step()
        {
            if (dirty)
                Prepare();

            if (Concepts.Keys.Count > 100)
                ParallelMultiply(modifiedKosko);
            else
                Multiply(modifiedKosko);
        }

        public FCMState ReportState()
        {
            int dim = Concepts.Keys.Count;
            string[] retConcepts = new string[dim];
            float[] retValues = new float[dim];

            int i = 0;
            foreach (KeyValuePair<uint, CognitiveConcept> kvp in Concepts)
            {
                retConcepts[i] = kvp.Value.Name;
                retValues[i] = kvp.Value.ActivationLevel;
                i++;
            }
            FCMState retState = new FCMState();
            retState.ConceptNames = retConcepts;
            retState.ActivationValues = retValues;
            return retState;
        }

        public void Reset()
        {
            foreach (KeyValuePair<uint, CognitiveConcept> kvp in Concepts)
            {
                kvp.Value.ActivationLevel = kvp.Value.InitialValue;
            }
            Prepare();
        }

        public void SetThresholdFunction(threshold func)
        {
            tfunc = func;
        }

        public void SwitchThresholdFunction(thresholdType desiredFunc)
        {
            switch (desiredFunc)
            {
                case thresholdType.BIVALENT:
                    tfunc = new threshold(bivalent);
                    break;

                case thresholdType.TRIVALENT:
                    tfunc = new threshold(trivalent);
                    break;

                case thresholdType.LOGISTIC:
                    tfunc = new threshold(logistic);
                    break;
            }
        }

        public void SetActivationRule(bool useModifiedKosko)
        {
            modifiedKosko = useModifiedKosko;
        }
        #endregion

        private float[] MakeConceptVector()
        {
            float[] vector = new float[Concepts.Keys.Count];

            // the supra-adjacency matrix is constructed in ascending order of the uint id assigned to each concept, hence we MUST use Concepts to correctly match matrix elements to
            // named concepts
            int dim = Concepts.Keys.Count;
            for (uint i = 0; i < dim; i++)
            {
                vector[i] = Concepts[i].ActivationLevel;
            }
            return vector;
        }

        private void AssignConceptVector(float[] vector)
        {
            if (vector.Length == Concepts.Keys.Count)
            {
                for (uint i = 0; i < vector.Length; i++)
                {
                    Concepts[i].ActivationLevel = vector[i];
                }
            }
        }

        private void Multiply(bool modified)
        {
            int dim = Concepts.Keys.Count;
            float[] newConceptVector = new float[dim];

            // This hideous bit is due to a. the ability to add and delete Concepts, b. Concepts are id'd in the graph with uint's, and c. the adjacency matrix is constructed
            // in ascending sort order -- the id's may not be contiguous due to deletes, but they will be in order. 

            for (uint i = 0; i < dim; i++)
            {
                float sum = 0.0F;
                for (uint j = 0; j < dim; j++)
                {

                    
                    sum += Concepts[currentKeys[j]].ActivationLevel * adjacencyMatrix[j, i]; 
                }
                if (modified)
                {
                    newConceptVector[i] = tfunc(Concepts[i].ActivationLevel + sum);
                }
                else
                {
                    newConceptVector[i] = tfunc(sum);
                }
            }

            for (int k = 0; k < dim; k++)
                Concepts[currentKeys[k]].ActivationLevel = newConceptVector[k];

        }

        private void ParallelMultiply(bool modified)
        {

            int dim = Concepts.Keys.Count;
            float[] newConceptVector = new float[dim];

            var result = Parallel.For(0, dim, i =>
            {
                float sum = 0.0F;
                for (uint j = 0; j < dim; ++j) // each col of B
                {
                    sum += Concepts[j].ActivationLevel * adjacencyMatrix[j, i];
                }
                if (modified)
                {
                    newConceptVector[i] = tfunc(Concepts[(uint)i].ActivationLevel + sum);
                }
                else
                {
                    newConceptVector[i] = tfunc(sum);
                }
            }
            );

            for (int k = 0; k < dim; k++)
                Concepts[currentKeys[k]].ActivationLevel = newConceptVector[k];
        }

        private void Prepare()
        {
            adjacencyMatrix = model.AdjacencyMatrix;
            uint[] keys = new uint[Concepts.Count];
            Concepts.Keys.CopyTo(keys, 0);
            List<uint> keySorter = new List<uint>(keys);
            keySorter.Sort();
            currentKeys = keySorter.ToArray();

            dirty = false;
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
