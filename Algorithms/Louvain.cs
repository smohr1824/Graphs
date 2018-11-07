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

/// This collapses Louvain_main and Louvain in the originator's SourceForge repository so that it works in the 
/// metaphor of the Algorithms namespace -- self-contained static algorithms that work with Network or MultilayerNetwork.
/// The original implementation is a collection of four console apps designed to be run in sequence with shared disk files.  Moreover,
/// the original implementation shows the progressive collapse of communities, but does not show how the original graph's nodes are ultimately assigned 
/// to communities.  Our other community detection algorithms do this and we leverage that to return a List of HashSets denoting the members of each community.
/// The steps needed to map string ids to integer node ids and collect the communities are memory intensive.
/// TODO: reimplement Louvain from scratch with better memory usage.
/// N.B. Louvain is conceptually similar to CIS.  The collapsed nodes in Louvain are essentially the expanding clusters of CIS, where the self-loops
/// of the former are the in-cluster edges of the latter.  It should be possible to simplify the code and eliminate the LouvainGraph class entirely.
/// This would have the benefit of not having to traverse the existing graph to build a new graph in the do-while loop in this class.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Algorithms
{
    public class Louvain
    {
        public List<double> neigh_weight;
        public List<int> neigh_pos;
        public int neigh_last;
        public int nb_pass;
        public double eps_impr;
        public LouvainQuality qual;

        public Louvain(int nbp, double epsq, LouvainQuality q)
        {
            qual = q;
            neigh_weight = new List<double>();
            neigh_pos = new List<int>();
            // just to be sure they are initialized...
            for (int i = 0; i < qual.size; i++)
            {
                neigh_weight.Add(-1);
                neigh_pos.Add(0);
            }

            nb_pass = nbp;
            eps_impr = epsq;
        }

        public void neigh_comm(int node)
        {
            for (int i = 0; i < neigh_last; i++)
                neigh_weight[neigh_pos[i]] = -1;

            neigh_last = 0;

            Tuple <List<int>, List<double>> p = (qual.g).neighbors(node);
            int deg = (qual.g).nb_neighbors(node);

            neigh_pos[0] = qual.n2c[node];
            neigh_weight[neigh_pos[0]] = 0;
            neigh_last = 1;

            for (int i = 0; i < deg; i++)
            {
                int neigh = p.Item1[i];
                int neigh_comm = qual.n2c[neigh];
                double neigh_w = ((qual.g).weights.Count() == 0) ? 1.0:(p.Item2[i]);

                if (neigh != node)
                {
                    if (neigh_weight[neigh_comm] == -1)
                    {
                        neigh_weight[neigh_comm] = 0.0;
                        neigh_pos[neigh_last++] = neigh_comm;
                    }
                    neigh_weight[neigh_comm] += neigh_w;
                }
            }
        }

        /// <summary>
        /// Create a new LouvainGraph such that the nodes are the communities detected at the preceding stage of the algorithm and the
        /// self-edges edges reflect the number of edges in the community so collapsed.
        /// </summary>
        /// <returns>Updated LouvainGraph</returns>
        public LouvainGraph partition2graph_binary()
        {
            // Renumber communities
            int [] renumber = new int[qual.size]; // source -1 inits, so comment out next lines
            for (int node = 0; node < qual.size; node++)
            {
                renumber[node] = -1;
            }

            for (int node = 0; node < qual.size; node++)
            {
                renumber[qual.n2c[node]]++;
            }

            int last = 0;
            for (int i = 0; i < qual.size; i++)
            {
                if (renumber[i] != -1)
                    renumber[i] = last++;
            }

            // Compute communities
            List<List<int>> comm_nodes = new List<List<int>>();
            List<int> comm_weight = new List<int>();
            for (int i = 0; i < last; i++)
            {
                comm_nodes.Add(new List<int>());
                comm_weight.Add(0);
            }
                

            for (int node = 0; node < (qual.size); node++)
            {
                comm_nodes[renumber[qual.n2c[node]]].Add(node);
                comm_weight[renumber[qual.n2c[node]]] += (qual.g).nodes_w[node];
            }

            // Compute weighted graph
            LouvainGraph g2 = new LouvainGraph();
            int nbc = comm_nodes.Count();

            g2.nb_nodes = comm_nodes.Count();
            g2.degrees = new List<ulong>();
            g2.nodes_w = new List<int>();
            for (int k = 0; k < nbc; k++)
            {
                g2.degrees.Add(0);
                g2.nodes_w.Add(0);
            }

            for (int comm = 0; comm < nbc; comm++)
            {
                Dictionary<int, double> m = new Dictionary<int, double>();

                int size_c = comm_nodes[comm].Count();

                g2.assign_weight(comm, comm_weight[comm]);

                for (int node = 0; node < size_c; node++)
                {
                    Tuple<List<int>, List<double>> p = (qual.g).neighbors(comm_nodes[comm][node]);
                    int deg = (qual.g).nb_neighbors(comm_nodes[comm][node]);
                    for (int i = 0; i < deg; i++)
                    {
                        int neigh = p.Item1[i];
                        int neigh_comm = renumber[qual.n2c[neigh]];
                        double neigh_weight = (((qual.g).weights.Count() == 0) ? 1.0:p.Item2[i]);

                        if(!m.ContainsKey(neigh_comm))
                            m.Add(neigh_comm, neigh_weight);
                        else
                            m[neigh_comm] += neigh_weight;
                    }
                }

                g2.degrees[comm] = (comm == 0) ? (ulong)m.Count() : g2.degrees[comm - 1] + (ulong)m.Count();
                g2.nb_links += (ulong)m.Count();

                foreach (KeyValuePair<int, double> kvp in m)
                {
                    g2.total_weight += kvp.Value;
                    g2.links.Add(kvp.Key);
                    g2.weights.Add(kvp.Value);
                }
            }

            return g2;
        }

        /// <summary>
        /// Perform one iteration of the Louvain algorithm
        /// </summary>
        /// <returns></returns>
        public bool one_level()
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            bool improvement = false;
            int nb_moves;
            int nb_pass_done = 0;
            double new_qual = qual.quality();
            double cur_qual = new_qual;

            List<int> random_order = new List<int>(); ;
            for (int i = 0; i < qual.size; i++)
                random_order.Add(i);
            for (int i = 0; i < qual.size - 1; i++)
            {
                int rand_pos = rand.Next() % (qual.size - i) + i;
                int tmp = random_order[i];
                random_order[i] = random_order[rand_pos];
                random_order[rand_pos] = tmp;
            }

            // repeat while 
            //   there is an improvement of quality
            //   or there is an improvement of quality greater than a given epsilon 
            //   or a predefined number of pass have been done
            do
            {
                cur_qual = new_qual;
                nb_moves = 0;
                nb_pass_done++;

                // for each node: remove the node from its community and insert it in the best community
                for (int node_tmp = 0; node_tmp < qual.size; node_tmp++)
                {
                    int node = random_order[node_tmp];
                    int node_comm = qual.n2c[node];
                    double w_degree = (qual.g).weighted_degree(node);

                    // computation of all neighboring communities of current node
                    neigh_comm(node);
                    // remove node from its current community
                    qual.remove(node, node_comm, neigh_weight[node_comm]);

                    // compute the nearest community for node
                    // default choice for future insertion is the former community
                    int best_comm = node_comm;
                    double best_nblinks = 0.0;
                    double best_increase = 0.0;
                    for (int i = 0; i < neigh_last; i++)
                    {
                        double increase = qual.gain(node, neigh_pos[i], neigh_weight[neigh_pos[i]], w_degree);
                        if (increase > best_increase)
                        {
                            best_comm = neigh_pos[i];
                            best_nblinks = neigh_weight[neigh_pos[i]];
                            best_increase = increase;
                        }
                    }

                    // insert node in the nearest community
                    qual.insert(node, best_comm, best_nblinks);

                    if (best_comm != node_comm)
                        nb_moves++;
                }

                new_qual = qual.quality();

                if (nb_moves > 0)
                    improvement = true;

            } while (nb_moves > 0 && new_qual - cur_qual > eps_impr);

            return improvement;
        }

        /// <summary>
        /// This is an adaptation of the method in the original Louvain source that displays communities and nodes such
        /// that the communities are maintained as HashSets of the original graph's vertices.  This method is essential to obtaining the 
        /// final list of communities.
        /// </summary>
        /// <param name="communities">List of HashSets denoting members of communities.  On the first pass, each vertex of the network occupies its own community.
        /// On subsequent passes, the existing communities are merged to reflect the new communties and the resulting list is returned.</param>
        /// <returns>List of hash sets of strings denoting the members of the current iteratrion's communities.</returns>
        public List<HashSet<string>> display_partition(List<HashSet<string>> communities)
        {
            List<int> renumber = new List<int>(); 
            for (int node = 0; node < qual.size; node++)
            {
                renumber.Add(-1);
            }

            for (int node = 0; node < qual.size; node++)
            {
                renumber[qual.n2c[node]]++; ;
            }


            int end = 0;
            for (int i = 0; i < qual.size; i++)
                if (renumber[i] != -1)
                    renumber[i] = end++;

            List<HashSet<string>> retVal = new List<HashSet<string>>();

            int commCt = qual.n2c.Max() + 1;
            for (int i = 0; i < commCt; i++)
                retVal.Add(new HashSet<string>());

            for (int i = 0; i < qual.size; i++)
            {
                retVal[renumber[qual.n2c[i]]].UnionWith(communities[i]);
            }

            return retVal;
        }
    }
}
