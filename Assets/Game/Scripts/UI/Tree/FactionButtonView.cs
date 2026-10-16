using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public sealed class FactionButtonView : MonoBehaviour
    {
        [Serializable]
        private struct FactionVisualStyle
        {
            [SerializeField] private string factionCode;
            [SerializeField] private Sprite background;
            [SerializeField] private Sprite icon;

            public bool Matches(string code)
            {
                return string.Equals(factionCode, code, StringComparison.OrdinalIgnoreCase);
            }

            public void Apply(Image backgroundImage, Image iconImage)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.sprite = background;
                }

                if (iconImage != null)
                {
                    iconImage.sprite = icon;
                }
            }
        }

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private FactionVisualStyle[] styles;

        public Button Button => button;

        public void Configure(string factionCode, string factionName)
        {
            if (title != null)
            {
                title.text = factionName;
            }

            if (styles == null)
            {
                return;
            }

            for (int i = 0; i < styles.Length; i++)
            {
                if (!styles[i].Matches(factionCode))
                {
                    continue;
                }

                styles[i].Apply(background, icon);
                return;
            }
        }
    }
}
