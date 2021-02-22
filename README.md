# Unity3D-Simple-Water-Buoyancy-Script
A simple script that you should attach to a plane with a box trigger collider to have stuff float in the trigger. Simulates water float/ water buoyancy simply - this is not physically accurate.

Variables explained:

force -> how much extra upwards force to add to the object when it's in the trigger. A value of 0 Should cause the object to just float in place.
waterDrag -> how much drag to apply to an object when it's in the trigger. This is also affected by how "deep" in the trigger an object is.
max_weight -> the max weight this body of water can keep up before the weight is stronger than the upwards force.
mass_force_mult -> helps the `force` variable. Kind of redundant but it helped me fine tune the look and feel for my game.
down_stream_float_strength -> lets the water carry the object in the direction this variable specifies.
