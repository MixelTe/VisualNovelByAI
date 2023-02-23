using UnityEngine;
using TMPro;

public class DialogPanel : MonoBehaviour
{
    public TMP_Text speakerElement;
    public TMP_Text textElement;

    public void DisplayDialog(string speaker, string portrait, string text)
    {
        if (speakerElement != null)
        {
            speakerElement.text = speaker;
        }
        if (textElement != null)
        {
            textElement.text = text;
        }
    }
}