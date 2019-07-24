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

namespace Networks.Core
{
    public class NodeLayerTupleEqualityComparer : IEqualityComparer<NodeLayerTuple>
    {
        bool IEqualityComparer<NodeLayerTuple>.Equals(NodeLayerTuple a, NodeLayerTuple b)
        {
            if (a.nodeId == b.nodeId)
            {
                if (a.coordinates.Count() == b.coordinates.Count())
                {
                    for (int i = 0; i < a.coordinates.Count(); i++)
                    {
                        if (a.coordinates[i] != b.coordinates[i])
                            return false;
                    }
                    return true;
                }
                else return false;
            }
            else
                return false;
        }

        int IEqualityComparer<NodeLayerTuple>.GetHashCode(NodeLayerTuple tuple)
        {
            int code = tuple.nodeId.GetHashCode();
            foreach (string coord in tuple.coordinates)
                code += coord.GetHashCode();
            return code;
        }
    }
}
