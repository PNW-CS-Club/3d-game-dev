using UnityEngine.UIElements;
namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class MainMenuView : View<MetagameApplication>
    {
        internal Button FindMatchButton { get; private set; }
        Button m_QuitButton;
        Button m_SinglePlayerButton;
        Label m_TitleLabel;
        VisualElement m_Root;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;

            FindMatchButton = m_Root.Q<Button>("findMatchButton");
            FindMatchButton.RegisterCallback<ClickEvent>(OnClickFindMatch);

            m_SinglePlayerButton = m_Root.Q<Button>("singlePlayerButton");
            m_SinglePlayerButton.RegisterCallback<ClickEvent>(OnClickStartSinglePlayer);

            m_QuitButton = m_Root.Q<Button>("quitButton");
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);

            m_TitleLabel = m_Root.Query<Label>("titleLabel");
            m_TitleLabel.text = "Game title";

            CustomNetworkManager.OnConfigurationLoaded += OnGameConfigurationLoaded;
        }

        void OnGameConfigurationLoaded()
        {
            DisableControlsUnsupportedInAutoconnectMode();
        }

        void OnDisable()
        {
            FindMatchButton.UnregisterCallback<ClickEvent>(OnClickFindMatch);
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
            CustomNetworkManager.OnConfigurationLoaded -= OnGameConfigurationLoaded;
        }

        void OnClickFindMatch(ClickEvent evt)
        {
            Broadcast(new EnterMatchmakerQueueEvent("Standard"));
        }

        void OnClickStartSinglePlayer(ClickEvent evt)
        {
            Broadcast(new StartSinglePlayerModeEvent());
        }

        void OnClickQuit(ClickEvent evt)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        internal void DisableControlsUnsupportedInAutoconnectMode()
        {
            if (!CustomNetworkManager.Singleton.AutoConnectOnStartup)
            {
                return;
            }
            FindMatchButton.SetEnabled(false);
            m_SinglePlayerButton.SetEnabled(false);
        }
    }
}
