using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Unity.Entities.SystemAPI;
using Object = UnityEngine.Object;

[UpdateInGroup(typeof(InitAfterSceneSysGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitCharAppearanceSys : ISystem {
	static readonly int baseColorID = Shader.PropertyToID( "_BaseColor" );
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, SkinColor, Appearance, TeamIndexLocal, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, skinColor, teamIndex, transform) in Query<RefRW<Appearance>, RefRO<SkinColor>, RefRO<TeamIndexLocal>, RefRO<LocalTransform>>().WithAll<Character, RequireInit>() ) {
			var instance = Object.Instantiate( appearance.ValueRO.prefab ) as GameObject;
			appearance.ValueRW.animator = instance!.GetComponent<Animator>();
			instance.transform.position = transform.ValueRO.Position + appearance.ValueRO.offset;
			instance.transform.forward = -Vector3.forward;
			
			// decals
			var projector = instance.GetComponentInChildren<DecalProjector>();
			appearance.ValueRW.decalProjector = new UnityObjectRef<DecalProjector> { Value = projector };
			appearance.ValueRW.decalProjDepthStart = projector.transform.localPosition.z;

			var teams = GetSingletonBuffer<Team>();
			var teamColor = teams[ teamIndex.ValueRO.value ].color;
			appearance.ValueRO.decalProjector.Value.material.SetColor( baseColorID, (Vector4)teamColor );

			var meshRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
			const int skinMatIndex = 0;
			const int lipMatIndex = 1;
			var lipsMat = meshRenderer.materials[ lipMatIndex ];
			var skinMat = meshRenderer.materials[ skinMatIndex ];

			lipsMat.SetColor( baseColorID, (Vector4)teamColor );

			var skinColor4 = new Color( skinColor.ValueRO.value.x, skinColor.ValueRO.value.y, skinColor.ValueRO.value.z, 1f );
			skinMat.SetColor( baseColorID, skinColor4 );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SyncAppearanceWithCharTransformSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance, LocalTransform>().WithNone<RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, transform) in Query<RefRO<Appearance>, RefRO<LocalTransform>>().WithAll<Character>().WithNone<RequireInit>() ) {
			var appearanceTransform = appearance.ValueRO.animator.Value.transform;
			appearanceTransform.position = transform.ValueRO.Position + appearance.ValueRO.offset;
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CharEatAppearanceSys : ISystem {
	EntityQuery query;
	static readonly int animHash = Animator.StringToHash( "Eating" );

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance>().WithPresent<Eating>().WithNone<RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, animEnabled) in Query<RefRO<Appearance>, EnabledRefRO<Eating>>().WithNone<RequireInit>().WithChangeFilter<Eating>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			appearance.ValueRO.animator.Value.SetBool( animHash, animEnabled.ValueRO );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CharChewAppearanceSys : ISystem {
	EntityQuery query;
	static readonly int animHash = Animator.StringToHash( "Chewing" );

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance>().WithPresent<Chewing>().WithNone<RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, animEnabled) in Query<RefRO<Appearance>, EnabledRefRO<Chewing>>().WithNone<RequireInit>().WithChangeFilter<Chewing>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			appearance.ValueRO.animator.Value.SetBool( animHash, animEnabled.ValueRO );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CharFullAppearanceSys : ISystem {
	EntityQuery query;
	static readonly int animHash = Animator.StringToHash( "Full" );

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance>().WithPresent<Full>().WithNone<RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, animEnabled) in Query<RefRO<Appearance>, EnabledRefRO<Full>>().WithNone<RequireInit>().WithChangeFilter<Full>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			appearance.ValueRO.animator.Value.SetBool( animHash, animEnabled.ValueRO );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CharBlowAppearanceSys : ISystem {
	EntityQuery query;
	static readonly int animHash = Animator.StringToHash( "Blowing" );

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance>().WithPresent<Blowing>().WithNone<RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, animEnabled) in Query<RefRO<Appearance>, EnabledRefRO<Blowing>>().WithNone<RequireInit>().WithChangeFilter<Blowing>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			appearance.ValueRO.animator.Value.SetBool( animHash, animEnabled.ValueRO );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CharStunAppearanceSys : ISystem {
	EntityQuery query;
	static readonly int animState = Animator.StringToHash( "Surprised" );

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance, BaseColor>().WithPresent<Stunned>().WithNone<RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, stunnedState) in Query<RefRO<Appearance>, EnabledRefRO<Stunned>>().WithNone<RequireInit>().WithChangeFilter<Stunned>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			if( appearance.ValueRO.decalProjector.Value ) appearance.ValueRO.decalProjector.Value.enabled = stunnedState.ValueRO;
			if( appearance.ValueRO.animator.Value ) appearance.ValueRO.animator.Value.SetBool( animState, stunnedState.ValueRO );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SplatWhileStunnedSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Appearance, Stunned>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (appearance, stunnedTime) in Query<RefRO<Appearance>, RefRO<StunnedTime>>().WithNone<RequireInit>().WithAll<Stunned>() ) {
			var projector = appearance.ValueRO.decalProjector.Value;
			var projectPos = projector.transform.localPosition;
			var projectorStartPos = new float3( projectPos.x, projectPos.y, appearance.ValueRO.decalProjDepthStart );
			var projectorEndPos = projectorStartPos + new float3( 0f, 0f, 1f ) * appearance.ValueRO.decalProjRange;

			projector.transform.localPosition = math.lerp( projectorStartPos, projectorEndPos, 1f - stunnedTime.ValueRO.time / stunnedTime.ValueRO.maxTime );
			
		}
	}
}