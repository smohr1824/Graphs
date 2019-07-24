graph [
	directed 1
	node [
		id 0
		label A
		activation 1.0000
		initial 1.0000
	]
	node [
		id 1
		label B
		activation 0.0000
		initial 0.0000
	]
	node [
		id 2
		label C
		activation 1.0000
		initial 1.0000
	]
	node [
		id 3
		label D
		activation 0.0000
		initial 0.0000
	]
	node [
		id 4
		label E
		activation 0.0000
		initial 0.0000
	]
	edge [
		source 0
		target 2
		weight 1.0000
	]
	edge [
		source 1
		target 0
		weight 1.0000
	]
	edge [
		source 1
		target 4
		weight -1.0000
	]
	edge [
		source 2
		target 4
		weight 1.0000
	]
	edge [
		source 3
		target 2
		weight -1.0000
	]
	edge [
		source 3
		target 1
		weight 1.0000
	]
	edge [
		source 4
		target 3
		weight 1.0000
	]
	edge [
		source 4
		target 0
		weight -1.0000
	]
]
