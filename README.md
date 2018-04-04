# Shepherding With Markov Brains
Teach a dog how to shepherd using Markov Brains.

[A technical introduction to Markov Brains](https://arxiv.org/pdf/1709.05601.pdf)

## Variables

### Shepherd
**SightType**
* Normal: Return a set of 0s and 1s determining whether a sheep is or isn't located in each of the sight segment
* Distance: Return a set of floats determening the distance of the closest sheep in each of the sight segments
* Furthest only: Return a set of 0s and one float determining the segment in which the sheep located furthest from the centroid is and how far away is it

**Logic Type**
* Probabilistic: Output of the logic gates used within the Markov Brain will be based on probability. 
* Set: Each input will exactly determine only one output for each of the logic gates.

**Movement Type**
* Discrete: The shepherd will move in four directions only. Forward, Backward, Left and Right.
* Analog: The shepherd will move forward or backwards. Or turn a few degrees left or right.

### Simulation
**Simulating Animation**: Animate the currently running simulation (can be toggled even during the learning process)

**Run Best Dog**: Run best dog in the previous learning session

**Sheep Spawn**
* Static: The starting position is the same for each iteration
* Random: The starting position of the sheep is randomized each iteration

**Sheep Grazing**: Whether or not the sheep will move without the presence of the shepherd nearby
