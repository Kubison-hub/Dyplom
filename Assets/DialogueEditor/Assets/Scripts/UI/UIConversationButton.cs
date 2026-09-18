using UnityEngine;
using UnityEngine.UI;

namespace DialogueEditor
{
    public class UIConversationButton : MonoBehaviour
    {
        public enum eHoverState
        {
            idleOff,
            animatingOn,
            idleOn,
            animatingOff,
        }

        public enum eButtonType
        {
            Option,
            Speech,
            End
        }

        // Getters
        public eButtonType ButtonType { get { return m_buttonType; } }

        // UI Elements
        [SerializeField] private TMPro.TextMeshProUGUI TextMesh = null;
        [SerializeField] private Image OptionBackgroundImage = null;
        private RectTransform m_rect;
        private CanvasGroup m_canvasGroup;
        private float m_transitionAlpha = 1f;
        private float m_usedAlpha = 1f;
        private float m_usedSelectedAlpha = 1f;
        private bool m_isUsed;
        private bool m_isSelected;

        // Node data
        private eButtonType m_buttonType;
        private ConversationNode m_node;    

        // Hovering 
        private float m_hoverT = 0.0f;
        private eHoverState m_hoverState;
        private bool Hovering { get { return (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.animatingOff); } }
        private Vector3 BigSize { get { return Vector3.one * 1.2f; } }


        //--------------------------------------
        // MonoBehaviour
        //--------------------------------------

        private void Awake()
        {
            m_rect = GetComponent<RectTransform>();
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (Hovering)
            {
                m_hoverT += Time.deltaTime;
                float normalised = m_hoverT / 0.2f;
                bool done = false;
                if (normalised >= 1)
                {
                    normalised = 1;
                    done = true;
                }
                Vector3 size = Vector3.one;
                float ease = EaseOutQuart(normalised);
                

                switch (m_hoverState)
                {
                    case eHoverState.animatingOn:
                        size = Vector3.Lerp(Vector3.one, BigSize, ease);
                        break;
                    case eHoverState.animatingOff:
                        size = Vector3.Lerp(BigSize, Vector3.one, ease);
                        break;
                }

                m_rect.localScale = size;

                if (done)
                {
                    m_hoverState = (m_hoverState == eHoverState.animatingOn) ? eHoverState.idleOn : eHoverState.idleOff;
                }
            }
        }




        //--------------------------------------
        // Input Events
        //--------------------------------------

        public void OnHover(bool hovering)
        {
            if (!ConversationManager.Instance.AllowMouseInteraction) { return; }

            if (hovering)
            {
                ConversationManager.Instance.AlertHover(this);
            }
            else
            {
                ConversationManager.Instance.AlertHover(null);
            }
        }

        public void OnClick()
        {
            if (!ConversationManager.Instance.AllowMouseInteraction) { return; }

            DoClickBehaviour();
        }

        public void OnButtonPressed()
        {
            DoClickBehaviour();
        }




        //--------------------------------------
        // Public calls
        //--------------------------------------

        public void SetHovering(bool selected)
        {
            if (selected && (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.idleOn)) { return; }
            if (!selected && (m_hoverState == eHoverState.animatingOff || m_hoverState == eHoverState.idleOff)) { return; }

            if (selected)
                m_hoverState = eHoverState.animatingOn;
            else
                m_hoverState = eHoverState.animatingOff;
            m_isSelected = selected;
            RefreshVisualAlpha();
            m_hoverT = 0f;
        }

        public void SetUsedVisual(bool isUsed, float usedAlpha, float selectedUsedAlpha)
        {
            m_isUsed = isUsed;
            m_usedAlpha = Mathf.Clamp01(usedAlpha);
            m_usedSelectedAlpha = Mathf.Clamp01(selectedUsedAlpha);
            RefreshVisualAlpha();
        }

        public void SetImage(Sprite sprite, bool sliced)
        {
            if (sprite != null)
            {
                OptionBackgroundImage.sprite = sprite;

                if (sliced)
                    OptionBackgroundImage.type = Image.Type.Sliced;
                else
                    OptionBackgroundImage.type = Image.Type.Simple;
            }
        }

        public void InitButton(OptionNode option)
        {
            // Set font
            if (option.TMPFont != null)
            {
                TextMesh.font = option.TMPFont;
            }
            else
            {
                TextMesh.font = null;
            }
        }

        public void SetAlpha(float a)
        {
            m_transitionAlpha = Mathf.Clamp01(a);
            RefreshVisualAlpha();
        }

        private void RefreshVisualAlpha()
        {
            if (m_canvasGroup == null)
                return;

            float stateAlpha = !m_isUsed
                ? 1f
                : m_isSelected ? m_usedSelectedAlpha : m_usedAlpha;
            m_canvasGroup.alpha = m_transitionAlpha * stateAlpha;
        }

        public void SetupButton(eButtonType buttonType, ConversationNode node, TMPro.TMP_FontAsset continueFont = null, TMPro.TMP_FontAsset endFont = null)
        {
            m_buttonType = buttonType;
            m_node = node;

            switch (m_buttonType)
            {
                case eButtonType.Option:
                    {
                        TextMesh.text = node.Text;
                        TextMesh.font = node.TMPFont;
                    }
                    break;

                case eButtonType.Speech:
                    {
                        TextMesh.text = "Kontynuuj";
                        TextMesh.font = continueFont;
                    }
                    break;

                case eButtonType.End:
                    {
                        TextMesh.text = "Koniec";
                        TextMesh.font = endFont;
                    }
                    break;
            }
        }




        //--------------------------------------
        // Private logic
        //--------------------------------------

        private void DoClickBehaviour()
        {
            switch (m_buttonType)
            {
                case eButtonType.Speech:
                    ConversationManager.Instance.SpeechSelected(m_node as SpeechNode);
                    break;

                case eButtonType.Option:
                    ConversationManager.Instance.OptionSelected(m_node as OptionNode);
                    break;

                case eButtonType.End:
                    ConversationManager.Instance.EndButtonSelected();
                    break;
            }
        }




        //--------------------------------------
        // Util
        //--------------------------------------

        private static float EaseOutQuart(float normalized)
        {
            return (1 - Mathf.Pow(1 - normalized, 4));
        }
    }
}
