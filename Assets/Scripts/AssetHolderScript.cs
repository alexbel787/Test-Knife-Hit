using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssetHolderScript : MonoBehaviour
{
    public GameObject[] ordinaryTargetPrefabs;
    public GameObject[] bossTargetPrefabs;
    public GameObject knifePrefab;
    public GameObject knifeSpritePrefab;
    public GameObject applePrefab;
    public Sprite[] knifeSkins;
    public Sprite[] bossSprites;
    public GameObject hitParticles;

    public Button menuButton;
    public GameObject menuObject;
    public Text recordLevelText;
    public Text recordAppleText;
    public GameObject resultObject;
    public Text levelReachedText;
    public Text appleCollectedText;
    public Text levelText;
    public Text applesText;
    public GameObject knivesObject;
    public Image sceneFadeImage;
}
