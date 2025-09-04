using DG.Tweening;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class DOTweenBootstrap : MonoBehaviour
{
    [SerializeField] int tweenersCapacity = 1200;
    [SerializeField] int sequencesCapacity = 256;

    void Awake()
    {
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);
        DontDestroyOnLoad(gameObject);     // giữ lại khi đổi scene
    }
}
