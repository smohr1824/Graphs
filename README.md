# Graphs
Research code for working with graphs

## Basic classes
Graphs are represented by the Network class.  Clusters are represented by HashSet<string>, where the string is a vertex id.  Graphs are loaded
via the static NetworkSerializer class, while the similar ClusterSerializer class exists for clusters. 

### Serialization Format
Each line of a graph represents an edge adjacency list.  The first string is the from vertex, followed by the delimiter character, followed by,
the to vertext, followed by the delimiter and the edge weight.  Edge weights are integers.  Graphs are assumed to be directed, unless the 
file is loaded with the directed parameter of LoadNetwork set to false.  In that case, an edge is added for the reciprocal direction.

# Community detection algorithms 
Presently, the Partitioning class implements the following community detection algorithms:
1. Connected Iterative Scan (CIS)
2. Speaker-Listener Propagation Algorithm (SLPA)

The Connected Iterative Scan algorithm is described in Baumes J., Goldberg M., Krishnamoorthy M., Magdon-Ismail M., Preston N. Finding Communities by Clustering a Graph into Overlapping Subgraphs. Proceedings of the IADIS International Conference on Applied Computing, :97-104, Feb-2005.
The algorithmic code itself is adapted from the standard C++ code provided at http://www.cs.rpi.edu/~magdon/LFDlabpublic.html/software/CIS/CIS.tar.gz.

SLPA is described in Xie, Jierui and Szymanski, Boleslaw, Towards Linear Time Overlapping Community Detection in Social Networks, Proceedings of the Pacific-Asiz Conference on Knowledge Discovery and Data Mining, :25-36, 2012.

Additional algorithm implementations are planned.
