// Copyright 2017 -- Stephen T. Mohr, OSIsoft, LLC
// Licensed under the MIT license
//
// MathNet Numerics nuget used in SLPA copyright:
///Copyright(c) 2002-2015 Math.NET

///Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

/// Code for the Louvain  algorithm is subject to the following notices:
/// // Community detection
// Based on the article "Fast unfolding of community hierarchies in large networks"
// Copyright (C) 2008 V. Blondel, J.-L. Guillaume, R. Lambiotte, E. Lefebvre
//
// This file is part of Louvain algorithm.
// 
// Louvain algorithm is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Louvain algorithm is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with Louvain algorithm.  If not, see <http://www.gnu.org/licenses/>.

/// Algorithms for community detection in networks
/// Alogorithms are Speaker-Listener Propagation Alogirthm, Connected Iterative Scan (CIS), and Louvain.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using Networks.Core;

namespace Networks.Algorithms
{
    public class CommunityDetection
    {
        #region CIS
        // ported from http://www.cs.rpi.edu/~magdon/LFDlabpublic.html/software/CIS/CIS.tar.gz
        // based on "Finding Overlapping Communities in Social Networks", Goldberg, Kelley, Magdon-Ismail, Mertsalov, Wallace, 2010
        // Takes a seed community (may be a single vertex) and expands it by neighboring vertices if doing so improves the community metric
        public static void CISExpandSeed(ref HashSet<string> seed, Network G, double lambda)
        {
            HashSet<string> fringe = new HashSet<string>();
            Dictionary<string, Tuple<float, float>> members = new Dictionary<string, Tuple<float, float>>();
            Dictionary<string, Tuple<float, float>> neighbors = new Dictionary<string, Tuple<float, float>>();
            float seedWeightIn = 0;
            float seedWeightOut = 0;

            foreach(string node in seed)
            {  
                //Add members of the seed, calculating individual
               // Win and Wout measures and noting neighborhood
                Dictionary<string, float> N = G.GetNeighbors(node);
                float Win = 0.0F, Wout = 0.0F;
                foreach (KeyValuePair<string, float> vertex in N)
                {
                    if (seed.Contains(vertex.Key))
                    {     
                        //If the neighbor is also in the seed, increase weight_in
                        Win += vertex.Value;
                        seedWeightIn += vertex.Value;
                    }
                    else
                    {                                            //Else increase weight out
                        Wout += vertex.Value;
                        seedWeightOut += vertex.Value;
                        fringe.Add(vertex.Key);
                    }
                }

                members.Add(node, new Tuple<float, float>(Win, Wout));

            }

            seedWeightIn /= 2;  //Internal edges were counted twice (assumed undirected)

            foreach (string node in fringe)
            { //Tally same information for neighborhood
                Dictionary<string, float> N = G.GetNeighbors(node);
                float Win = 0F, Wout = 0F;
                foreach(KeyValuePair<string, float> vertex in N)
                {
                    if (seed.Contains(vertex.Key))
                    {
                        Win += vertex.Value;
                    }
                    else
                    {
                        Wout += vertex.Value;
                    }
                }

                neighbors.Add(node, new Tuple<float, float>(Win, Wout));

            }

            bool changed = true;

            //While the seed cluster is changing, add new members and remove poor members
            while (changed)
            {
                changed = false;
                List<string> to_check = new List<string>();
                List<HashSet<string>> order_by_degree = new List<HashSet<string>>();

                foreach (KeyValuePair<string, Tuple<float, float>> neighbor in neighbors)
                {
                    int deg = G.Degree(neighbor.Key);
                    if (order_by_degree.Count() < deg + 1)
                    {
                        // expand the list so we can insert the vertex at the location corresponding its degree

                        order_by_degree.Capacity = deg + 1;
                        for (int k = 0; k < deg + 1; k++)
                            order_by_degree.Add(new HashSet<string>());
                    }
                    order_by_degree[deg].Add(neighbor.Key);
                }

                for (int k = 0; k < order_by_degree.Count(); k++)
                {
                    foreach (string vertex in order_by_degree[k])
                    {
                        to_check.Add(vertex);
                    }
                }

                for (int i = 0; i < to_check.Count(); i++)
                { 
                    Tuple<float, float> neighborWts = neighbors[to_check[i]];
                        
                    // Add the vertex if it increases the density metric
                    if (CalcDensity(seed.Count(), seedWeightIn, seedWeightOut, lambda) < CalcDensity(seed.Count() + 1, seedWeightIn + neighborWts.Item1, seedWeightOut + neighborWts.Item2 - neighborWts.Item1, lambda))
                    {
                        // flag that the seed cluster has changed
                        changed = true; 
                        seedWeightIn += neighborWts.Item1;
                        seedWeightOut = seedWeightOut - neighborWts.Item1 + neighborWts.Item2;
                        seed.Add(to_check[i]);  

                        // add to members and remove from neighbors

                        members.Add(to_check[i], neighborWts);
                        neighbors.Remove(to_check[i]); 

                        // Because the seed cluster has changed, the values of weights within the seed and going out of the seed have changed.
                        // Update the member and neighbor lists to reflect this.

                        Dictionary<string, float> N = G.GetNeighbors(to_check[i]);

                        foreach (KeyValuePair<string, float> neighbor in N)
                        {
                            Tuple<float, float> weights;
                            if (members.TryGetValue(neighbor.Key, out weights))
                            { 
                                Tuple<float, float> updatedWeights = new Tuple<float, float>(weights.Item1 + neighbor.Value, weights.Item2 - neighbor.Value);
                                members[neighbor.Key] = updatedWeights;
                            }
                            else if (neighbors.TryGetValue(neighbor.Key, out weights))
                            { 
                                Tuple<float, float> updatedWeights = new Tuple<float, float>(weights.Item1 + neighbor.Value, weights.Item2 - neighbor.Value);
                                neighbors[neighbor.Key] = updatedWeights;
                            }
                            else
                            { 
                                // Not found in either, so add it
                                Dictionary<string, float> N2 = G.GetNeighbors(neighbor.Key);

                                float newWin = 0F, newWout = 0F;
                                foreach (KeyValuePair<string, float> neighbor2 in N2)
                                {
                                    Tuple<float, float> wts;
                                    if (members.TryGetValue(neighbor2.Key, out wts))
                                        newWin += neighbor2.Value;
                                    else
                                        newWout += neighbor2.Value;
                                }

                                neighbors.Add(neighbor.Key, new Tuple<float, float>(newWin, newWout));
                            }
                        }
                    }
                }

                // Repeat scan for members
                to_check.Clear();
                order_by_degree.Clear();

                foreach (KeyValuePair<string, Tuple<float, float>> member in members)
                {
                    int deg = G.Degree(member.Key);
                    if (order_by_degree.Count() < deg + 1)
                    {
                        order_by_degree.Capacity = deg + 1;

                        for (int k = 0; k < deg + 1; k++)
                            order_by_degree.Add(new HashSet<string>());
                    }
                    order_by_degree[deg].Add(member.Key);
                }

                for (int k = 0; k < order_by_degree.Count(); k++)
                {
                    foreach (string node in order_by_degree[k])
                    {
                        to_check.Add(node);
                    }
                }

                for (int i = 0; i < to_check.Count(); i++)
                {
                    Tuple<float, float> weights;

                    members.TryGetValue(to_check[i], out weights);
                    double density1 = CalcDensity(seed.Count(), seedWeightIn, seedWeightOut, lambda);
                    double density2 = CalcDensity(seed.Count() - 1, seedWeightIn - weights.Item1, seedWeightOut - weights.Item2 + weights.Item1, lambda);
                    if (density1 < density2)
                    {
                        changed = true;
                        seedWeightIn -= weights.Item1;
                        seedWeightOut = seedWeightOut + weights.Item1 - weights.Item2;
                        seed.Remove(to_check[i]);
                        neighbors.Add(to_check[i], weights);
                        members.Remove(to_check[i]);

                        // update member and neighbor weights
                        Dictionary<string, float> N = G.GetNeighbors(to_check[i]);

                        foreach(KeyValuePair<string, float> neighbor in N)
                        {
                            Tuple<float, float> d2Wts;
                            if (members.TryGetValue(neighbor.Key, out d2Wts))
                            { 
                                members[neighbor.Key] = new Tuple<float, float>(d2Wts.Item1 - neighbor.Value, d2Wts.Item2 + neighbor.Value);
                            }
                            else if (neighbors.TryGetValue(neighbor.Key, out d2Wts))
                            { 
                                neighbors[neighbor.Key] = new Tuple<float, float>(d2Wts.Item1 - neighbor.Value, d2Wts.Item2 + neighbor.Value);
                            } 
                        }
                    }
                }

                //Get best component to move forward with
                SortedDictionary<double, HashSet<string>> comps = Components(seed, G, lambda);
                seed = comps.First().Value;
            }

        }

        #endregion CIS

        #region SLPA
        // algorithm from "Towards Linear Time Overlapping Community Detection in Social Networks", 2012, Jierui Xie and Boleslaw Szymanski
        // 
        /// <summary>
        /// Identifies overlapping or disjoint communities of a graph using the SLPA.
        /// </summary>
        /// <param name="G">Network in which to find communities</param>
        /// <param name="iterations">number of speaker-listener iterations to perform -- 20 has shown good convergence</param>
        /// <param name="inclusionThreshold">decimal percentage of the overall total number of labels heard below which a given label will be excluded from the returned set of communities</param>
        /// <param name="seed">Random seed -- passing the same seed leads to the same set of communities</param>
        /// <returns></returns>
        static public List<HashSet<string>> SLPA(Network G, int iterations, double inclusionThreshold, int seed)
        {
            Random randSrc = new Random(seed);
            List<string> vertices = G.Vertices;
            Dictionary<string,Dictionary<int, int>> nodeLabelMemory = new Dictionary<string, Dictionary<int, int>>();

            InitLabels(G, ref nodeLabelMemory);

            // perform the requested number of passes
            for (int i = 0; i < iterations; i++)
            {
                // shuffle the vertex order to avoid bias toward later vertices
                int[] order = Combinatorics.GeneratePermutation(G.Order, randSrc);

                // each pass has each vertex function as a listener
                foreach(int nodeIdx in order)
                {
                    Dictionary<string, float> adjacencyList = G.GetNeighbors(vertices[nodeIdx]);

                    // keep track of the labels sent to the listener
                    Dictionary<int, int> labelsSeen = new Dictionary<int, int>();

                    // iterate through all neighbors of the listener vertex
                    foreach (string neighbor in adjacencyList.Keys)
                    {
                        // Obtain the labels seen by the neighbor and make a random selection weighted by the frequency seen
                        Dictionary<int, int> labelDict;
                        nodeLabelMemory.TryGetValue(neighbor, out labelDict);
                        int sumLabels = SumLabels(labelDict);
                        double[] probs = GetProbabilities(labelDict, sumLabels);
                        Multinomial dist = new Multinomial(probs, 1, randSrc);
                        int[] sample = dist.Sample();
                        int maxSeen = Array.IndexOf(sample, sample.Max<int>());
                        int label = labelDict.Keys.ToList<int>()[maxSeen];

                        // update the labels seen by the listener so far
                        int count;
                        if (!labelsSeen.TryGetValue(label, out count))
                            labelsSeen.Add(label, 1);
                        else
                            labelsSeen[label] = ++count;

                    }

                    // find the maximum number of times any label has been seen
                    Dictionary<int, int> listenerDict = nodeLabelMemory[vertices[nodeIdx]];
                    int max = labelsSeen.Values.Max();

                    // retrieve all labels whose value equals the maximum count seen
                    IEnumerable<KeyValuePair<int, int>> maxLabels = labelsSeen.Where(x => x.Value == max);
                    int maxLabel;

                    // take the label seen most often; if there is a tie, select one label at random
                    if (maxLabels.Count() == 1)
                        maxLabel = maxLabels.First().Key;
                    else
                    {
                        maxLabel = labelsSeen.Keys.ElementAt(randSrc.Next(maxLabels.Count()));
                    }

                    // update the listener's dictionary of labels
                    int timesSeen;
                    if (listenerDict.TryGetValue(maxLabel, out timesSeen))
                    {
                        listenerDict[maxLabel] = ++timesSeen;
                    }
                    else
                    {
                        listenerDict.Add(maxLabel, 1);
                    }
                }
            }
                    

            return PostProcess(G, nodeLabelMemory, inclusionThreshold);
        }

        #endregion

        #region Louvain

        /// <summary>
        /// Replaces the main_louvain controller from the louvain source
        /// </summary>
        /// <param name="G">Network to process</param>
        /// <param name="metric">which quality metric class to use</param>
        /// <param name="improvement">improvement threshold</param>
        /// <returns></returns>
        public static List<HashSet<string>> Louvain(Network G, LouvainMetric metric, double improvement = 0.000001)
        {
            List<HashSet<string>> retVal = new List<HashSet<string>>();
            LouvainGraph g = new LouvainGraph(G);
            ushort nb_calls = 1;
            List<string> vertices = G.Vertices;
            for (int i = 0; i < G.Order; i++)
            {
                retVal.Add(new HashSet<string>());
                retVal[i].Add(vertices[i]);
            }

            LouvainQuality q = MakeMetric(g, metric);

            Louvain c = new Louvain(-1, improvement, q);

            bool improved = true;
            double quality = c.qual.quality();
            double new_qual;

            do
            {
                improved = c.one_level();
                new_qual = c.qual.quality();

                retVal = c.display_partition(retVal);

                g = c.partition2graph_binary();
                q = MakeMetric(g, metric);
                nb_calls++;

                c = new Louvain(-1, improvement, q);

                quality = new_qual;
            } while (improved);
            return retVal;
        }

        /// <summary>
        /// Louvain algorithm with the greedy resolution algorithm specified
        /// </summary>
        /// <param name="G">Network to process</param>
        /// <param name="resolution">theoretically on the range -1..1, but typically 0.5..1, with r=1 equivalent to the Modularity metric</param>
        /// <param name="improvement">minimum improvement threshold to continue</param>
        /// <returns></returns>
        public static List<HashSet<string>> Louvain(Network G, double resolution, double improvement = 0.000001)
        {
            List<HashSet<string>> retVal = new List<HashSet<string>>();
            LouvainGraph g = new LouvainGraph(G);
            ushort nb_calls = 1;
            List<string> vertices = G.Vertices;
            for (int i = 0; i < G.Order; i++)
            {
                retVal.Add(new HashSet<string>());
                retVal[i].Add(vertices[i]);
            }

            LouvainQuality q = MakeMetric(g, LouvainMetric.Resolution, resolution);

            Louvain c = new Louvain(-1, improvement, q);

            bool improved = true;
            double quality = c.qual.quality();
            double new_qual;

            do
            {
                improved = c.one_level();
                new_qual = c.qual.quality();

                retVal = c.display_partition(retVal);

                g = c.partition2graph_binary();
                q = MakeMetric(g, LouvainMetric.Resolution, resolution);
                nb_calls++;

                c = new Louvain(-1, improvement, q);

                quality = new_qual;
            } while (improved);
            return retVal;
        }

        #endregion Louvain

        #region private helpers for Louvain

        /// <summary>
        /// Create the appropriate metric instance -- need to do this at every iteration to account for the change in weights and communities/nodes
        /// </summary>
        /// <param name="g">Louvain graph instance</param>
        /// <param name="metric">enumerated metric desired</param>
        /// <param name="resolution">resolution (only used with LouvainMetric.Resolution</param>
        /// <returns></returns>
        private static LouvainQuality MakeMetric(LouvainGraph g, LouvainMetric metric, double resolution = 1.0)
        {
            LouvainQuality q;
            switch (metric)
            {
                case LouvainMetric.Modularity:
                    q = new LouvainModularity(g);
                    break;

                case LouvainMetric.Goldberg:
                    q = new LouvainGoldberg(g, g.max_weight());
                    break;

                case LouvainMetric.Resolution:
                    q = new LouvainResolution(g, resolution);
                    break;

                default:
                    q = new LouvainModularity(g);
                    break;
            }

            return q;
        }
        #endregion Louvain
        #region private helpers for CIS
        /// <summary>
        /// Gets the connected components of a seed cluster
        /// </summary>
        /// <param name="seedCluster">seed for the partitioning</param>
        /// <param name="G">network to be partitioned</param>
        /// <param name="lambda">relative weighting between density measures</param>
        /// <returns>sorted dictionary of components sorted by density</returns>
        private static SortedDictionary<double, HashSet<string>> Components(HashSet<string> seedCluster, Network G, double lambda)
        {
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> seen = new HashSet<string>();
            SortedDictionary<double, HashSet<string>> retVal = new SortedDictionary<double, HashSet<string>>();
            
            // running total of edge weights within the component and from the component to outside the component
            double wIn = 0, wOut = 0;

            foreach(string node in seedCluster)
            {
                wIn = 0;
                wOut = 0;
                HashSet<string> component = new HashSet<string>();
                HashSet<string> toCheck = new HashSet<string>();

                if (seen.Contains(node))
                    continue;

                toCheck.Add(node);

                while (toCheck.Count() != 0)
                {
                    string first = toCheck.First();

                    component.Add(first);
                    seen.Add(first);

                    Dictionary<string, float> Neighborhood = G.GetNeighbors(first);
                    toCheck.Remove(first);

                    // expand by the neighbors of the node to check; must not have been previously seen and must be part of the seed cluster
                    foreach(KeyValuePair<string, float> edge in Neighborhood)
                    {
                        if (!seen.Contains(edge.Key) && seedCluster.Contains(edge.Key))
                        {
                            toCheck.Add(edge.Key);
                            wIn += edge.Value;
                        }
                        else
                        {
                            wOut += edge.Value;
                        }
                    }

                }

                // get the component's density and add the pair to the return dictionary
                double density = CalcDensity(component.Count(), wIn, wOut, lambda);
                retVal.Add(density, component);
            }

            return retVal;

        }
        /// <summary>
        /// Calculates the density of a set of nodes in a network 
        /// </summary>
        /// <param name="size">number of nodes in the network</param>
        /// <param name="win">weight of edges wholly within the set</param>
        /// <param name="wout">weight of edges with only one end in the set</param>
        /// <param name="lambda">weight of the 2win metric versus 1 - lambda for the (win/win + wout) metric</param>
        /// <returns>denisty of the set</returns>
        private static double CalcDensity(int size, double win, double wout, double lambda)
        {
            if (size < 1)
                return Double.MinValue;

            // original density metric
            double partA = ((1 - lambda) * (win / (win + wout)));

            // improved density metric
            double partB = (lambda * ((2 * win) / (size * (size - 1))));
            if (size == 1) partB = lambda;

            return partA + partB;
        }

        #endregion private helpers for CIS

        #region private helpers for SLPA

        /// <summary>
        /// Initialize the labels for each vertex using the ordinal position of the vertex in G as the label
        /// </summary>
        /// <param name="G">Graph</param>
        /// <param name="communityLabels">Dictionary of vertex ids and their associated dictionary of weighted labels</param>
        private static void InitLabels(Network G, ref Dictionary<string, Dictionary<int, int>> communityLabels)
        {
            List<string> vertices = G.Vertices;
            int i = 0;
            foreach (string id in vertices)
            {
                Dictionary<int, int> labels = new Dictionary<int, int>();
                labels.Add(i++, 1);
                communityLabels.Add(id, labels);
            }
        }

        /// <summary>
        /// Pot-processing for SLPA.  Apply the threshold and construct communities given the dictionary of vertex ids and their associated labels
        /// </summary>
        /// <param name="G">graph</param>
        /// <param name="communityLabels">Dictionary of labels and the number of times the label was seen</param>
        /// <param name="threshold">Threshold [0..1.0] such that if a label's ratio of occurences to total label occurences is less than threshold, the labeled community is dropped. Threshold >= 0.5 results in disjoint communities.</param>
        /// <param name="minCommunitySize">Minimum size community to return (default is 2)</param>
        /// <returns>List of communities</returns>
        private static List<HashSet<string>> PostProcess(Network G, Dictionary<string, Dictionary<int, int>> communityLabels, double threshold, int minCommunitySize = 2)
        {
            foreach (Dictionary<int, int> dict in communityLabels.Values)
                ApplyThreshold(dict, threshold);

            // Iterate through the surviving (significant) communities and place them in sets based on labels, i.e., assign vertices to communities
            Dictionary<int, HashSet<string>> communities = new Dictionary<int, HashSet<string>>();
            foreach (KeyValuePair<string, Dictionary<int, int>> vertexPair in communityLabels)
            {
                HashSet<string> community;
                foreach (int label in vertexPair.Value.Keys)
                {
                    if (communities.TryGetValue(label, out community))
                    {
                        community.Add(vertexPair.Key);
                    }
                    else
                    {
                        HashSet<string> newCommunity = new HashSet<string>();
                        newCommunity.Add(vertexPair.Key);
                        communities.Add(label, newCommunity);
                    }
                }
            }
            return communities.Values.Where(p=>p.Count >= minCommunitySize).ToList<HashSet<string>>();
        }

        /// <summary>
        /// For a given list of labels, eliminate the labels appearing less than threshold % of the number of label occurrences
        /// </summary>
        /// <param name="dict">Dictionary of labels and the number of times the label was seen by a given vertex</param>
        /// <param name="threshold">Percentage below which to eliminate communities</param>
        private static void ApplyThreshold(Dictionary<int, int> dict, double threshold)
        {
            // Calculate the total number of times this vertex saw any label
            int valSum = SumLabels(dict);

            // Iterate through the dictionary removing any labels below threshold
            List<KeyValuePair<int, int>> d2 = dict.ToList<KeyValuePair<int, int>>();
            foreach (KeyValuePair<int, int> kvp in d2)
            {
                if (kvp.Value < valSum * threshold)
                    dict.Remove(kvp.Key);
            }
        }

        /// <summary>
        /// Calculates the total number of times a label was seen/heard by a given vertex
        /// </summary>
        /// <param name="labelsDict">Dictionary of labels and their occurences</param>
        /// <returns>total count</returns>
        private static int SumLabels(Dictionary<int, int> labelsDict)
        {
            int retVal = 0;
            foreach (int i in labelsDict.Keys)
            {
                int val;
                if (labelsDict.TryGetValue(i, out val)) // and it bloody well ought to work
                    retVal += val;
            }

            return retVal;
        }

        /// <summary>
        /// Calculates the probability of seeing a label based on its number of occurences relative to the total count (used to weight label selection)
        /// </summary>
        /// <param name="labelsDict">Dictionary of labels and the number of times the label was seen</param>
        /// <param name="sum">Total count of label occurrences</param>
        /// <returns>Array of probabilities</returns>
        private static double[] GetProbabilities(Dictionary<int, int> labelsDict, int sum)
        {
            double[] probs = new double[labelsDict.Keys.Count];
            int idx = 0;
            foreach (int key in labelsDict.Keys)
            {
                probs[idx] = labelsDict[key] / (double)sum;
                idx++;
            }

            return probs;
        }
        #endregion


    }
}
