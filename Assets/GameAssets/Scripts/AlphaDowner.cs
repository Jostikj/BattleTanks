using System.Collections;
using UnityEngine;

public class Alpha : MonoBehaviour
{
    private Material _material;

    private void OnEnable()
    {
        _material = GetComponent<Renderer>().material;
        _material.SetTransparentMode();
    }

    public void StartCoroutine(float liveTime, float time)
    {
        StartCoroutine(AlphaDown(liveTime, time));
    }

    private IEnumerator AlphaDown(float liveTime, float time)
    {
        yield return new WaitForSeconds(liveTime);

        if (time > 0)
        {
            Color color = _material.color;
            float startAlpha = color.a;
            float elapsed = 0f;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / time);
                color.a = alpha;
                _material.color = color;
                yield return null;
            }

            gameObject.SetActive(false);
            color.a = 1f;
            _material.color = color;
        }
        else gameObject.SetActive(false);
    }
}
