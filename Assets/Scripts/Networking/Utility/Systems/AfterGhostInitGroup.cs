using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(GhostSimulationSystemGroup))] [UpdateAfter(typeof(InitGhostSystemGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class AfterGhostInitGroup : ComponentSystemGroup {}