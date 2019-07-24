multilayer_network [
	directed 1
	aspects [
		process electrical,flow,control
		site PHL,SLTC
	]
	layer [
		coordinates electrical,PHL
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
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates flow,PHL
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 4
			]
			node [
				id 5
			]
			edge [
				source 1
				target 4
				weight 1.0000
			]
			edge [
				source 1
				target 5
				weight 1.0000
			]
		]
	]
	layer [
		coordinates control,PHL
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
			node [
				id 4
			]
			node [
				id 5
			]
			node [
				id 19
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
			edge [
				source 2
				target 4
				weight 1.0000
			]
			edge [
				source 3
				target 5
				weight 1.0000
			]
		]
	]
	layer [
		coordinates electrical,SLTC
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
			edge [
				source 2
				target 3
				weight 1.0000
			]
		]
	]
	layer [
		coordinates flow,SLTC
		graph [
			directed 1
			node [
				id 1
			]
			node [
				id 2
			]
			node [
				id 4
			]
			node [
				id 5
			]
			node [
				id 26
			]
			edge [
				source 1
				target 4
				weight 1.0000
			]
			edge [
				source 1
				target 5
				weight 1.0000
			]
			edge [
				source 4
				target 5
				weight 2.0000
			]
			edge [
				source 26
				target 2
				weight 2.0000
			]
		]
	]
	layer [
		coordinates control,SLTC
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
			node [
				id 4
			]
			node [
				id 5
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
			edge [
				source 2
				target 4
				weight 1.0000
			]
			edge [
				source 3
				target 5
				weight 1.0000
			]
		]
	]
	edge [
		source [
			id 3
			coordinates control,PHL
		]
		target [
			id 1
			coordinates control,SLTC
		]
		weight 4
	]
	edge [
		source [
			id 1
			coordinates electrical,SLTC
		]
		target [
			id 2
			coordinates control,SLTC
		]
		weight 2
	]
	edge [
		source [
			id 2
			coordinates electrical,SLTC
		]
		target [
			id 3
			coordinates control,SLTC
		]
		weight 2
	]
]
