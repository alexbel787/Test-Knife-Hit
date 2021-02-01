using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuHandler : MonoBehaviour
{
    private GameManagerScript GMS;
    private AssetHolderScript AHS;

    private void Awake()
    {
        GMS = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
        AHS = GameObject.Find("AssetHolder").GetComponent<AssetHolderScript>();
    }

    public void MenuButton()
    {
        Time.timeScale = 0;
        AHS.recordLevelText.text = PlayerPrefs.GetInt("maxLevel", 1).ToString();
        AHS.recordAppleText.text = PlayerPrefs.GetInt("maxApples", 0).ToString();
        GMS.SetMenuActive(true);
    }

    public void RestartButton()
    {
        GMS.disableInput = false;
        GMS.level = 1;
        GMS.apples = 0;
        StartCoroutine(RestartCoroutine());
    }

    private IEnumerator RestartCoroutine()
    {
        StartCoroutine(GMS.SceneFadeOut(true, 2));
        yield return new WaitForSeconds(.5f);
        SceneManager.LoadScene(0);
    }
}
