using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Main view of the <see cref="MetagameApplication"></see>
    /// </summary>
    public class MetagameView : View<MetagameApplication>
    {
        internal MainMenuView MainMenu => m_MainMenuView;

        [SerializeField]
        MainMenuView m_MainMenuView;

        internal MatchmakerView Matchmaker => m_MatchmakerView;

        [SerializeField]
        MatchmakerView m_MatchmakerView;

        internal LoadingScreenView LoadingScreen => m_LoadingScreenView;

        [SerializeField]
        LoadingScreenView m_LoadingScreenView;

        void Start()
        {
            if (App.IsDedicatedServer)
            {
                OnDedicatedServerDestroyViews();
            }
        }

        void OnDedicatedServerDestroyViews()
        {
            Destroy(gameObject);
        }
    }
}
