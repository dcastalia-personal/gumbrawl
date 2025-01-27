using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class ScoringAuth : MonoBehaviour {
	public float levelArea;
	public float fractionToComplete;
	public GameObject playerScorePrefab;

	public class Baker : Baker<ScoringAuth> {

		public override void Bake( ScoringAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Scoring {
				levelArea = auth.levelArea,
				fractionToComplete = auth.fractionToComplete,
				playerScorePrefab = new UnityObjectRef<GameObject> { Value = auth.playerScorePrefab },
			} );

			AddComponent( self, new LevelFinished {} ); SetComponentEnabled<LevelFinished>( self, false );
			AddBuffer<ScorePopupReq>( self );
			AddComponent( self, new ScorePopupsPending {} ); SetComponentEnabled<ScorePopupsPending>( self, false );
		}
	}
}

public struct Scoring : IComponentData {
	public float fractionToComplete;
	public float levelArea;
	[GhostField] public float levelComplete;
	
	public UnityObjectRef<GameObject> playerScorePrefab;
}

[InternalBufferCapacity( 0 )] [GhostComponent]
public struct ScoreElement : IBufferElementData {
	[GhostField] public uint value;
}

[GhostComponent]
public struct Score : IComponentData {
	[GhostField] public short value;
}

[GhostComponent] [GhostEnabledBit] public struct LevelFinished : IComponentData, IEnableableComponent {}

public struct ScorePopupsPending : IComponentData, IEnableableComponent {}

[InternalBufferCapacity( 10 )]
public struct ScorePopupReq : IBufferElementData {
	public float2 position;
	public short amount;
	public short team;
}