using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class CoordinateTensorEqualityComparer : IEqualityComparer<List<int>>
    {
        bool IEqualityComparer<List<int>>.Equals(List<int> a, List<int> b)
        {
            if (a.Count() == b.Count())
            {
                for (int i = 0; i < a.Count(); i++)
                {
                    if (a[i] != b[i])
                        return false;
                }
                return true;
            }
            else
                return false;
        }

        int IEqualityComparer<List<int>>.GetHashCode(List<int> tensor)
        {
            string field = string.Empty;
            foreach (int coord in tensor)
                field += coord.ToString();
            return field.GetHashCode();
        }
    }

}
