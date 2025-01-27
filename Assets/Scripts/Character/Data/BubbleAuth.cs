using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class BubbleAuth : MonoBehaviour {

	public class Baker : Baker<BubbleAuth> {

		public override void Bake( BubbleAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Bubble {} );
			// AddBuffer<BallIndexRefElement>( self );
			AddComponent( self, new PlayerRef {} );
			AddComponent( self, new CharacterRef {} );
			AddComponent( self, new Blowing {} ); SetComponentEnabled<Blowing>( self, false );
			AddComponent( self, new SpawnTick {} );
			AddComponent( self, new Popped {} ); SetComponentEnabled<Popped>( self, false );
			AddComponent( self, new Speed {} );
			AddComponent( self, new TeamIndexLocal {} );
			AddComponent( self, new PredictDestroy {} ); SetComponentEnabled<PredictDestroy>( self, false );
			AddComponent( self, new DisableCollisionEvents {} ); SetComponentEnabled<DisableCollisionEvents>( self, false );
			// AddComponent( self, new CopyDataFromTarget {} ); SetComponentEnabled<CopyDataFromTarget>( self, false );
		}
	}
}

public struct Bubble : IComponentData {
	
}

public struct Speed : IComponentData {
	public float value;
}

public struct Popped : IComponentData, IEnableableComponent {}

[GhostComponent]
public struct CharacterRef : IComponentData {
	[GhostField] public Entity value;
}

[GhostComponent]
public struct SpawnTick : IComponentData {
	[GhostField] public NetworkTick value;
}