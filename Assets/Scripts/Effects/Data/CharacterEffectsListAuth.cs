using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CharacterEffectsListAuth : MonoBehaviour {
	public List<GameObject> effectPrefabs = new();

	public class Baker : Baker<CharacterEffectsListAuth> {

		public override void Bake( CharacterEffectsListAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			var effects = AddBuffer<CharacterEffectRefElement>( self );

			foreach( var prefab in auth.effectPrefabs ) {
				effects.Add( new CharacterEffectRefElement { value = GetEntity( prefab, TransformUsageFlags.None ) } );
			}
		}
	}
}

public struct CharacterEffectRefElement : IBufferElementData {
	public Entity value;
}