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

/// This is the base class for quality metrics used with the Louvain algorithm.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Algorithms
{
    public enum LouvainMetric
    {
        Modularity = 1,
        Goldberg = 2,
        Resolution = 3
    }
    public abstract class LouvainQuality
    {
        public LouvainGraph g;
        public int size;
        public List<int> n2c;   // a list of community memberships such that the nth value is the id of the community to which the nth node belongs

        public LouvainQuality(LouvainGraph gr)
        {
            g = gr;
            size = g.nb_nodes;
        }
        // virtual methods

        // remove the node from its current community with which it has dnodecomm links
        public abstract void remove(int node, int comm, double dnodecomm);
  
        // insert the node in comm with which it shares dnodecomm links
        public abstract void insert(int node, int comm, double dnodecomm);
  
        // compute the gain of quality by adding node to comm
        public abstract double gain(int node, int comm, double dnodecomm, double w_degree);
  
        // compute the quality of the current partition
        public abstract double quality();
    }
}
