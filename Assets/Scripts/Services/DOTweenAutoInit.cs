using DG.Tweening;
using UnityEngine;

public static class DOTweenAutoInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        DOTween.SetTweensCapacity(1200, 256);
    }
}
