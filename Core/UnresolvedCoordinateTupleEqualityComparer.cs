using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class UnresolvedCoordinateTupleEqualityComparer : IEqualityComparer<List<string>>
    {
        bool IEqualityComparer<List<string>>.Equals(List<string> a, List<string> b)
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

        int IEqualityComparer<List<string>>.GetHashCode(List<string> tuple)
        {
            string field = string.Empty;
            foreach (string coord in tuple)
                field += coord.ToString();
            return field.GetHashCode();
        }
    }
}
