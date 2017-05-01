# Graphs
Research code for working with graphs

## Basic classes
Graphs are represented by the Network class.  Clusters are represented by HashSet<string>, where the string is a vertex id.  Graphs are loaded
via the static NetworkSerializer class, while the similar ClusterSerializer class exists for clusters. 

### Serialization Format
Each line of a graph represents an edge adjacency list.  The first string is the from vertex, followed by the delimiter character, followed by,
the to vertext, followed by the delimiter and the edge weight.  Edge weights are integers.  Graphs are assumed to be directed, unless the 
file is loaded with the directed parameter of LoadNetwork set to false.  In that case, an edge is added for the reciprocal direction.

# Partitioning algorithms 
Presently, the Partitioning class implements the Connected Iterative Scan algorithm as described in "Finding Communities by Clustering a Graph into Overlapping Subgraphs",
Baumes J., Goldberg M., Krishnamoorthy M., Magdon-Ismail M., Preston N. Finding Communities by Clustering a Graph into Overlapping Subgraphs. Proceedings of the IADIS International Conference on Applied Computing, :97-104, Feb-2005.
The algorithmic code itself is adapted from the standard C++ code provided at http://www.cs.rpi.edu/~magdon/LFDlabpublic.html/software/CIS/CIS.tar.gz.

Additional clustering algorithm implementations are planned.
