using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [UpdateAfter(typeof(InitLevelSingletonsOnClientSys))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitScoreUISys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Scoring, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {

		var score = GetSingleton<Scoring>();
		var teams = GetSingletonBuffer<Team>();
		foreach( var (teamIndex, playerUIRef, playerName) in Query<RefRO<TeamIndex>, RefRW<PlayerUIRef>, RefRO<SyncedName>>().WithAll<Player>() ) {
			var playerUI = ((GameObject)Object.Instantiate( score.playerScorePrefab )).GetComponent<PlayerScoreEntryUI>();
			var playerBackgroundAlpha = playerUI.background.color.a;
			var teamColor = (Vector4)teams[ teamIndex.ValueRO.value ].color;
			teamColor.w = playerBackgroundAlpha;
			playerUI.background.color = teamColor;
			playerUI.nameLabel.text = playerName.ValueRO.value.ToString();
			playerUIRef.ValueRW.value = playerUI;

			InGameUI.current.playerScoresLayout.Add( playerUI.rectTransform );
		}
	}
}

[UpdateInGroup(typeof(InitAfterSceneSysGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct IncrementLevelCompleteSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, LocalTransform, CountTowardsLevelCompletion, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var scoringEntity = GetSingletonEntity<Scoring>();
		var scoring = GetComponent<Scoring>( scoringEntity );
		foreach( var localTransform in Query<RefRO<LocalTransform>>().WithAll<Bubble, CountTowardsLevelCompletion>() ) {
			scoring.levelComplete += math.square(localTransform.ValueRO.Scale / 2f) * math.PI;
		}

		SetComponent( scoringEntity, scoring );
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct DecrementLevelCompleteSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, LocalTransform, CountTowardsLevelCompletion, Destroy>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var scoringEntity = GetSingletonEntity<Scoring>();
		var scoring = GetComponent<Scoring>( scoringEntity );
		foreach( var localTransform in Query<RefRO<LocalTransform>>().WithAll<Bubble, CountTowardsLevelCompletion, Destroy>() ) {
			scoring.levelComplete -= math.square(localTransform.ValueRO.Scale / 2f) * math.PI;
		}
		
		SetComponent( scoringEntity, scoring );
	}
}

public partial struct UpdateLevelCompleteDisplaySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Scoring>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var scoring in Query<RefRO<Scoring>>().WithChangeFilter<Scoring>() ) {
			InGameUI.current.levelCompleteLabel.text = $"{math.min((int)((scoring.ValueRO.levelComplete / (scoring.ValueRO.levelArea * scoring.ValueRO.fractionToComplete)) * 100f), 100).ToString()}%";
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct CheckLevelCompleteSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Scoring>().WithPresent<LevelFinished>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new CheckLevelCompleteJob {}.Schedule( query );
	}

	[BurstCompile] partial struct CheckLevelCompleteJob : IJobEntity {
		
		void Execute( in Scoring scoring, EnabledRefRW<LevelFinished> levelCompleteEnabled ) {
			if( scoring.levelComplete / scoring.levelArea > scoring.fractionToComplete ) {
				levelCompleteEnabled.ValueRW = true;
			}
		}
	}
}

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))] // We are updating before `PhysicsSimulationGroup` - this means that we will get the events of the previous frame
public partial struct IncrementScoreSys : ISystem {
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Scoring>();
		state.RequireForUpdate<ScoreElement>();
	}

	[BurstCompile]
	public void OnUpdate( ref SystemState state ) {
		state.Dependency = new IncrementScoreJob {
			physicsWorld = GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
			ballLookup = GetComponentLookup<Ball>( isReadOnly: true ),
			countsForScoringLookup = GetComponentLookup<CountTowardsLevelCompletion>( isReadOnly: true ),
			teamIndexLocalLookup = GetComponentLookup<TeamIndexLocal>( isReadOnly: true ),
			scores = GetSingletonBuffer<ScoreElement>(),
			worthPointsLookup = GetComponentLookup<WorthPoints>( isReadOnly: true ),
			popupReqs = GetSingletonBuffer<ScorePopupReq>(),
			scoreEntity = GetSingletonEntity<Scoring>(),
			scorePopupsPendingLookup = GetComponentLookup<ScorePopupsPending>(),
			playerRefLookup = GetComponentLookup<PlayerRef>( isReadOnly: true ),
			scoreLookup = GetComponentLookup<Score>(),
			networkTime = GetSingleton<NetworkTime>(),
			isClient = state.WorldUnmanaged.IsClient(),
		}.Schedule( GetSingleton<SimulationSingleton>(), state.Dependency );
	}
	
	[BurstCompile]
	public partial struct IncrementScoreJob : ICollisionEventsJob {
		[ReadOnly] public PhysicsWorld physicsWorld;
		[ReadOnly] public ComponentLookup<Ball> ballLookup;
		[ReadOnly] public ComponentLookup<CountTowardsLevelCompletion> countsForScoringLookup;
		[ReadOnly] public ComponentLookup<TeamIndexLocal> teamIndexLocalLookup;
		[ReadOnly] public ComponentLookup<WorthPoints> worthPointsLookup;
		[ReadOnly] public ComponentLookup<PlayerRef> playerRefLookup;
		public ComponentLookup<Score> scoreLookup;
		public Entity scoreEntity;
		public ComponentLookup<ScorePopupsPending> scorePopupsPendingLookup;
		public DynamicBuffer<ScorePopupReq> popupReqs;
		public DynamicBuffer<ScoreElement> scores;
		public NetworkTime networkTime;
		public bool isClient;
		
		public void Execute( CollisionEvent collisionEvent ) {
			Entity ballEntity = Entity.Null;
			if( ballLookup.HasComponent( collisionEvent.EntityA ) ) ballEntity = collisionEvent.EntityA;
			else if( ballLookup.HasComponent( collisionEvent.EntityB ) ) ballEntity = collisionEvent.EntityB;
			
			Entity scoringEntity = Entity.Null;
			if( countsForScoringLookup.HasComponent( collisionEvent.EntityA ) ) scoringEntity = collisionEvent.EntityA;
			else if( countsForScoringLookup.HasComponent( collisionEvent.EntityB ) ) scoringEntity = collisionEvent.EntityB;

			if( ballEntity == Entity.Null || scoringEntity == Entity.Null ) {
				return;
			}
			
			var teamIndex = teamIndexLocalLookup[ scoringEntity ].value;
			var teamScore = scores[ teamIndex ];
			var worthPoints = worthPointsLookup[ scoringEntity ].value;

			if( !isClient ) {
				teamScore.value += (uint)worthPoints;
				scores[ teamIndex ] = teamScore;

				var playerEntity = playerRefLookup[ scoringEntity ].value;
				var score = scoreLookup[ playerEntity ];
				score.value += worthPoints;
				scoreLookup[ playerEntity ] = score;
			}
			else {
				if( !networkTime.IsFirstTimeFullyPredictingTick ) return;
				popupReqs.Add( new ScorePopupReq { position = collisionEvent.CalculateDetails( ref physicsWorld ).AverageContactPointPosition.xy, amount = worthPoints, team = teamIndex } );
				scorePopupsPendingLookup.SetComponentEnabled( scoreEntity, true );
			}
		}
	}
}

public partial struct DisplayScorePopupSys : ISystem {
	const float popupZ = -1f;
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ScorePopupReq, ScorePopupsPending>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		var scoringEntity = GetSingletonEntity<Scoring>();
		var popupReqBuf = GetBuffer<ScorePopupReq>( scoringEntity );
		var teams = GetSingletonBuffer<Team>();

		foreach( var popupReq in popupReqBuf ) {
			// if( Pool.current == null ) Debug.Log( $"No pool!" );
			var popupInstance = Pool.current.Get<PlusOneUI>( InGameUI.current.plusOnePopupPrefab );
			popupInstance.label.text = $"+{popupReq.amount.ToString()}";
			popupInstance.label.color = (Vector4)teams[ popupReq.team ].color;
			popupInstance.transform.position = new Vector3( popupReq.position.x, popupReq.position.y, popupZ );
		}

		popupReqBuf.Clear();
		SetComponentEnabled<ScorePopupsPending>( scoringEntity, false );
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct UpdatePlayerScoreDisplaySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Scoring>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player, Score, PlayerUIRef>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (score, uiRef) in Query<RefRO<Score>, RefRO<PlayerUIRef>>().WithAll<Player>().WithChangeFilter<Score>() ) {
			var ui = uiRef.ValueRO.value.Value;
			if( !ui ) return;
			ui.score.text = score.ValueRO.value.ToString();
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DisplayLevelCompleteSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelFinished>() );
		query.AddChangedVersionFilter( ComponentType.ReadOnly<LevelFinished>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var teams = GetSingletonBuffer<Team>();
		var teamScores = GetSingletonBuffer<ScoreElement>();
		int winningTeamIndex = -1;

		uint highestScore = 0;
		for( int index = 0; index < teamScores.Length; index++ ) {
			var teamScore = teamScores[ index ].value;

			if( teamScore > highestScore ) winningTeamIndex = index;
		}

		var victoryUI = InGameUI.current.victoryUI;
		if( winningTeamIndex == -1 ) victoryUI.teamNameLabel.text = "No team";
		else {
			victoryUI.teamNameLabel.text = $"Team {winningTeamIndex + 1}";
			victoryUI.teamNameLabel.color = (Vector4)teams[ winningTeamIndex ].color;
		}
		
		if( ClientServerBootstrap.HasServerWorld ) {
			victoryUI.nextMatchButton.gameObject.SetActive( true );
		}
		
		victoryUI.canvas.enabled = true;
	}
}