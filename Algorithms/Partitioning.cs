using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace Algorithms
{
    public class Partitioning
    {

        public static void ExpandSeed(ref HashSet<string> seed, Network G, double lambda)
        {
            HashSet<string> fringe = new HashSet<string>();
            Dictionary<string, Tuple<int, int>> members = new Dictionary<string, Tuple<int, int>>();
            Dictionary<string, Tuple<int, int>> neighbors = new Dictionary<string, Tuple<int, int>>();
            double seedWeightIn = 0;
            double seedWeightOut = 0;

            foreach(string node in seed)
            {  //Tally members of the seed, calculating individual
               // Win and Wout measures and noting neighborhood
                Dictionary<string, int> N = G.GetNeighbors(node);
                int Win = 0, Wout = 0;
                foreach (KeyValuePair<string, int> vertex in N)
                {
                    if (seed.Contains(vertex.Key))
                    {     //If the neighbor is also in the seed, increase weight_in
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

                members.Add(node, new Tuple<int, int>(Win, Wout));

            }

            seedWeightIn /= 2;  //Internal edges were counted twice (assumed undirected)

            foreach (string node in fringe)
            { //Tally same information for neighborhood
                Dictionary<string, int> N = G.GetNeighbors(node);
                int Win = 0, Wout = 0;
                foreach(KeyValuePair<string, int> vertex in N)
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

                neighbors.Add(node, new Tuple<int, int>(Win, Wout));

            }

            bool changed = true;

            //While the seed is changing, add new members and remove poor members
            while (changed)
            {
                changed = false;
                List<string> to_check = new List<string>();
                List<HashSet<string>> order_by_degree = new List<HashSet<string>>();

                foreach (KeyValuePair<string, Tuple<int, int>> neighbor in neighbors)
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
                { // Go through all the neighbors
                    Tuple<int, int> neighborWts = neighbors[to_check[i]];

                    if (CalcDensity(seed.Count(), seedWeightIn, seedWeightOut, lambda) < CalcDensity(seed.Count() + 1, seedWeightIn + neighborWts.Item1, seedWeightOut + neighborWts.Item2 - neighborWts.Item1, lambda))
                    {
                        //If the density would increase by including the vertex - do it
                        changed = true; //Mark the change in seed
                        seedWeightIn += neighborWts.Item1;
                        seedWeightOut = seedWeightOut - neighborWts.Item1 + neighborWts.Item2;
                        seed.Add(to_check[i]);  //Update seed
                        members.Add(to_check[i], neighborWts);
                        neighbors.Remove(to_check[i]); //Update local trackers

                        //UPDATE MEMBER AND NEIGHBOR LISTS
                        // The Win and Wout values of vertices connected to the added vertex have changed...
                        Dictionary<string, int> N = G.GetNeighbors(to_check[i]);

                        foreach (KeyValuePair<string, int> neighbor in N)
                        {
                            Tuple<int, int> weights;
                            if (members.TryGetValue(neighbor.Key, out weights))
                            { //Update member
                                Tuple<int, int> updatedWeights = new Tuple<int, int>(weights.Item1 + neighbor.Value, weights.Item2 - neighbor.Value);
                                members[neighbor.Key] = updatedWeights;
                            }
                            else if (neighbors.TryGetValue(neighbor.Key, out weights))
                            { //Update current neighbor
                                Tuple<int, int> updatedWeights = new Tuple<int, int>(weights.Item1 + neighbor.Value, weights.Item2 - neighbor.Value);
                                neighbors[neighbor.Key] = updatedWeights;
                            }
                            else
                            { //Add new neighbor
                                Dictionary<string, int> N2 = G.GetNeighbors(neighbor.Key);

                                int newWin = 0, newWout = 0;
                                foreach (KeyValuePair<string, int> neighbor2 in N2)
                                {
                                    Tuple<int, int> wts;
                                    if (members.TryGetValue(neighbor2.Key, out wts))
                                        newWin += neighbor2.Value;
                                    else
                                        newWout += neighbor2.Value;
                                }

                                neighbors.Add(neighbor.Key, new Tuple<int, int>(newWin, newWout));
                            }
                        }
                    }
                }

                //REPEAT FOR MEMBERS (reversing mathematical signs where necessary, of course)
                to_check.Clear();
                order_by_degree.Clear();

                foreach (KeyValuePair<string, Tuple<int, int>> member in members)
                {
                    int deg = G.Degree(member.Key);
                    if (order_by_degree.Count() < deg + 1)
                    {
                        order_by_degree.Capacity = deg + 1;

                        for (int k = 0; k < deg + 1; k++)
                            order_by_degree.Add(new HashSet<string>());

                        //order_by_degree.AddRange(Enumerable.Repeat<HashSet<string>>(new HashSet<string>(), deg - order_by_degree.Count() + 1));
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
                    Tuple<int, int> weights;
                    //neighbors.TryGetValue(to_check[i], out weights);
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

                        //UPDATE MEMBER AND NEIGHBOR LISTS
                        Dictionary<string, int> N = G.GetNeighbors(to_check[i]);

                        foreach(KeyValuePair<string, int> neighbor in N)
                        //for (it_n = N.begin(); it_n != N.end(); it_n++)
                        {
                            Tuple<int, int> d2Wts;
                            if (members.TryGetValue(neighbor.Key, out d2Wts))
                            { //Update member
                                members[neighbor.Key] = new Tuple<int, int>(d2Wts.Item1 - neighbor.Value, d2Wts.Item2 + neighbor.Value);
                            }
                            else if (neighbors.TryGetValue(neighbor.Key, out d2Wts))
                            { //Update current neighbor
                                neighbors[neighbor.Key] = new Tuple<int, int>(d2Wts.Item1 - neighbor.Value, d2Wts.Item2 + neighbor.Value);
                            } //No new neighbors can be added to consider when removing members
                        }
                    }
                }

                //Get best component to move forward with
                SortedDictionary<double, HashSet<string>> comps = Components(seed, G, lambda);
                seed = comps.First().Value;

                //Print ( seed );
            }

        }
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
            
            // running total of edge weights in the component and outside the component
            int wIn = 0, wOut = 0;

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

                    Dictionary<string, int> Neighborhood = G.GetNeighbors(first);
                    toCheck.Remove(first);

                    // expand by the neighbors of the node to check; must not have been previously seen and must be part of the seed cluster
                    foreach(KeyValuePair<string, int> edge in Neighborhood)
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

            double partA = ((1 - lambda) * (win / (win + wout)));

            double partB = (lambda * ((2 * win) / (size * (size - 1))));
            if (size == 1) partB = lambda;

            return partA + partB;
        }


    }
}
