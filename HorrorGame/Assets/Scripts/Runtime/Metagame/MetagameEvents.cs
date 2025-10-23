namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class EnterMatchmakerQueueEvent : AppEvent
    {
        public string QueueName { get; private set; }

        public EnterMatchmakerQueueEvent(string queueName)
        {
            QueueName = queueName;
        }
    }

    internal class StartSinglePlayerModeEvent : AppEvent { }

    /// <summary>
    /// Called to stop the matchmaker
    /// </summary>
    internal class ExitMatchmakerQueueEvent : AppEvent { }
    /// <summary>
    /// Called after the matchmaking stops
    /// </summary>
    internal class ExitedMatchmakerQueueEvent : AppEvent { }
    internal class MatchLoadingEvent : AppEvent { }
    internal class ExitMatchLoadingEvent : AppEvent { }
    internal class PlayerSignedIn : AppEvent
    {
        public bool Success { get; private set; }
        public string PlayerId { get; private set; }

        public PlayerSignedIn(bool success, string playerId)
        {
            Success = success;
            PlayerId = playerId;
        }
    }
}
