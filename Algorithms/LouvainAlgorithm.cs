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

using System.Collections.Generic;
using Networks.Core;

namespace Networks.Algorithms
{
    public partial class CommunityDetection
    {
        #region Louvain

        /// <summary>
        /// Replaces the main_louvain controller from the louvain source
        /// </summary>
        /// <param name="G">Network to process</param>
        /// <param name="metric">which quality metric class to use</param>
        /// <param name="improvement">improvement threshold</param>
        /// <returns></returns>
        public static List<HashSet<uint>> Louvain(Network G, LouvainMetric metric, double improvement = 0.000001)
        {
            List<HashSet<uint>> retVal = new List<HashSet<uint>>();
            LouvainGraph g = new LouvainGraph(G);
            ushort nb_calls = 1;
            List<uint> vertices = G.Vertices;
            for (int i = 0; i < G.Order; i++)
            {
                retVal.Add(new HashSet<uint>());
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
        public static List<HashSet<uint>> Louvain(Network G, double resolution, double improvement = 0.000001)
        {
            List<HashSet<uint>> retVal = new List<HashSet<uint>>();
            LouvainGraph g = new LouvainGraph(G);
            ushort nb_calls = 1;
            List<uint> vertices = G.Vertices;
            for (int i = 0; i < G.Order; i++)
            {
                retVal.Add(new HashSet<uint>());
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

    }
}
