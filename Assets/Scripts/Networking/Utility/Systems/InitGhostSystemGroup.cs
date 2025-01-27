using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(GhostSimulationSystemGroup))] [UpdateAfter(typeof(GhostUpdateSystem))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class InitGhostSystemGroup : ComponentSystemGroup {}