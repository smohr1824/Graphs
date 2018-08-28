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

        private struct coloring
        {
            string vertex;
            color color;

        }
        public static bool IsBipartite(Network G, out List<string> Red, out List<string> Blue)
        {
            Red = new List<string>();
            Blue = new List<string>();

            //if (!G.Connected)
            //    return false;
            Queue<string> tovisit = new Queue<string>();
            Dictionary<string, color> coloring = new Dictionary<string, color>(G.Order);

            string start = G.StartingVertex();
            coloring[start] = color.red;
            Red.Add(start);
            tovisit.Enqueue(start);

            while (tovisit.Count() != 0)
            {
                string parent = tovisit.Dequeue();
                color parentColor = coloring[parent];
                color tocolor = color.uncolored;
                if (parentColor == color.red)
                    tocolor = color.blue;
                else
                    tocolor = color.red;

                Dictionary<string, float> neighbors = G.GetNeighbors(parent);
                foreach (string vertex in neighbors.Keys)
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
    }
}
