using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitServerIndexSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ServerIndex, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitServerIndexJob {}.Schedule( query );
	}

	[BurstCompile] partial struct InitServerIndexJob : IJobEntity {
	    
		void Execute( Entity self, ref ServerIndex serverIndex ) {
			serverIndex.value = self.Index;
		}
	}
}