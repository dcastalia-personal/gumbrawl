using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class CharacterAuth : MonoBehaviour {
	public GameObject blowingBubblePrefab;
	public GameObject finishedBubblePrefab;
	public GameObject bubbleSpawn;
	public GameObject mouth;
	public GameObject appearancePrefab;
	public Transform appearanceAnchor;
	public float mouthRadius;
	public float movementSpeed;
	public float acceleration;
	public float bubbleBlowingSpeed;
	public float stunTime;
	public float decalProjRange;
	public int roomInMouth;

	public PhysicsCategoryTags canEatFilter;

	public class Baker : Baker<CharacterAuth> {

		public override void Bake( CharacterAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Character {
				speed = auth.movementSpeed,
				acceleration = auth.acceleration,
			} );
			AddComponent( self, new CharacterInput {} );
			AddComponent( self, new PlayerRef {} );
			AddComponent( self, new Chewing {} ); SetComponentEnabled<Chewing>( self, false );
			AddComponent( self, new Eating {} ); SetComponentEnabled<Eating>( self, false );
			AddComponent( self, new Blowing {} ); SetComponentEnabled<Blowing>( self, false );
			AddComponent( self, new Full {} ); SetComponentEnabled<Full>( self, false );
			AddComponent( self, new Blow {
				blowingBubblePrefab = GetEntity( auth.blowingBubblePrefab, TransformUsageFlags.Dynamic ),
				finishedBubblePrefab = GetEntity( auth.finishedBubblePrefab, TransformUsageFlags.Dynamic ),
				speed = auth.bubbleBlowingSpeed,
				bubbleSpawnOffset = auth.bubbleSpawn.transform.localPosition,
			} );
			AddComponent( self, new Eat {
				mouthOffset = auth.mouth.transform.localPosition, 
				radius = auth.mouthRadius,
				canEatFilter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = auth.canEatFilter.Value },
				roomInMouth = auth.roomInMouth
			} );

			AddComponent( self, new Appearance {
				prefab = new UnityObjectRef<GameObject> { Value = auth.appearancePrefab },
				offset = auth.appearanceAnchor.localPosition,
				decalProjRange = auth.decalProjRange,
			} );

			AddComponent( self, new SkinColor {} );

			AddBuffer<BallIndexRefElement>( self );
			AddComponent( self, new StunnedTime { maxTime = auth.stunTime } );
			AddComponent( self, new Stunned {} ); SetComponentEnabled<Stunned>( self, false );
			AddComponent( self, new TeamIndexLocal {} );

			AddBuffer<BubbleEffectRefElement>( self );
			AddBuffer<EffectDisplayReqElement>( self );
			AddBuffer<RecentBallProxiesCreated>( self );
		}
	}
}

public struct Character : IComponentData {
	public float speed;
	public float acceleration;
}

[GhostComponent]
public struct SkinColor : IComponentData {
	[GhostField] public float3 value;
}

public struct Appearance : IComponentData {
	public float3 offset;
	public UnityObjectRef<GameObject> prefab;
	public UnityObjectRef<Animator> animator;
	public UnityObjectRef<DecalProjector> decalProjector;
	public float decalProjDepthStart;
	public float decalProjRange;
}

[GhostComponent]
public struct Eat : IComponentData {
	public float3 mouthOffset;
	public float radius;
	public CollisionFilter canEatFilter;
	[GhostField] public int roomInMouth;
}

[GhostComponent]
public struct Blow : IComponentData {
	public Entity blowingBubblePrefab;
	public Entity finishedBubblePrefab;
	public float3 bubbleSpawnOffset;
	[GhostField] public float speed;
	
	[GhostField] public Entity bubble;
}

public struct CharacterInput : IInputComponentData {
	public InputEvent eatPress;
	public InputEvent eatRelease;
	public InputEvent blowPress;
	public InputEvent blowRelease;
	public float2 movementDelta;
}

[GhostComponent]
public struct StunnedTime : IComponentData {
	[GhostField] public float maxTime;
	[GhostField] public float time;
}

// list of the last second's worth of ball proxies you've created
public struct RecentBallProxiesCreated : IBufferElementData {
	public int serverIndex;
	public NetworkTick tick;
}

[GhostComponent] [GhostEnabledBit] public struct Stunned : IComponentData, IEnableableComponent {}
[GhostComponent] [GhostEnabledBit] public struct Chewing : IComponentData, IEnableableComponent {}
[GhostComponent] [GhostEnabledBit] public struct Eating : IComponentData, IEnableableComponent {}
[GhostComponent] [GhostEnabledBit] public struct Blowing : IComponentData, IEnableableComponent {}
[GhostComponent] [GhostEnabledBit] public struct Full : IComponentData, IEnableableComponent {}