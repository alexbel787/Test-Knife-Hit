using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NotificationSamples;

public class GameManagerScript : MonoBehaviour
{
    public int level;
    [Range(0f, 5f)]
    public float nextKnifeDelay;
    [Range(0f, 4f)]
    public float targetRotationSpeed;
    [Range(0, 2f)]
    public float stopRotationDelay;
    [Range(1f, 5f)]
    public float rotationStopDuration;
    public int rotationDirecton = 1;
    public bool disableInput;
    private bool knifeReady;
    [HideInInspector]
    public bool bossLevel;
    private float bossHealth;
    private int bossCurrentSprite;
    private bool menuActive;
    private int knives;
    private List<GameObject> knivesList = new List<GameObject>();
    private int currentSkin;
    public int apples;
    private List<GameObject> allSpawnedObjectsList = new List<GameObject>();
    private List<GameObject> spawnedKnivesList = new List<GameObject>();
    private List<GameObject> spawnedApplesList = new List<GameObject>();
    private bool isEnoughSpace;

    public GameObject target;
    private GameObject knife;
    private Transform envT;
    public Coroutine cor;

    public Chance chanceToAppear;

    public GameNotificationsManager notificationsManager;

    public static GameManagerScript instance = null;
    private AssetHolderScript AHS;

    private void Awake()
    {
        if (instance == null) //Set DontDestroyOnLoad
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            level = 1;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        float screenRatio = (float)Screen.height / Screen.width; //Adjust UI and camera view to fit screen size
        if (screenRatio < 1.7f)
            Camera.main.GetComponent<CameraConstantWidth>().WidthOrHeight = 1f;
        else if (screenRatio > 1.95f)
            GameObject.Find("Canvas").GetComponent<CanvasScaler>().matchWidthOrHeight = .6f;

        InitializeNotifications(); //Push notifications init and shedule in 8 hours
        CreateNotification("Hello!", "It's time to throw knives! ", System.DateTime.Now.AddSeconds(28800));

        Vibration.Init(); 
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !disableInput)
        {
            // Check if there is a touch
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // Check if finger is over a UI element
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    //It means clicked on panel. So we do not consider this as click on game Object. Hence returning. 
                    Debug.Log("Touched button and return");
                    return;
                }
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                //It means clicked on panel. So we do not consider this as click on game Object. Hence returning. 
                Debug.Log("Clicked on button and return");
                return;
            }

            if (menuActive)         //Close main menu
            {
                SetMenuActive(false);
                Time.timeScale = 1;
            }
            else if (knifeReady)    //Throw knife
            {
                knifeReady = false;
                knife.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                knife.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, 3000));
                GameObject currentKnife = knivesList[knivesList.Count - 1];
                currentKnife.GetComponent<Image>().color = new Color(.5f, .5f, .5f);
                knivesList.Remove(currentKnife);
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            print("Reset PlayerPrefs");
            PlayerPrefs.DeleteAll();
        }
    }

    private void FixedUpdate()
    {
        target.transform.Rotate(0f, 0.0f, targetRotationSpeed * rotationDirecton * (Time.deltaTime * 60), Space.Self);
    }

    private void OnSceneLoaded(Scene aScene, LoadSceneMode aMode)
    {
        AHS = GameObject.Find("AssetHolder").GetComponent<AssetHolderScript>();
        knife = GameObject.Find("Environment/Knife");
        envT = GameObject.Find("Environment").transform;
        knives = 6;
        knifeReady = true;
        AHS.levelText.text = level.ToString();
        AHS.applesText.text = apples.ToString();

        //The higher level is - the more knives need to beat level;
        knives += level / 3;

        //Every 3rd level is a boss level, otherwise ordinary level
        if ((float)level / 3 == level / 3)
        {
            bossLevel = true;
            knives += 2;
            bossHealth = (float)knives / AHS.bossSprites.Length; //Knife hits need to change boss sprite
            bossCurrentSprite = 0;
            target = Instantiate(AHS.bossTargetPrefabs[0], new Vector2(0, 2.1f), Quaternion.identity, envT);
        }
        else
        {
            bossLevel = false;
            target = Instantiate(AHS.ordinaryTargetPrefabs[0], new Vector2(0, 2.1f), Quaternion.identity, envT);
        }

        //Set knife skin
        currentSkin = PlayerPrefs.GetInt("knifeSkin", 1) - 1;
        knife.GetComponentInChildren<SpriteRenderer>().sprite = AHS.knifeSkins[currentSkin];
        AHS.knivesObject.GetComponentInChildren<Image>().sprite = AHS.knifeSkins[currentSkin];

        //Set random target rotation
        targetRotationSpeed = Random.Range(3f, 4f);
        if (Random.Range(0, 2) == 0) rotationDirecton = -1;
        else rotationDirecton = 1;

        //Set knives quantity in UI
        GameObject _knife = AHS.knivesObject.GetComponentInChildren<Image>().gameObject;
        knivesList.Clear();
        knivesList.Add(_knife);
        for (int i = 1; i <= knives; i++)
        {
            GameObject obj = Instantiate(_knife, AHS.knivesObject.transform);
            obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, i * 75);
            knivesList.Add(obj);
        }

        //Spawn random apples and knives in the target
        allSpawnedObjectsList.Clear();
        spawnedApplesList.Clear();
        spawnedKnivesList.Clear();
        if (Random.value <= chanceToAppear.chance) SpawnObject(AHS.applePrefab);
        for (int i = 0; i < Random.Range(1, 4); i++) SpawnObject(knife);

        cor = StartCoroutine(RotationSpeedChange());
        StartCoroutine(SceneFadeOut(false, 2));
    }

    private void SpawnObject(GameObject prefab)
    {
        isEnoughSpace = false;
        Vector3 point = Vector3.zero;
        while (!isEnoughSpace)
        {
            point = Random.insideUnitCircle.normalized * 2.7f + new Vector2(target.transform.position.x, target.transform.position.y);
            if (allSpawnedObjectsList.Count > 0) //Check distance between spawned objects
            {
                foreach (GameObject _obj in allSpawnedObjectsList)
                {
                    if (Vector3.Distance(point, _obj.transform.position) < 1.1f)
                    {
                        isEnoughSpace = false;
                        break;
                    }
                    else isEnoughSpace = true;
                }
            }
            else isEnoughSpace = true;
        }
        point += new Vector3(0, 0, .1f);
        GameObject obj = Instantiate(prefab, point, Quaternion.identity);
        if (prefab.CompareTag("Apple"))
        {
            obj.transform.up = -(target.transform.position - obj.transform.position);
            spawnedApplesList.Add(obj);
        }
        else
        {
            Destroy(obj.GetComponent<KnifeScript>());
            obj.transform.up = target.transform.position - obj.transform.position;
            spawnedKnivesList.Add(obj);
        }
        allSpawnedObjectsList.Add(obj);
        obj.transform.SetParent(target.transform);
        obj.transform.eulerAngles = new Vector3(0, 0, obj.transform.eulerAngles.z);
    }

    private IEnumerator RotationSpeedChange()
    {
        float maxRotationSpeed = targetRotationSpeed;
        while (true)
        {
            while (targetRotationSpeed > 0)
            {
                targetRotationSpeed -= Time.deltaTime / rotationStopDuration * 5;
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(stopRotationDelay);
            while (targetRotationSpeed < maxRotationSpeed)
            {
                targetRotationSpeed += Time.deltaTime / rotationStopDuration * 5;
                yield return new WaitForEndOfFrame();
            }
        }
    }

    public void NextKnife()
    {
        if (knivesList.Count == 0)
        {
            print("Win");
            target.GetComponent<SpriteRenderer>().enabled = false;
            List<GameObject> list = new List<GameObject>();
            foreach (Transform t in target.transform) list.Add(t.gameObject);
            foreach (GameObject obj in list)
            {
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                    obj.AddComponent<Rigidbody2D>();
                }
                obj.transform.SetParent(null);
                obj.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                obj.GetComponent<Rigidbody2D>().gravityScale = 1;
                obj.GetComponent<Rigidbody2D>().AddForce((obj.transform.position - target.transform.position) * Random.Range(200, 400));
            }
            
            StartCoroutine(NextLevelCoroutine());
        }
        else StartCoroutine(NextKnifeCoroutine());
    }

    public IEnumerator NextKnifeCoroutine()
    {
        StartCoroutine(TargetFlashCoroutine());
        if (bossLevel)
        {
            bossHealth--;
            if (bossHealth < 0)
            {
                bossCurrentSprite++;
                target.GetComponent<SpriteRenderer>().sprite = AHS.bossSprites[bossCurrentSprite];
                bossHealth += (float)knives / AHS.bossSprites.Length;
            }
        }
        yield return new WaitForSeconds(nextKnifeDelay);
        knife = Instantiate(AHS.knifePrefab, new Vector3(0, -5.3f, .1f), Quaternion.identity, envT);
        knife.GetComponentInChildren<SpriteRenderer>().sprite = AHS.knifeSkins[currentSkin];
        knifeReady = true;
    }

    private IEnumerator TargetFlashCoroutine()
    {
        Vibration.VibratePop();
        target.GetComponent<SpriteRenderer>().color = new Color(1, .75f, 75f);
        target.transform.position += new Vector3(0, .1f, 0);
        yield return new WaitForSeconds(.1f);
        target.GetComponent<SpriteRenderer>().color = Color.white;
        target.transform.position += new Vector3(0, -.1f, 0);
    }

    private IEnumerator NextLevelCoroutine()
    {
        StopCoroutine(cor);
        long[] a = { 0, 200, 200, 400 };
        Vibration.Vibrate(a, -1);

        if (bossLevel) //Upgrade knife skin if available
        {
            if (AHS.knifeSkins.Length > currentSkin + 1)
            {
                yield return new WaitForSeconds(1f);
                SoundManager.instance.PlaySingle(SoundManager.instance.knifeSounds[3], 1f);
                var kn = Instantiate(AHS.knifeSpritePrefab, new Vector3(0, -4f, .1f), Quaternion.identity, envT);
                kn.GetComponent<SpriteRenderer>().sprite = AHS.knifeSkins[currentSkin + 1];
                for (float i = 0; i <= 1; i += Time.deltaTime * 2)
                {
                    kn.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, i);
                    yield return null;
                }
                Instantiate(AHS.hitParticles, kn.transform.position, Quaternion.identity, envT);

                PlayerPrefs.SetInt("knifeSkin", currentSkin + 2);
                PlayerPrefs.Save();
            }
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(1f);
        StartCoroutine(SceneFadeOut(true, 2));
        yield return new WaitForSeconds(.5f);
        level++;
        SceneManager.LoadScene(0);
    }

    public IEnumerator GameOverCoroutine()
    {
        disableInput = true;
        yield return new WaitForSeconds(.5f);
        SoundManager.instance.PlaySingle(SoundManager.instance.failSound, 1f);
        yield return new WaitForSeconds(1f);
        AHS.levelReachedText.text = level.ToString();
        AHS.appleCollectedText.text = apples.ToString();
        AHS.resultObject.SetActive(true);
        StopCoroutine(cor);
        if (level > PlayerPrefs.GetInt("maxLevel", 1)) PlayerPrefs.SetInt("maxLevel", level);
        if (apples > PlayerPrefs.GetInt("maxApples", 0)) PlayerPrefs.SetInt("maxApples", apples);
        PlayerPrefs.Save();
    }

    public void SetMenuActive(bool isOn)
    {
        menuActive = isOn;
        AHS.menuButton.interactable = !isOn;
        AHS.menuObject.SetActive(isOn);
    }

    public IEnumerator SceneFadeOut(bool fadeOut, float time)
    {
        if (fadeOut)
        {
            for (float i = 0; i <= 1; i += Time.deltaTime * time)
            {
                AHS.sceneFadeImage.color = new Color(0, 0, 0, i);
                yield return null;
            }
        }
        else
        {
            for (float i = 1; i >= 0; i -= Time.deltaTime * time)
            {
                AHS.sceneFadeImage.color = new Color(0, 0, 0, i);
                yield return null;
            }
            AHS.sceneFadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    private void InitializeNotifications()
    {
        GameNotificationChannel channel = new GameNotificationChannel("Identificator", "Title", "Description");
        notificationsManager.Initialize(channel);
    }

    private void CreateNotification(string title, string body, System.DateTime time)
    {
        IGameNotification notification = notificationsManager.CreateNotification();
        if (notification != null)
        {
            notification.Title = title;
            notification.Body = body;
            notification.DeliveryTime = time;
            notification.SmallIcon = "icon_0";
            notificationsManager.ScheduleNotification(notification);
        }
    }
}
