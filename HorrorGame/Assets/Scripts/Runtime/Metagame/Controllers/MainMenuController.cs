using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class MainMenuController : Controller<MetagameApplication>
    {
        MainMenuView View => App.View.MainMenu;

        void Awake()
        {
            AddListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            AddListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            AddListener<MatchLoadingEvent>(OnMatchLoading);
            AddListener<ExitedMatchmakerQueueEvent>(OnExitedMatchmakerQueue);
            AddListener<StartSinglePlayerModeEvent>(OnStartSinglePlayerMode);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<MatchLoadingEvent>(OnMatchLoading);
            RemoveListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            RemoveListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            RemoveListener<ExitedMatchmakerQueueEvent>(OnExitedMatchmakerQueue);
            RemoveListener<StartSinglePlayerModeEvent>(OnStartSinglePlayerMode);
        }

        void OnMatchLoading(MatchLoadingEvent evt)
        {
            if (!CustomNetworkManager.Singleton.AutoConnectOnStartup)
            {
                return; //then we're starting a match from the matchmaker
            }
            View.Hide();
            App.View.LoadingScreen.Show();
        }

        void OnEnterMatchmakerQueue(EnterMatchmakerQueueEvent evt)
        {
            View.Hide();
        }

        void OnExitMatchmakerQueue(ExitMatchmakerQueueEvent evt)
        {
            View.Show();
            View.FindMatchButton.SetEnabled(false); //needs to be called here as the defualt status of the button in the UI is enabled, so disabling it before showing the view does nothing
        }

        void OnExitedMatchmakerQueue(ExitedMatchmakerQueueEvent evt)
        {
            View.FindMatchButton.SetEnabled(true);
        }

        void OnStartSinglePlayerMode(StartSinglePlayerModeEvent evt)
        {
            View.Hide();
            CustomNetworkManager.Singleton.InitializeNetworkLogic(true, false);
        }
    }
}
