using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class RandomnessAuth : MonoBehaviour {

	public class Baker : Baker<RandomnessAuth> {

		public override void Bake( RandomnessAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Randomness {} );
		}
	}
}

public struct Randomness : IComponentData {
	public Random rng;
}