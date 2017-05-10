// Copyright 2017 -- Stephen T. Mohr, OSIsoft, LLC
// Licensed under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{

    /// <summary>
    ///  Used with LINQ Distinct to compare two sets and determine if they have the same members
    /// </summary>
    public class SetEqualityComparer : IEqualityComparer<HashSet<string>>
    {
        bool  IEqualityComparer<HashSet<string>>.Equals(HashSet<string> x, HashSet<string> y)
        {
            if (x.Count() == y.Count())
            {
                foreach (string item in x)
                {
                    if (!y.Contains(item))
                        return false;
                }
                return true;
            }
            else
                return false;
        }

        int IEqualityComparer<HashSet<string>>.GetHashCode(HashSet<string> set)
        {
            int code = 0;
            foreach (string member in set)
                code += member.GetHashCode();
            return code;
        }
    }
}
