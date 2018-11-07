// MIT License

// Copyright(c) 2017 - 2018 Stephen Mohr

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
//
// MathNet Numerics nuget used in SLPA copyright:
///Copyright(c) 2002-2015 Math.NET


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

/// This is an adaptation of the binary_graph class in the original Louvain code.  It is retained both for simplicity and so that the working graph 
/// can be collapsed as per the Louvain algorithm without destroying the original network.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networks.Core;

namespace Networks.Algorithms
{
    public class LouvainGraph
    {
        public int nb_nodes;
        public ulong nb_links;
        public double total_weight;
        public int sum_nodes_w;
        public List<ulong> degrees;
        public List<int> links;
        public List<double> weights;
        public List<int> nodes_w;


        public LouvainGraph()
        {
            nb_nodes = 0;
            nb_links = 0;

            total_weight = 0.0;
            sum_nodes_w = 0;
            degrees = new List<ulong>();
            links = new List<int>();
            weights = new List<double>();
            nodes_w = new List<int>();
        }

        public LouvainGraph(Network G)
        {
            degrees = new List<ulong>();
            links = new List<int>();
            weights = new List<double>();
            nodes_w = new List<int>();

            total_weight = 0;

            nb_nodes = G.Order;
            sum_nodes_w = nb_nodes;
            int sumDegree = 0;
            // get the list of vertices to aid in translating from string id to integer index
            List<string> vertices = G.Vertices;

            // populate the nodes, links, and weights collections
            for (int i = 0; i < G.Order; i++)
            {
                string vertex = vertices[i];
                nodes_w.Add(1);
                sumDegree += G.Degree(vertex);
                degrees.Add((ulong)sumDegree);
                Dictionary<string, float> neighbors = G.GetNeighbors(vertex);
                foreach (KeyValuePair<string, float> kvp in neighbors)
                {
                    links.Add(vertices.IndexOf(kvp.Key));
                    weights.Add(kvp.Value);
                }

            }

            // Compute the total weight
            for (int k = 0; k < nb_nodes; k++)
                total_weight += (double)weighted_degree(k);
        }

        public double max_weight()
        {
            return weights.Max();
        }

        public void assign_weight(int node, int weight)
        {
            sum_nodes_w -= nodes_w[node];
            nodes_w[node] = weight;
            sum_nodes_w += weight;
        }

        public void add_selfloops()
        {
            List<ulong> aux_degrees = new List<ulong>();
            List<int> aux_links = new List<int>();

            ulong sum_d = 0;

            for (int u = 0; u < nb_nodes; u++)
            {
                Tuple<List<int>, List<double>> p = neighbors(u);
                int deg = nb_neighbors(u);

                for (int i = 0; i < deg; i++)
                {
                    int neigh = p.Item1[i];
                    aux_links.Add(neigh);
                }
                sum_d += (ulong)deg;

                if (nb_selfloops(u) == 0)
                {
                    // add a self-loop
                    aux_links.Add(u);
                    sum_d += 1;
                }

                // add the new degree of vertex u
                aux_degrees.Add(sum_d);
            }

            links = aux_links;
            degrees = aux_degrees;

            nb_links += (ulong)nb_nodes;
        }
        public int nb_neighbors(int node)
        {
            if (node < 0 || node >= nb_nodes)
                throw new ArgumentException($"Incorrect vertex index in LouvainGraph, {node} passed, number of nodes in the graph is {nb_nodes}.");

            if (node == 0)
                return (int)degrees[0];
            else
                return (int)(degrees[node] - degrees[node - 1]);
        }

        /// <summary>
        /// Despite the name, this returns the weight of the first self-loop found -- should be the only one, with a weight
        /// equal to the number of neighboring nodes collapsed during an iteration of louvain
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double nb_selfloops(int node)
        {
            if (node < 0 || node >= nb_nodes)
                throw new ArgumentException($"Incorrect vertex index in LouvainGraph, {node} passed, number of nodes in the graph is {nb_nodes}.");

            Tuple<List<int>, List<double>> p = neighbors(node);
            for (int i = 0; i < nb_neighbors(node); i++)
            {
                if (p.Item1[i] == node)
                {
                    return p.Item2[i];
                }

            }
            return 0;
            
        }

        // get the sum of the weights of all outgoing links
        public double weighted_degree(int node)
        {
            if (node < 0 || node >= nb_nodes)
                throw new ArgumentException($"Incorrect vertex index in LouvainGraph, {node} passed, number of nodes in the graph is {nb_nodes}.");

            Tuple<List<int>, List<double>> p = neighbors(node);
            double res = 0;
            for (int i = 0; i < nb_neighbors(node); i++)
            {
                res += (double)(p.Item2[i]);
            }

            return res;
        }

        public Tuple<List<int>, List<double>> neighbors(int node)
        {
            if (node < 0 || node >= nb_nodes)
                throw new ArgumentException($"Incorrect vertex index in LouvainGraph, {node} passed, number of nodes in the graph is {nb_nodes}.");

            List<int> retNodes = new List<int>();
            List<double> retWeights = new List<double>();
            int beginningOffset = 0;
            if (node > 0)
                beginningOffset = (int)degrees[node - 1];
            for (int i = 0; i < nb_neighbors(node); i++)
            {
                retNodes.Add(links[beginningOffset + i]);
                retWeights.Add(weights[beginningOffset + i]);
            }
            Tuple<List<int>, List<double>> retVal = new Tuple<List<int>, List<double>>(retNodes, retWeights);
            return retVal;
        }

        /*public void display()
        {
            for (int node = 0; node < nb_nodes; node++)
            {
                Tuple<List<int>, List<double>> p = neighbors(node);
                Console.Write(node + ": ");
                for (int i = 0; i < nb_neighbors(node); i++)
                {
                    if (true)
                    {
                        if (weights.Count() != 0)
                            Console.Write(p.Item1[i] + " " + p.Item2[i]);
                        else
                            Console.Write(" " + p.Item1[i]);
                    }
                }
                Console.WriteLine();
            }
        }*/
    }
}
