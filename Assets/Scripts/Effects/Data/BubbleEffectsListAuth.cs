using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BubbleEffectsListAuth : MonoBehaviour {
	public List<GameObject> effectPrefabs = new();

	public class Baker : Baker<BubbleEffectsListAuth> {

		public override void Bake( BubbleEffectsListAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			var effects = AddBuffer<BubbleEffectRefElement>( self );

			foreach( var prefab in auth.effectPrefabs ) {
				effects.Add( new BubbleEffectRefElement { value = GetEntity( prefab, TransformUsageFlags.None ) } );
			}
		}
	}
}

public struct BubbleEffectRefElement : IBufferElementData {
	public Entity value;
}