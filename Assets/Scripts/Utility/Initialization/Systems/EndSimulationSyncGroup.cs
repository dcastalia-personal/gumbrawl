using Unity.Entities;
using Unity.Scenes;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
public partial class EndSimulationSyncGroup : ComponentSystemGroup {}