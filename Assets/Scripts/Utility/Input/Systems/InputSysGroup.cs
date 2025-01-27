using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(GhostInputSystemGroup), OrderFirst = true)]
public partial class InputSysGroup : ComponentSystemGroup {}

[UpdateInGroup(typeof(InputSysGroup), OrderLast = true)]
public partial class InputSideEffectsGroup : ComponentSystemGroup {} // for setting state you want to be valid the same frame you receive input