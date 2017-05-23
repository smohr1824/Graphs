Overview
==========
Networks
==========
Adding an edge when one or both vertices does not exist results in the creation of the needed vertices.
MultilayerNetworks
==================
This implementation of generalized multilayer networks is intended for uses in which elementary layers may be developed and used independently,
but layers and aspects may be desired to add semantic richness to the model.  Aspects are fixed at the time of network creation. Aspects are defined by string labels, 
and layer sets within an aspect are similarly addressed by a string value.  Tensor notation is supported 
for brevity and convenience.  Vertices in the multilayer network are addressed in the form <node id>:a1,a2,..,ad where a1..ad are the aspect coordinates for the d aspects.

Once a network is added to the multilayer network as an elementary layer, further addition or removal of nodes and edges is through the multilayer network. If an
elementary layer is returned, it is returned as a copy.  Changes made to the copy are not reflected in the multilayer network.  This supports applications which wish
to work with an elementary layer (e.g., looking at a particular process represented as a graph) but retain access to the additional semantics of a multilayer network.

Addition of an edge in which one or both vertices does not exist results in the creation of the needed vertex or vertices in the designated elementary layer.  Attempting to
add an edge in which one or both vertices specifies an elementary layer which does not exist, however, results in an Argument exception, i.e., elementary layers are only created through
the addition of an existing Network.