using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : MonoBehaviour
{
    [SerializeField]
    float lerpTime;
    float visibleAlpha;
    Graphic graphic;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        visibleAlpha = graphic.color.a;
    }

    bool _visible = true;
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
                return;

            _visible = value;

            float targetAlpha = value ? visibleAlpha : 0f;

            StopAllCoroutines();

            _ = StartCoroutine(lerpAlpha(graphic.color.a, targetAlpha));
        }
    }

    IEnumerator lerpAlpha(float startAlpha, float targetAlpha)
    {
        var color = graphic.color;
        var time = 0f;

        while (Mathf.Abs(graphic.color.a - targetAlpha) > .01)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, time / lerpTime);
            graphic.color = color;

            yield return new WaitForEndOfFrame();
        }

        color.a = targetAlpha;
        graphic.color = color;
    }
}
