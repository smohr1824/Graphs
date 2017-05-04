using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class Network
    {
        // each key in the edge list is the id of a from vertex.  The value is an adjacency list composed of a dictionary of to vertex ids and the edge weight
        private Dictionary<string, Dictionary<string, int>> EdgeList;

        public Network()
        {
            EdgeList = new Dictionary<string, Dictionary<string, int>>();
        }

        #region public methods

        public List<string> Vertices
        {
             get { return EdgeList.Keys.ToList(); }
        }
        public void AddEdge(string from, string to, int weight, bool directed)
        {
            Dictionary<string, int> neighbors;
            if (!EdgeList.TryGetValue(from, out neighbors))
            {
                neighbors = new Dictionary<string, int>(); 
                neighbors.Add(to, weight);
                EdgeList.Add(from, neighbors);
            }
            else
            {
                // found from, check for to
                if (!neighbors.ContainsKey(to))
                {
                    neighbors.Add(to, weight);
                }
                else
                {
                    neighbors[to] = weight;
                }
            }

            // if this is an undirected edge, add the reciprocal edge as well
            if (!directed)
            {
                if (!EdgeList.TryGetValue(to, out neighbors))
                {
                    neighbors = new Dictionary<string, int>(); 
                    neighbors.Add(from, weight);
                    EdgeList.Add(to, neighbors);
                }
                else
                {
                    if (!neighbors.ContainsKey(from))
                    {
                        neighbors.Add(from, weight);
                    }
                    else
                    {
                        neighbors[from] = weight;
                    }
                }
            }
        }

        public void RemoveEdge(string from, string to, bool directed)
        {
            Dictionary<string, int> neighbors = new Dictionary<string, int>();
            if (HasEdge(from, to))
            {
                EdgeList.TryGetValue(from, out neighbors);
                neighbors.Remove(to);

                if (!directed)
                {
                    if (EdgeList.TryGetValue(to, out neighbors))
                        neighbors.Remove(from);
                }
            }
        }

        public Dictionary<string, int> GetNeighbors(string node)
        {
            Dictionary<string, int> nhood = null;
            if (!EdgeList.TryGetValue(node, out nhood))
            {
                return new Dictionary<string, int>();
            }
            else
            {
                return nhood;
            }
        }

        public bool HasEdge(string from, string to)
        {
            return false;
        }

        public int Degree(string node)
        {
            Dictionary<string, int> neighbors = null;
            if (!EdgeList.TryGetValue(node, out neighbors))
                return 0;
            else
                return neighbors.Count();

        }

        public double EdgeWeight(string from, string to)
        {
            Dictionary<string, int> neighbors = null;
            if (HasEdge(from, to))
            {
                EdgeList.TryGetValue(from, out neighbors);
                return neighbors[to];
            }
            else
                return 0;
        }

        public void List(TextWriter writer, char delimiter)
        {
            foreach (string key in EdgeList.Keys)
            {
                Dictionary<string, int> targets = EdgeList[key];
                foreach (string to in targets.Keys)
                {
                    writer.Write(key + delimiter + to + delimiter + targets[to].ToString());
                }
            }
        }
        #endregion
    }
}
