using UnityEngine;

namespace RAV.Scripts.UI.Tree
{
    public sealed class FactionTreeView : MonoBehaviour
    {
        [SerializeField] private RectTransform arrowsLayer;
        [SerializeField] private RectTransform rowsContainer;

        public RectTransform ArrowsLayer => arrowsLayer;
        public RectTransform RowsContainer => rowsContainer;
    }
}
