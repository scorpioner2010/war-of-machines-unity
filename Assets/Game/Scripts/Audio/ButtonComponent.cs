using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Audio
{
    public class ButtonComponent : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IDeselectHandler, ISelectHandler
    {
        private enum ChildGraphicState
        {
            Normal,
            Highlighted,
            Pressed,
            Selected,
            Disabled
        }

        [SerializeField] private Button button;
        [SerializeField] private Graphic[] childGraphics = Array.Empty<Graphic>();

        private Color[] childGraphicColors;
        private bool initialized;
        private bool isPointerDown;
        private bool isPointerInside;
        private bool isSelected;
        private bool wasInteractable;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();

            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(PlayClickSound);
            ApplyChildGraphicColors(true, GetChildGraphicState());
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }

        private void Update()
        {
            if (button == null || !initialized)
            {
                return;
            }

            bool isInteractable = button.IsInteractable();
            if (wasInteractable == isInteractable)
            {
                return;
            }

            if (!isInteractable)
            {
                CacheChildGraphicColors();
            }

            wasInteractable = isInteractable;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        private void Initialize()
        {
            if (initialized || button == null)
            {
                return;
            }

            CacheChildGraphicColors();
            wasInteractable = button.IsInteractable();
            initialized = true;
        }

        private void CacheChildGraphicColors()
        {
            childGraphicColors = new Color[childGraphics.Length];

            for (int index = 0; index < childGraphics.Length; index++)
            {
                Graphic childGraphic = childGraphics[index];
                if (childGraphic == null)
                {
                    continue;
                }

                childGraphicColors[index] = childGraphic.color;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            ApplyChildGraphicColors(false, GetChildGraphicState());
        }

        private ChildGraphicState GetChildGraphicState()
        {
            if (button == null || !button.IsInteractable())
            {
                return ChildGraphicState.Disabled;
            }

            if (isPointerDown)
            {
                return ChildGraphicState.Pressed;
            }

            if (isSelected)
            {
                return ChildGraphicState.Selected;
            }

            if (isPointerInside)
            {
                return ChildGraphicState.Highlighted;
            }

            return ChildGraphicState.Normal;
        }

        private void ApplyChildGraphicColors(bool instant, ChildGraphicState state)
        {
            if (childGraphicColors == null)
            {
                return;
            }

            float fadeDuration = instant ? 0f : button.colors.fadeDuration;
            Color stateColor = GetStateColor(state) * button.colors.colorMultiplier;

            for (int index = 0; index < childGraphics.Length; index++)
            {
                Graphic childGraphic = childGraphics[index];
                if (childGraphic == null)
                {
                    continue;
                }

                Color targetColor = state == ChildGraphicState.Normal ? childGraphicColors[index] : stateColor;
                childGraphic.CrossFadeColor(targetColor, fadeDuration, true, true);
            }
        }

        private Color GetStateColor(ChildGraphicState state)
        {
            ColorBlock colors = button.colors;

            if (state == ChildGraphicState.Highlighted)
            {
                return colors.highlightedColor;
            }

            if (state == ChildGraphicState.Pressed)
            {
                return colors.pressedColor;
            }

            if (state == ChildGraphicState.Selected)
            {
                return colors.selectedColor;
            }

            if (state == ChildGraphicState.Disabled)
            {
                return colors.disabledColor;
            }

            return colors.normalColor;
        }

        private void PlayClickSound()
        {
            SoundCaller.PlayOneShot("SFX_UI_Button_Mouse_Thick_Generic_1");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(Button sourceButton)
        {
            button = sourceButton;

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            int childGraphicCount = 0;

            for (int index = 0; index < graphics.Length; index++)
            {
                Graphic graphic = graphics[index];
                if (graphic.transform == transform || graphic == button.targetGraphic)
                {
                    continue;
                }

                childGraphicCount++;
            }

            childGraphics = new Graphic[childGraphicCount];
            int childGraphicIndex = 0;

            for (int index = 0; index < graphics.Length; index++)
            {
                Graphic graphic = graphics[index];
                if (graphic.transform == transform || graphic == button.targetGraphic)
                {
                    continue;
                }

                childGraphics[childGraphicIndex] = graphic;
                childGraphicIndex++;
            }
        }
#endif
    }
}
