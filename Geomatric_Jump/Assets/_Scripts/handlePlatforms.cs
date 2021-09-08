using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class handlePlatforms : MonoBehaviour
{
    public platformData[] platforms;
    public Transform startingBase;
    public Transform trailParticleTrans;
    public Transform[] jumpParticleTrans;

    public ParticleSystem trailParticle;
    public ParticleSystem[] jumpParticle;
    [HideInInspector]
    public Vector3 lastPlatPos;
    [HideInInspector]
    public int lastPlatIndex;


    private int checkingForPlatform;
    private bool initIndexLooping;
    private Vector2 lastPlayerPos;
    private List<int> cumulativeProbability;
    private Bounds currPlatBound;
    private ParticleSystem.Burst burstInstance;

    void Start()
    {
        persistentData.Instance.canIncreaseRawScore = true;
        persistentData.Instance.hPlat = null;
        persistentData.Instance.hPlat = this;
        lastPlatIndex = -1;
        StartCoroutine(detectClassInstance());
    }

    void Update()
    {
        if (persistentData.Instance.gameStarted && !persistentData.Instance.gameOver)
        {
            Vector3 playerLeftPoint = persistentData.Instance.hPlayer.playerObj.position;
            playerLeftPoint.x -= persistentData.Instance.playerHalfWidth;
            Vector3 playerRightPoint = persistentData.Instance.hPlayer.playerObj.position;
            playerRightPoint.x += persistentData.Instance.playerHalfWidth;
            playerLeftPoint.y = playerRightPoint.y = persistentData.Instance.hPlayer.playerObj.position.y - persistentData.Instance.playerHalfWidth;
            currPlatBound.center = platforms[checkingForPlatform].platTrans.position;
            currPlatBound.size = platforms[checkingForPlatform].platRend.size;
            if (!persistentData.Instance.hPlayer.jumpingUp)
            {
                persistentData.Instance.canIncreaseRawScore = false;
                if ((playerRightPoint.y <= currPlatBound.min.y) && (!currPlatBound.Contains(playerLeftPoint) || !currPlatBound.Contains(playerRightPoint)))
                {
                    checkingForPlatform = initIndexLooping ? Bezier.LoopIndex(--checkingForPlatform, platforms.Length) : Mathf.Clamp(--checkingForPlatform, 0, platforms.Length);
                }
                if (currPlatBound.Contains(playerRightPoint) || currPlatBound.Contains(playerLeftPoint))
                {
                    if (lastPlatIndex == -1 || platforms[checkingForPlatform].platTrans.position.y > platforms[lastPlatIndex].platTrans.position.y)
                    {
                        persistentData.Instance.canIncreaseRawScore = true;
                        lastPlatIndex = checkingForPlatform;
                    }
                    landedOnPlatform(playerLeftPoint.y);
                }
            }
            else
            {
                if (startingBase.position.x > persistentData.Instance.viewportLeft - 5f)
                {
                    Vector3 tempPos = startingBase.position;
                    tempPos.x -= 0.5f;
                    startingBase.position = tempPos;
                }
                if (playerRightPoint.y > currPlatBound.max.y)
                {
                    checkingForPlatform = Bezier.LoopIndex(++checkingForPlatform, platforms.Length);
                }
                if (persistentData.Instance.hPlayer.playerY > persistentData.Instance.playerTopMargin)
                {
                    float DeltaY = persistentData.Instance.hPlayer.playerY - lastPlayerPos.y;
                    panView(DeltaY);
                }
                if (persistentData.Instance.hPlayer.bigJumping)
                {
                    Vector2 tempPos = trailParticleTrans.position;
                    tempPos.y = playerLeftPoint.y;
                    tempPos.x = persistentData.Instance.hPlayer.playerObj.position.x;
                    trailParticleTrans.position = tempPos;
                    if (!trailParticle.isPlaying)
                    {
                        trailParticle.Play();
                    }
                }
                else
                {
                    if (trailParticle.isPlaying)
                    {
                        trailParticle.Stop();
                    }
                }
                persistentData.Instance.increaseRawScore();
            }
            lastPlayerPos.y = persistentData.Instance.hPlayer.playerY;
        }
    }

    void landedOnPlatform(float playerBase)
    {
        initIndexLooping = true;
        persistentData.Instance.hPlayer.toggleJump();
        if (platforms[checkingForPlatform].moveType == 0)
        {
            platforms[checkingForPlatform].moveTemporary();
        }
        for (int i = 0; i < 2; i++)
        {
            if (!jumpParticle[i].IsAlive())
            {
                Vector2 tempPos = jumpParticleTrans[i].position;
                tempPos.y = playerBase;
                tempPos.x = persistentData.Instance.hPlayer.playerObj.position.x;
                jumpParticleTrans[i].position = tempPos;
                jumpParticle[i].Play();
                i++;
            }
        }
    }

    void placePlat(int index)
    {
        int r = persistentData.Instance.score > 40f && persistentData.Instance.pjManager.EnabledCurveIndex == -1 ? Random.Range(1, 100) : Random.Range(cumulativeProbability[0] + 1, 100);
        if (r < cumulativeProbability[0])
        {
            placeCurve();
        }
        else
        {
            Vector2 platSize = Vector2.one;
            platSize.y = platforms[0].platRend.size.y;
            lastPlatPos.x = Random.Range(persistentData.Instance.viewportLeft + (platSize.x / 2f), persistentData.Instance.viewportRight - (platSize.x / 2f));
            lastPlatPos.y = lastPlatPos.y + persistentData.Instance.playerHalfWidth + Random.Range(0.3f, 1.5f);
            if (r < cumulativeProbability[1] && persistentData.Instance.score > 50f)
            {
                platforms[index].placeMoving(lastPlatPos, platSize);
            }
            else if (r < cumulativeProbability[2] && persistentData.Instance.score > 50f)
            {
                platforms[index].placeTemporary(lastPlatPos, platSize);
            }
            else
            {
                if (persistentData.Instance.score > 20f)
                {
                    r = Random.Range(0, 100);

                    if (r < cumulativeProbability[0])
                    {
                        if (persistentData.Instance.score > 50f && index > 3 && index < platforms.Length)
                        {
                            platSize.x = 2.5f;
                            lastPlatPos.x = Random.Range(persistentData.Instance.viewportLeft + (platSize.x / 2f), persistentData.Instance.viewportRight - (platSize.x / 2f));
                            Vector2 tempPos = lastPlatPos;
                            tempPos.x -= platSize.x / 2f;
                            tempPos.y += platSize.y / 2f + 0.5f;
                            persistentData.Instance.pjManager.placeEnemy(tempPos);
                        }
                        else if (index <= 3)
                        {
                            Vector2 tempPos = lastPlatPos;
                            tempPos.y += platSize.y / 2f + 0.5f;
                            persistentData.Instance.pjManager.placeBigJump(tempPos);
                        }
                    }
                }
                platforms[index].placeStatic(lastPlatPos, platSize);
            }
            lastPlatPos.y += platSize.y / 2f;
        }
    }

    void placeCurve()
    {
        lastPlatPos.x = 0f;
        lastPlatPos.y += 1.5f;
        persistentData.Instance.pjManager.showCurve(lastPlatPos);
        lastPlatPos.y = persistentData.Instance.pjManager.pivotObject.position.y + persistentData.Instance.pjManager.CurveData[persistentData.Instance.pjManager.EnabledCurveIndex].highestPoint.y;
    }

    void panView(float withDelta)
    {
        lastPlatPos.y = lastPlatPos.y - withDelta;
        for (int platIndex = 0; platIndex < platforms.Length; platIndex++)
        {
            Transform trans = platforms[platIndex].platTrans;
            Vector3 tempPos = trans.position;
            tempPos.y = tempPos.y - withDelta;
            trans.position = tempPos;
            if ((trans.position.y + currPlatBound.size.y / 2f) < persistentData.Instance.deathMargin)
            {
                placePlat(platIndex);
            }
        }
        for (int i = 0; i < 2; i++)
        {
            if (jumpParticleTrans[i].position.y > persistentData.Instance.deathMargin)
            {
                Vector3 tempPos = jumpParticleTrans[i].position;
                tempPos.y -= withDelta;
                jumpParticleTrans[i].position = tempPos;
            }
        }
        persistentData.Instance.pjManager.changePivotPosition(withDelta);
    }

    IEnumerator initParticles()
    {
        while (!persistentData.Instance.gameStarted)
        {
            float deltaX = Mathf.Abs(persistentData.Instance.hPlayer.playerObj.position.x - lastPlayerPos.x);
            Vector2 particlePos = jumpParticleTrans[0].position;
            particlePos.x = persistentData.Instance.hPlayer.playerObj.position.x;
            jumpParticleTrans[0].position = particlePos;
            if (deltaX >= 0.02f)
            {
                if (!jumpParticle[0].isEmitting)
                {
                    jumpParticle[0].Play();
                }
            }
            else
            {
                jumpParticle[0].Stop();
            }
            lastPlayerPos.x = persistentData.Instance.hPlayer.playerObj.position.x;
            yield return new WaitForEndOfFrame();
        }
        for (int i = 0; i < 2; i++)
        {
            var pBC = jumpParticle[i].emission;
            var burstInstance = pBC.GetBurst(0);
            jumpParticle[i].Stop();
            var pMain = jumpParticle[i].main;
            pMain.gravityModifier = 0.3f;
            pMain.loop = false;
            pMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            var pEmm = jumpParticle[i].emission;
            pEmm.rateOverTime = 0f;
            burstInstance.count = 10f;
            pEmm.SetBurst(0, burstInstance);
        }
    }

    IEnumerator detectClassInstance()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        while (persistentData.Instance.hPlayer == null)
        {
            yield return null;
        }
        lastPlatPos = new Vector3(0f, persistentData.Instance.hPlayer.playerObj.position.y - persistentData.Instance.playerHalfWidth);
        lastPlayerPos = persistentData.Instance.hPlayer.playerObj.position;
        lastPlayerPos.y = persistentData.Instance.hPlayer.playerY;
        cumulativeProbability = new List<int>();
        cumulativeProbability.Add(8); // For Powerjumps
        cumulativeProbability.Add(18); // For moving
        cumulativeProbability.Add(18); // For Temporary
        cumulativeProbability.Add(56); // For static
        currPlatBound = new Bounds(Vector3.zero, new Vector3(1f, platforms[0].platRend.size.y, 1f));
        for (int i = 1; i < cumulativeProbability.Count; i++)
        {
            cumulativeProbability[i] += cumulativeProbability[i - 1];
        }
        for (int i = 0; i < platforms.Length; i++)
        {
            placePlat(i);
        }
        persistentData.Instance.gameOver = false;
        persistentData.Instance.gameStarted = false;
        StartCoroutine(initParticles());
    }
}
