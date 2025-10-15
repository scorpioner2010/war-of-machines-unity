using TMPro;
using UnityEngine;
using DG.Tweening;
using Game.Scripts.Gameplay.Robots;

public class FloatingText : MonoBehaviour
{
    public TMP_Text text;
    public float duration = 0.8f;
    public float moveUp = 1.5f;
    public float endScale = 1.3f;
    private Camera _cam;
    
    public void SetText(string value)
    {
        _cam = CameraSync.In.gameplayCamera;
        
        text.text = value;
        var c = text.color;
        c.a = 1f;
        text.color = c;
        transform.localScale = Vector3.one;
        var seq = DOTween.Sequence();
        seq.Join(transform.DOMoveY(transform.position.y + moveUp, duration));
        seq.Join(text.DOFade(0f, duration));
        seq.Join(transform.DOScale(endScale, duration));
        seq.SetEase(Ease.OutQuad).OnComplete(() => Destroy(gameObject));
    }

    private void LateUpdate()
    {
        if (_cam != null) transform.forward = _cam.transform.forward;
    }
}