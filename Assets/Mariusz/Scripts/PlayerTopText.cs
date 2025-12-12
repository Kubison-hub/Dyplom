using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerTopText : MonoBehaviour
{
    public static PlayerTopText Instance;

    public TextMeshPro sherlockTopText;
    public TextMeshPro watsonTopText;

    public float textTime = 3f;

    private void Awake()
    {
        Instance = this;
    }

    public void ShotTopText(string sText = "", string wText = "")
    {
        StartCoroutine(ShowTopTextCor(sText, wText));
    }

    public IEnumerator ShowTopTextCor(string sText, string wText)
    {
        sherlockTopText.text = sText;
        yield return new WaitForSeconds(textTime);
        sherlockTopText.text = "";


        watsonTopText.text = wText;
        yield return new WaitForSeconds(textTime);
        watsonTopText.text = "";

        yield return null;
    }


}
