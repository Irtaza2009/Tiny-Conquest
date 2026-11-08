using UnityEngine;
using TMPro;

public class RGBColour : MonoBehaviour
{
    [SerializeField] private TMP_Text creditText;
    [SerializeField] private float colourChangeSpeed = 1f;

    private void Update()
    {
        float hue = Mathf.Repeat(Time.time * colourChangeSpeed, 1f);

        creditText.color = Color.HSVToRGB(hue, 1f, 1f);
    }
}
