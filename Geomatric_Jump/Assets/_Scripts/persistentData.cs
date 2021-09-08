#if UNITY_EDITOR
#define ON_PC
#endif
#if !UNITY_EDITOR && UNITY_ANDROID
#define ON_PHONE
#endif
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;
using GoogleMobileAds.Api;
using GooglePlayGames;
using System.Collections;

public class persistentData : MonoBehaviour
{
    public Sprite[] platSprites;
    [HideInInspector]
    public int score;
    [HideInInspector]
    public bool gameOver, gameStarted, canIncreaseRawScore;
    [HideInInspector]
    public float playerHalfWidth, viewportLeft, viewportRight, playerTopMargin, playerBottomMargin, deathMargin;
    [HideInInspector]
    public handlePlayer hPlayer = null;
    [HideInInspector]
    public powerJumpManager pjManager = null;
    [HideInInspector]
    public handlePlatforms hPlat = null;
    public List<inputBase> ibColl;

    public GameData GameData { get; private set; }

    public static persistentData Instance;

    private float rawScore;
    private bool _bannerLoaded;

    private BannerView _banner;
    private InterstitialAd _interstitial;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        MobileAds.Initialize("ca-app-pub-3940256099942544~3347511713");
        _banner = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.Banner, AdPosition.Top);
        _banner.OnAdLoaded += OnBannerLoaded;
        _banner.OnAdFailedToLoad += OnBannerLoadFailed;
        AdRequest rqst = new AdRequest.Builder().Build();
        _banner.LoadAd(rqst);
        _interstitial = new InterstitialAd("ca-app-pub-3940256099942544/1033173712");
        _interstitial.OnAdFailedToLoad += OnInterstitialFailedToLoad;
        _interstitial.OnAdClosed += OnInterstitialClosed;
        _interstitial.LoadAd(rqst);

        PlayGamesPlatform.Instance.Authenticate((result) => { });

        ibColl = new List<inputBase>();
        playerHalfWidth = GameObject.Find("Player_Default_2").GetComponent<SpriteRenderer>().size.x / 2f;
        Camera tempCam = Camera.main;
        viewportRight = tempCam.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0f, 0f)).x;
        viewportLeft = tempCam.ScreenToWorldPoint(new Vector3(0f, 0f, 0f)).x;
        playerTopMargin = tempCam.ViewportToWorldPoint(new Vector2(0f, 0.6f)).y;
        playerBottomMargin = tempCam.ViewportToWorldPoint(new Vector2(0f, 0.7f)).y;
        deathMargin = tempCam.ScreenToWorldPoint(new Vector2(0f, -playerHalfWidth)).y;
        try
        {
            if (File.Exists(Application.persistentDataPath + "/GameData.json"))
            {
                string jsonData = File.ReadAllText(Application.persistentDataPath + "/GameData.json");
                GameData = JsonUtility.FromJson<GameData>(jsonData);
            }
            else
            {
                GameData = new GameData();
                string jsonData = JsonUtility.ToJson(GameData);
                File.WriteAllText(Application.persistentDataPath + "/GameData.json", jsonData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in Reading/Writing game data : " + e.Message);
        }
    }

    void Update()
    {
        if (ibColl == null)
        {
            return;
        }
        if (!gameOver)
        {
#if ON_PC
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ibColl.ForEach(x => x.OnTouch());
                persistentData.Instance.gameStarted = true;
            }
            if (Input.GetKey(KeyCode.A))
            {
                ibColl.ForEach(x => x.OnAccelerometre(-0.15f));
            }
            if (Input.GetKey(KeyCode.D))
            {
                ibColl.ForEach(x => x.OnAccelerometre(0.15f));
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                ibColl.ForEach(x => x.OnAccelerometre(0f));
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                ibColl.ForEach(x => x.OnAccelerometre(0f));
            }
#endif
#if ON_PHONE
            if(Input.acceleration != Vector3.zero){
                ibColl.ForEach(x => x.OnAccelerometre(Mathf.Min(Mathf.Max(-0.15f, Input.acceleration.x * 0.6f), 0.15f)));
			}
#endif
        }
    }

    void uploadScore(int score)
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ReportScore(score, GPGSIds.leaderboard_qwetwr, (result) => { });
        }
    }

    public void StartGame()
    {
        if (!gameStarted)
        {
#if ON_PHONE
			if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)
			{
				ibColl.ForEach(x => x.OnTouch());
				persistentData.Instance.gameStarted = true;
                _banner.Show();
			}
#endif
        }
    }

    public void increaseRawScore()
    {
        rawScore += canIncreaseRawScore ? 8f * Time.deltaTime : 0f;
        if (rawScore > (score + 1))
        {
            score++;
        }
    }

    public void ToggleAudioVolume()
    {
        GameData.AudioMuted = !GameData.AudioMuted;
        try
        {
            if (File.Exists(Application.persistentDataPath + "/GameData.json"))
            {
                string jsonData = JsonUtility.ToJson(GameData);
                File.WriteAllText(Application.persistentDataPath + "/GameData.json", jsonData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in Writing game data : " + e.Message);
        }
    }

    public void makeGameOver()
    {
        GameData.HighestScore = score > GameData.HighestScore ? score : GameData.HighestScore;
        try
        {
            if (File.Exists(Application.persistentDataPath + "/GameData.json"))
            {
                string jsonData = JsonUtility.ToJson(GameData);
                File.WriteAllText(Application.persistentDataPath + "/GameData.json", jsonData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in Writing game data : " + e.Message);
        }
        _banner.Hide();
        ibColl.Clear();
        gameOver = true;
        rawScore = 0f;
        uploadScore(score);
        score = 0;
    }

    public void showInterstitial()
    {
        if (_interstitial.IsLoaded())
        {
            _interstitial.Show();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void showLeaderboard()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI(GPGSIds.leaderboard_qwetwr);
        }
    }

    public float GetBannerAdHeight
    {
        get
        {
            return _bannerLoaded ? _banner.GetHeightInPixels() : 0f;
        }
    }

    private void OnBannerLoaded(object sender, EventArgs e)
    {
        _banner.Hide();
        _bannerLoaded = true;
    }

    private void OnBannerLoadFailed(object sender, AdFailedToLoadEventArgs e)
    {
        _bannerLoaded = false;
    }

    private void OnInterstitialClosed(object sender, EventArgs e)
    {
        AdRequest rqst = new AdRequest.Builder().Build();
        _interstitial.LoadAd(rqst);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnInterstitialFailedToLoad(object sender, AdFailedToLoadEventArgs e)
    {
        StartCoroutine(RetryInterstitialLoad());
    }

    IEnumerator RetryInterstitialLoad()
    {
        yield return new WaitForSeconds(30f);
        AdRequest rqst = new AdRequest.Builder().Build();
        _interstitial.LoadAd(rqst);
    }
}

public class GameData
{
    public bool AudioMuted;
    public int HighestScore;
}
