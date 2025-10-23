using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Manages the connection requests of players
    /// </summary>
    /// <remarks>
    /// This should be placed on the same GameObject as the NetworkManager.
    /// </remarks>
    class ConnectionApprovalHandler : MonoBehaviour
    {
        /// <summary>
        /// used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        /// </summary>
        const int k_MaxConnectPayload = 1024;
        NetworkManager m_NetworkManager;

        void Start()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
            if (m_NetworkManager != null)
            {
                m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
                m_NetworkManager.ConnectionApprovalCallback = ApprovalCheck;
            }
        }

        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            /* you can use this method to customize one of more aspects of the player 
             * (I.E: their start position, their character) and to perform additional validation checks. */
            var connectionData = request.Payload;
            if (connectionData.Length > k_MaxConnectPayload)
            {
                /* If connectionData too high, deny immediately to avoid wasting time on the server. This is intended as
                a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage. */
                Debug.Log($"DOS attack detected by client with ID: {request.ClientNetworkId}");
                response.Approved = false;
                response.Reason = "DOS attack detected";
                return;
            }

            if ((m_NetworkManager.ConnectedClients.Count + CustomNetworkManager.Singleton.BotsSpawned) >= CustomNetworkManager.Singleton.ExpectedPlayers)
            {
                Debug.Log("Rejecting player since server is full");
                response.Approved = false;
                response.Reason = "Server is full";
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = true;
        }

        void OnClientDisconnectCallback(ulong obj)
        {
            if (!m_NetworkManager.IsServer && m_NetworkManager.DisconnectReason != string.Empty)
            {
                Debug.Log($"Server declined connection because: {m_NetworkManager.DisconnectReason}");
            }
        }
    }
}