multilayer_network [
	directed 1
	aspects [
		Roman I,II,III
		Latin A,B
		Numeric 1,2
	]
	layer [
		coordinates I,A,1
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 2
				target 1
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates I,A,2
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates I,B,1
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
			edge [
				source 3
				target 1
				weight 1.0000
			]
		]
	]
	layer [
		coordinates I,B,2
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 2
				target 1
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates II,A,1
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 3
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates II,A,2
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 2
				target 1
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates II,B,1
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 1
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates II,B,2
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 2
				target 1
				weight 1.0000
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 3
				target 1
				weight 1.0000
			]
		]
	]
	layer [
		coordinates III,A,1
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
			edge [
				source 3
				target 1
				weight 1.0000
			]
		]
	]
	layer [
		coordinates III,A,2
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 3
				target 2
				weight 1.0000
			]
			edge [
				source 2
				target 1
				weight 1.0000
			]
			edge [
				source 1
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates III,B,1
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 2
				target 1
				weight 1.0000
			]
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates III,B,2
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 3
			]
			edge [
				source 1
				target 3
				weight 1.0000
			]
			edge [
				source 1
				target 2
				weight 1.0000
			]
			edge [
				source 3
				target 2
				weight 1.0000
			]
		]
	]
	edge [
		source [
			id 1
			coordinates I,A,1
		]
		target [
			id 2
			coordinates I,B,1
		]
		weight 1
	]
	edge [
		source [
			id 2
			coordinates I,A,1
		]
		target [
			id 3
			coordinates I,B,2
		]
		weight 1
	]
	edge [
		source [
			id 3
			coordinates II,A,1
		]
		target [
			id 1
			coordinates II,A,2
		]
		weight 1
	]
	edge [
		source [
			id 3
			coordinates II,B,1
		]
		target [
			id 1
			coordinates I,A,2
		]
		weight 1
	]
	edge [
		source [
			id 3
			coordinates III,B,2
		]
		target [
			id 1
			coordinates I,A,1
		]
		weight 1
	]
]
