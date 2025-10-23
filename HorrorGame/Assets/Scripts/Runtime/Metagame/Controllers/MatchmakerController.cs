using System;
using System.Collections;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
using UnityEngine;
using static Unity.Services.Matchmaker.Models.MultiplayAssignment;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class MatchmakerController : Controller<MetagameApplication>
    {
        MatchmakerView View => App.View.Matchmaker;
        Coroutine m_DisconnectIfOpponentDoesNotJoinRoutine;

        void Awake()
        {
            AddListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            AddListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            AddListener<MatchLoadingEvent>(OnMatchLoading);
            AddListener<ExitMatchLoadingEvent>(OnExitMatchLoading);
            AddListener<MatchEnteredEvent>(OnMatchEntered);
            App.OnReturnToMetagameAfterMatch -= OnReturnToMetagameAfterMatch;
            App.OnReturnToMetagameAfterMatch += OnReturnToMetagameAfterMatch;
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        void OnApplicationQuit()
        {
            StopMatchmaker();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            RemoveListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            RemoveListener<MatchLoadingEvent>(OnMatchLoading);
            RemoveListener<ExitMatchLoadingEvent>(OnExitMatchLoading);
            RemoveListener<MatchEnteredEvent>(OnMatchEntered);
        }

        void OnEnterMatchmakerQueue(EnterMatchmakerQueueEvent evt)
        {
            View.Show();
            CustomNetworkManager.Singleton.OnEnteredMatchmaker();
            UnityServicesInitializer.Instance.Matchmaker.FindMatch(evt.QueueName, OnMatchSearchCompleted, View.UpdateTimer);
        }

        void OnExitMatchmakerQueue(ExitMatchmakerQueueEvent evt)
        {
            StopMatchmaker();
            View.Hide();
        }

        void OnMatchSearchCompleted(SessionError sessionError)
        {
            var error = string.Empty;
            switch (sessionError)
            {
                case SessionError.None:
                case SessionError.Unknown:
                    Debug.Log("Match found!");
                    break;
                case SessionError.MatchmakerAssignmentFailed:
                    error = $"Failed to get ticket status.";
                    break;
                case SessionError.MatchmakerAssignmentTimeout:
                    error = "Could not find enough players in a reasonable amount of time";
                    break;
                case SessionError.MatchmakerCancelled:
                    Debug.Log("Matchmaker was cancelled");
                    break;
                default:
                    throw new InvalidOperationException($"Unmanaged session error: '{sessionError}'");
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                Broadcast(new ExitMatchmakerQueueEvent());
            }
        }

        void StopMatchmaker()
        {
            if (UnityServicesInitializer.Instance.Matchmaker)
            {
#pragma warning disable CS4014 // Can't await, so the method execution will continue
                UnityServicesInitializer.Instance.Matchmaker.StopSearch();
#pragma warning restore CS4014 // Can't await, so the method execution will continue
            }
        }

        void OnMatchLoading(MatchLoadingEvent evt)
        {
            if (CustomNetworkManager.Singleton.AutoConnectOnStartup)
            {
                return; //we're starting a match from the main menu
            }
            View.Hide();
            App.View.LoadingScreen.Show();
            CoroutinesHelper.StopAndNullifyRoutine(ref m_DisconnectIfOpponentDoesNotJoinRoutine, this);
            m_DisconnectIfOpponentDoesNotJoinRoutine = StartCoroutine(DisconnectIfOpponentDoesNotJoin());
        }

        IEnumerator DisconnectIfOpponentDoesNotJoin()
        {
            yield return CoroutinesHelper.FiveSeconds; //a long-enough grace period
            Debug.Log("No opponent joined even if server was instantiated: they probably quit the queue while the server was being initialized. Going back to matchmaking.");
            Broadcast(new ExitMatchLoadingEvent());
        }

        void OnMatchEntered(MatchEnteredEvent evt)
        {
            CoroutinesHelper.StopAndNullifyRoutine(ref m_DisconnectIfOpponentDoesNotJoinRoutine, this);
        }

        void OnReturnToMetagameAfterMatch()
        {
            if (UnityServicesInitializer.Instance.Matchmaker)
            {
#pragma warning disable CS4014 // Can't await, so the method execution will continue
                UnityServicesInitializer.Instance.Matchmaker.LeaveSession();
#pragma warning restore CS4014 // Can't await, so the method execution will continue
            }
        }

        void OnExitMatchLoading(ExitMatchLoadingEvent evt)
        {
            App.View.LoadingScreen.Hide();
            View.Show();
            CoroutinesHelper.StopAndNullifyRoutine(ref m_DisconnectIfOpponentDoesNotJoinRoutine, this);
            NetworkManager.Singleton.Shutdown();
            Broadcast(new EnterMatchmakerQueueEvent(UnityServicesInitializer.Instance.Matchmaker.LastQueueName));
        }
    }
}
