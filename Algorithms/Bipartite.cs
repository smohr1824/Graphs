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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networks;
using Networks.Core;

namespace Networks.Algorithms
{
    public partial class Bipartite
    {
        private enum color
        {
            uncolored = 0,
            red = 1,
            blue = 2
        }

        /// <summary>
        /// Determines if G is bipartite by attempting a two-coloring of the network.
        /// </summary>
        /// <param name="G">network to test</param>
        /// <param name="Red">if bipartite, list of vertices in one disjoint set</param>
        /// <param name="Blue">if bipartite, list of vertices in the other disjoint set</param>
        /// <returns>true if G is bipartite, false otherwise. If bipartite, the two disjoint sets of vertices are returned.</returns>
        /// <remarks>If G has disconnected vertices, Red.Count() + Blue.Count() will be less than G.Order as the vertices will not be visited and may be included in either disjoint set</remarks>
        /// 
        public static bool IsBipartite(Network G, out List<uint> Red, out List<uint> Blue)
        {
            uint start = G.StartingVertex();

            // check for disconnected vertices
            if ((G.Directed && G.OutDegree(start) == 0) || (!G.Directed && G.Degree(start) == 0))
                start = FindConnectedVertex(G);

            return IsBipartite(start, G, out Red, out Blue);

        }

        #region private methods
        private static bool IsBipartite(uint start, Network G, out List<uint> Red, out List<uint> Blue)
        {
            Red = new List<uint>();
            Blue = new List<uint>();

            Queue<uint> tovisit = new Queue<uint>();
            Dictionary<uint, color> coloring = new Dictionary<uint, color>(G.Order);

            // BFS traversal of the network
            coloring[start] = color.red;
            Red.Add(start);
            tovisit.Enqueue(start);

            while (tovisit.Count() != 0)
            {
                uint parent = tovisit.Dequeue();
                color parentColor = coloring[parent];
                color tocolor = color.uncolored;
                if (parentColor == color.red)
                    tocolor = color.blue;
                else
                    tocolor = color.red;

                Dictionary<uint, float> neighbors = G.GetNeighbors(parent);

                // this is painful, but necessary to ensure reachability in directed graphs
                // Petri nets do not have this problem, but we are trying for generality here
                if (G.Directed)
                {
                    Dictionary<uint, float> predecessors = G.GetSources(parent);
                    foreach (KeyValuePair<uint, float> kvp in predecessors)
                    {
                        neighbors.Add(kvp.Key, kvp.Value);
                    }
                }

                // check for conflicts in coloring between parent and child
                // do the coloring otpimistically and compose the lists for the two disjoint sets
                foreach (uint vertex in neighbors.Keys)
                {
                    color childColor = new color();
                    if (!coloring.TryGetValue(vertex, out childColor))
                    {
                        // if not visited, set the color and enqueue
                        tovisit.Enqueue(vertex);
                        childColor = tocolor;
                        if (childColor == color.red)
                            Red.Add(vertex);
                        else
                            Blue.Add(vertex);
                        coloring[vertex] = childColor;
                    }
                    else
                    {
                        // check for conflict
                        if (childColor == parentColor)
                            return false;
                    }
                }

            }

            return true;
        }

        // Now we're down in the weeds
        // If a starting point is a disconnected vertex, traverse the list of vertices until a vertex with at least
        // one adjacent vertex is found.
        private static uint FindConnectedVertex(Network G)
        {
            List<uint> vertices = G.Vertices;

            uint retVal = 0;
            bool foundOne = false;

            foreach (uint vertex in vertices)
            {

                if ((G.Directed && G.OutDegree(vertex) > 0) || (!G.Directed && G.Degree(vertex) > 0))
                {
                    retVal = vertex;
                    foundOne = true;
                    break;
                }
            }

            if (foundOne)
                return retVal;
            else
                throw new BipartiteDisconnectedException();
        }

        #endregion
    }
}
