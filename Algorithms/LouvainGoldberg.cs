using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Algorithms
{
    public class LouvainGoldberg : LouvainQuality
    {
        public List<double> ins; // used to compute the quality participation of each community
        public List<int> w; // used to store size of communities

        public double max; // biggest weight on links

        public LouvainGoldberg(LouvainGraph gr, double max_w) : base(gr)
        {
            max = max_w;
            n2c = new List<int>();
            ins = new List<double>();
            w = new List<int>();

            // initialization
            for (int i = 0; i < size; i++)
            {
                n2c.Add(i);
                ins.Add(g.nb_selfloops(i));
                w.Add(g.nodes_w[i]);
            }
        }

        #region overrides

        public override double quality()
        {
            double q = 0.0;
            double n = (double)g.sum_nodes_w;

            for (int i = 0; i < size; i++)
            {
                double wc = (double)w[i] * 2.0;
                if (wc > 0.0)
                    q += ins[i] / wc;
            }

            q /= n* max;
  
            return q;
        }

        public override void remove(int node, int comm, double dnodecomm)
        {
            if (node < 0 || node >= size)
                throw new ArgumentException($"Parametrer node must be in the range [0..size), was {node} in LouvainGoldberg.remove");

            ins[comm] -= 2.0 * dnodecomm + g.nb_selfloops(node);
            w[comm]  -= g.nodes_w[node];

            n2c[node] = -1;
        }

        public override void insert(int node, int comm, double dnodecomm)
        {
            if (node < 0 || node >= size)
                throw new ArgumentException($"Parametrer node must be in the range [0..size), was {node} in LouvainGoldberg.insert");

            ins[comm] += 2.0 * dnodecomm + g.nb_selfloops(node);
            w[comm]  += g.nodes_w[node];

            n2c[node] = comm;
        }

        public override double gain(int node, int comm, double dnc, double degc)
        {
            if (node < 0 || node >= size)
                throw new ArgumentException($"Parametrer node must be in the range [0..size), was {node} in LouvainGoldberg.remove");

            double inc = ins[comm];
            double self = g.nb_selfloops(node);
            double wc = (double)w[comm];
            double wu = (double)g.nodes_w[node];
  
            double gain;

            if (wc == 0.0)
                gain  = (2.0 * dnc+self) / (2.0 * wu);
            else
            {
                gain  = (2.0 * dnc + self + inc) / (2.0 * (wc+wu));
                gain -= inc / (2.0 * wc);
            }

          return gain;
        }

        #endregion
    }
}
