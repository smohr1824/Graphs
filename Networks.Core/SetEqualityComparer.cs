﻿// MIT License

// Copyright(c) 2017 - 2019 Stephen Mohr

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

namespace Networks.Core
{

    /// <summary>
    ///  Used with LINQ Distinct to compare two sets and determine if they have the same members
    /// </summary>
    public class SetEqualityComparer : IEqualityComparer<HashSet<uint>>
    {
        bool  IEqualityComparer<HashSet<uint>>.Equals(HashSet<uint> x, HashSet<uint> y)
        {
            if (x.Count() == y.Count())
            {
                foreach (uint item in x)
                {
                    if (!y.Contains(item))
                        return false;
                }
                return true;
            }
            else
                return false;
        }

        int IEqualityComparer<HashSet<uint>>.GetHashCode(HashSet<uint> set)
        {
            int code = 0;
            foreach (uint member in set)
                code += member.GetHashCode();
            return code;
        }
    }
}
