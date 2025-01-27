using System.Collections.Generic;
using System.Linq;
using Unity.Services.Relay.Models;

namespace Networking.Connection {

public static class RelayUtilities {
    public static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType) {
        return endpoints.FirstOrDefault(endpoint => endpoint.ConnectionType == connectionType);
    }
}

}