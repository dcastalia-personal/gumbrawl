using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnPrefabAuth : MonoBehaviour {
	public float frequency;
	public float initialSpawnTime;
	public float rateIncreasePerSecond;
	public Vector2 speedRange;

	public class Baker : Baker<SpawnPrefabAuth> {

		public override void Bake( SpawnPrefabAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new SpawnPrefab {
				frequency = auth.frequency,
				speedRange = auth.speedRange,
				timer = auth.initialSpawnTime,
				rateIncreasePerSecond = auth.rateIncreasePerSecond,
			} );
		}
	}
}

public struct SpawnPrefab : IComponentData {
	public float frequency;
	public float timer;
	public float2 speedRange;
	public float rateIncreasePerSecond;
}