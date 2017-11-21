// Copyright 2017 -- Stephen T. Mohr, OSIsoft, LLC
// Licensed under the MIT license
//
// MathNet Numerics nuget used in SLPA copyright:
///Copyright(c) 2002-2015 Math.NET

///Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

/// Based on the paper "Laplacian Dynamics and Multiscale Modular Structure in Networks"
/// Copyright (C) 2009 R. Lambiotte1, J.-C. Delvenne, and M. Barahona (arXiv 0812:1770v3)
/// This implements a quality metric for the greedy resolution algorithm proposed in the above paper.
/// 
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

// This is the dynamic process refinement of the standard Modularity metric described in the above paper.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Algorithms
{
    class LouvainResolution : LouvainQuality
    {
        public List<double> ins;
        public List<double> tot;
        public double time;

        /// <summary>
        /// Constructs a quality function instance for the resolution metric
        /// </summary>
        /// <param name="gr">Louvain graph instance</param>
        /// <param name="timet">resolution; multiplicative constant where increasing values of timet yield larger partitions, hence fewer communities; range is [-1..1]. At timet = 1, this behaves like LouvainModularity.</param>
        public LouvainResolution(LouvainGraph gr, double timet) : base(gr)
        {
            n2c = new List<int>();
            ins = new List<double>();
            tot = new List<double>();
            time = timet;

            for (int i = 0; i < size; i++)
            {
                n2c.Add(i);
                ins.Add(g.nb_selfloops(i));
                tot.Add(g.weighted_degree(i));
            }
        }

        public override double quality()
        {
            double q = 1.0 - time;
            double m2 = (double)g.total_weight;

            for (int i = 0; i < size; i++)
            {
                if (tot[i] > 0)
                    q += time * (double)ins[i]/m2 - ((double) tot[i]/m2)*((double) tot[i]/m2);
            }

            return q;
        }

        public override void remove(int node, int comm, double dnodecomm)
        {
            if (node < 0 || node >= size)
                throw new ArgumentException($"Passed index {node} to modularity.remove");

            ins[comm] -= 2.0 * dnodecomm + g.nb_selfloops(node);
            tot[comm] -= g.weighted_degree(node);

            n2c[node] = -1;
        }

        public override void insert(int node, int comm, double dnodecomm)
        {
            if (node < 0 || node >= size)
                throw new ArgumentException($"Passed index {node} to modularity.insert");

            ins[comm] += 2.0 * dnodecomm + g.nb_selfloops(node);
            tot[comm] += g.weighted_degree(node);

            n2c[node] = comm;
        }

        public override double gain(int node, int comm, double dnc, double degc)
        {
            if (node < 0 || node >= size)
                throw new ArgumentException($"Passed index {node} to modularity.gain");

            double totc = tot[comm];
            double m2 = g.total_weight;

            return (dnc - totc * degc / m2);
        }
    }
}
