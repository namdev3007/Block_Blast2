using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class JellyTweenUI : MonoBehaviour, IPointerClickHandler
{
    public RectTransform target;
    public bool useUnscaledTime = true;
    public float time = 0.28f;

    Sequence _seq;

    void Awake()
    {
        if (!target) target = transform as RectTransform;
    }

    public void Play()
    {
        _seq?.Kill();
        target.localScale = Vector3.one;

        _seq = DOTween.Sequence().SetUpdate(useUnscaledTime);
        _seq.Append(target.DOScale(1.12f, time * 0.35f).SetEase(Ease.OutQuad));
        _seq.Append(target.DOScale(0.96f, time * 0.30f).SetEase(Ease.InOutQuad));
        _seq.Append(target.DOScale(1.04f, time * 0.20f).SetEase(Ease.OutQuad));
        _seq.Append(target.DOScale(1.00f, time * 0.15f).SetEase(Ease.InOutQuad));
    }

    public void OnPointerClick(PointerEventData e) => Play();
}
