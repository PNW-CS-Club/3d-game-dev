using UnityEngine.UIElements;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class MatchView : View<GameApplication>
    {
        Button m_WinButton;
        Label m_TimerLabel;
        VisualElement m_Root;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;

            m_WinButton = m_Root.Q<Button>("winButton");
            m_WinButton.RegisterCallback<ClickEvent>(OnClickWin);

            m_TimerLabel = m_Root.Query<Label>("timerLabel");
        }

        void OnDisable()
        {
            m_WinButton.UnregisterCallback<ClickEvent>(OnClickWin);
        }

        internal void OnCountdownChanged(uint newValue)
        {
            m_TimerLabel.text = string.Format("{0:D2}:{1:D2}", newValue / 60, newValue % 60);
        }

        void OnClickWin(ClickEvent evt)
        {
            Broadcast(new WinButtonClickedEvent());
        }
    }
}
