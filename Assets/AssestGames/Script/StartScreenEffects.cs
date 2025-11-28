using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StartScreenEffects : MonoBehaviour
{
    public Image background;
    public Image logo;
    public TMP_Text tapToStart;

    public float fadeTime = 1f;
    public float logoPopTime = 0.8f;
    public float blinkSpeed = 0.8f;

    Vector3 logoEndScale = new Vector3(6.447658f, 3.784079f, 5.695812f);

    void Start()
    {
        StartCoroutine(RunEffects());
    }

    IEnumerator RunEffects()
    {
        Color bg = background.color;
        bg.a = 0;
        background.color = bg;

        yield return FadeInBackground();

        logo.transform.localScale = Vector3.zero;
        yield return PopLogo();

        StartCoroutine(BlinkTapToStart());
    }

    IEnumerator FadeInBackground()
    {
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = t / fadeTime;
            Color c = background.color;
            c.a = a;
            background.color = c;
            yield return null;
        }
    }

    IEnumerator PopLogo()
    {
        float t = 0;
        while (t < logoPopTime)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, t / logoPopTime);
            logo.transform.localScale = Vector3.Lerp(Vector3.zero, logoEndScale, s);
            yield return null;
        }
    }

    IEnumerator BlinkTapToStart()
    {
        while (true)
        {
            float a = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            Color c = tapToStart.color;
            c.a = a;
            tapToStart.color = c;
            yield return null;
        }
    }
}
