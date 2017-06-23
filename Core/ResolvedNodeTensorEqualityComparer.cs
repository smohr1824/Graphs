﻿// MIT License

// Copyright(c) 2017 Stephen Mohr and OSIsoft, LLC

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
    public class ResolvedNodeTensorEqualityComparer : IEqualityComparer<ResolvedNodeTensor>
    {
        bool IEqualityComparer<ResolvedNodeTensor>.Equals(ResolvedNodeTensor a, ResolvedNodeTensor b)
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

        int IEqualityComparer<ResolvedNodeTensor>.GetHashCode(ResolvedNodeTensor tensor)
        {
            int code = tensor.nodeId.GetHashCode();
            string field = string.Empty;
            foreach (int coord in tensor.coordinates)
                field += coord.ToString();
            code += field.GetHashCode();
            return code;
        }
    }
}