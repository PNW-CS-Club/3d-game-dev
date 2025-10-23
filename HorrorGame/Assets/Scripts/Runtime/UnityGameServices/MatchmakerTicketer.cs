using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using SessionState = Unity.Services.Multiplayer.SessionState;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    ///<summary>
    ///Holds matchmaker search logic
    ///</summary>
    internal class MatchmakerTicketer : MonoBehaviour
    {
        internal string LastQueueName { get; private set; }
        internal bool Searching { get; private set; }
        string m_TicketId = "";
        Coroutine m_PollingCoroutine = null;
        CancellationTokenSource m_MatchmakerCancellationSource;
        ISession m_MatchmakerSession;

        internal async void FindMatch(string queueName, Action<SessionError> onMatchSearchCompleted, Action<int> onMatchmakerTicked)
        {
            try
            {
                if (!Searching)
                {
                    if (m_TicketId.Length > 0)
                    {
                        Debug.LogError($"Already matchmaking!");
                        return;
                    }

                    Searching = true;
                    await StartSearch(queueName, onMatchSearchCompleted, onMatchmakerTicked);
                }
            }
            catch (SessionException e)
            {
                StopSearch();
                MetagameApplication.Instance.Broadcast(new ExitMatchmakerQueueEvent());
                switch (e.Error)
                {
                    case SessionError.MatchmakerAssignmentFailed:
                    case SessionError.MatchmakerAssignmentTimeout:
                    case SessionError.MatchmakerCancelled:
                        onMatchSearchCompleted?.Invoke(e.Error);
                        break;
                    default:
                        Debug.LogError($"{e.Error}: {e.Message}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                StopSearch();
                MetagameApplication.Instance.Broadcast(new ExitMatchmakerQueueEvent());
            }
        }

        async Task StartSearch(string queueName, Action<SessionError> onMatchSearchCompleted, Action<int> onMatchmakerTicked)
        {
            var matchmakerOptions = new MatchmakerOptions
            {
                QueueName = queueName
            };

            var sessionOptions = new SessionOptions()
            {
                MaxPlayers = 2
            }.WithDirectNetwork();

            m_MatchmakerCancellationSource = new CancellationTokenSource();
            LastQueueName = queueName;

            CoroutinesHelper.StopAndNullifyRoutine(ref m_PollingCoroutine, this);
            m_PollingCoroutine = StartCoroutine(PollTicketStatus(onMatchSearchCompleted, onMatchmakerTicked));
            m_MatchmakerSession = await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, m_MatchmakerCancellationSource.Token);
        }

        internal async Task LeaveSession()
        {
            Debug.Log("Leaving the session of last match...");
            if (m_MatchmakerSession != null)
            {
                await m_MatchmakerSession.LeaveAsync();
                m_MatchmakerSession = null;
                Debug.Log("Session left!");
                return;
            }
            Debug.Log("No session to leave.");
        }

        internal void StopSearch()
        {
            CoroutinesHelper.StopAndNullifyRoutine(ref m_PollingCoroutine, this);
            if (m_MatchmakerCancellationSource != null
            && !m_MatchmakerCancellationSource.IsCancellationRequested)
            {
                m_MatchmakerCancellationSource.Cancel();
                m_MatchmakerCancellationSource.Dispose();
            }
            Searching = false;
            MetagameApplication.Instance.Broadcast(new ExitedMatchmakerQueueEvent());
        }

        IEnumerator PollTicketStatus(Action<SessionError> onMatchSearchCompleted, Action<int> onMatchmakerTicked)
        {
            bool polling = true;
            int elapsedTime = 0;

            while (polling)
            {
                yield return CoroutinesHelper.OneSecond;
                elapsedTime++;
                onMatchmakerTicked?.Invoke(elapsedTime);

                try
                {
                    if (m_MatchmakerSession != null)
                    {
                        switch (m_MatchmakerSession.State)
                        {
                            case SessionState.None:
                                //Do nothing
                                break;
                            case SessionState.Connected:
                            case SessionState.Disconnected:
                            case SessionState.Deleted:
                                polling = false;
                                break;
                            default:
                                throw new InvalidOperationException($"Unmanaged session state: '{m_MatchmakerSession.State}'");
                        }
                    }
                }
                catch (SessionException ex)
                {
                    StopSearch();
                    onMatchSearchCompleted?.Invoke(ex.Error);
                    throw;
                }
            }

            StopSearch();
            onMatchSearchCompleted?.Invoke(SessionError.None);
        }
    }
}
