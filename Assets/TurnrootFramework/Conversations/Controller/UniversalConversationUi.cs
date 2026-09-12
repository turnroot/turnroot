using TMPro;
using Turnroot.GameSettings;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Conversations
{
    public class UniversalConversationUi : MonoBehaviour
    {
        [Header("Conversation UI")]
        public GameObject _choiceButtonPrefab =>
            GamewideUiSettings.Instance.ConversationChoiceButtonPrefab;
        public Transform ChoiceButtonsContainer;

        [Header("Conversation UI")]
        public UIFade _uiFade;

        [Header("Dialogue UI")]
        public TextMeshProUGUI _dialogueText;
        public TextMeshProUGUI _speakerNameText;
        public Image _speakerPortraitImageActive;
        public Image _speakerPortraitImageInactive;
    }
}
