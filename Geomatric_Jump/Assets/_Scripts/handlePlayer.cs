using UnityEngine;
using System.Collections;
public class handlePlayer : MonoBehaviour, inputBase
{
    public Transform playerObj;
    public Transform PlatformMask;

    [HideInInspector]
    public float playerY, rawX;
    [HideInInspector]
    public bool jumpingUp, bigJumping;

    private float jumpCurrTime, jumpTotalTime, speed;
    private bool goingLeft;

    void Start()
    {
        persistentData.Instance.hPlayer = null;
        persistentData.Instance.hPlayer = this;
        playerY = playerObj.position.y;
        persistentData.Instance.ibColl.Add(this);
    }

    void Update()
    {
        if (!persistentData.Instance.gameOver)
        {
            Vector2 currPos = playerObj.position;
            if (persistentData.Instance.gameStarted)
            {
                float t = calculateT();
                if (!bigJumping)
                {
                    if (jumpingUp)
                    {
                        float speedStep = Mathf.Lerp(speed * Time.deltaTime, 0f, shootToOne(t, 1.2f));
                        playerY += speedStep;
                        if (speedStep == 0f)
                        {
                            toggleJump();
                        }
                        if (playerY < persistentData.Instance.playerTopMargin)
                        {
                            currPos.y = playerY;
                        }
                    }
                    else
                    {
                        currPos.y -= Mathf.Lerp(0f, speed * Time.deltaTime, shootToOne(Mathf.Clamp(t, 0f, 0.5f), 0.4f));
                        playerY = currPos.y;
                    }
                }
                else
                {
                    if (t != 1)
                    {
                        playerY += speed * Time.deltaTime;
                        if (playerY < persistentData.Instance.playerTopMargin)
                        {
                            currPos.y = playerY;
                        }
                    }
                    else
                    {
                        bigJumping = false;
                        powerJump();
                    }
                }
            }
            currPos.x = Mathf.Lerp(currPos.x, rawX, 0.175f);
            playerObj.position = currPos;
            if (((playerObj.position.x + persistentData.Instance.playerHalfWidth) < persistentData.Instance.viewportLeft && goingLeft) || ((playerObj.position.x - persistentData.Instance.playerHalfWidth) > persistentData.Instance.viewportRight && !goingLeft))
            {
                currPos.x *= -1f;
                playerObj.position = currPos;
                rawX = playerObj.position.x;
            }
            if (currPos.y < persistentData.Instance.deathMargin - persistentData.Instance.playerHalfWidth)
            {
                persistentData.Instance.makeGameOver();
                persistentData.Instance.showInterstitial();
            }
        }
    }

    public void OnAccelerometre(float velocityX)
    {
        goingLeft = velocityX <= 0f;
        rawX += velocityX;
    }

    public void OnTouch()
    {
        if (!persistentData.Instance.gameStarted)
        {
            toggleJump();
            StartCoroutine(RemovePlatformMask());
        }
    }

    private float calculateT()
    {
        jumpCurrTime += Time.deltaTime;
        if (jumpCurrTime >= jumpTotalTime)
        {
            jumpCurrTime = jumpTotalTime;
        }
        return jumpCurrTime / jumpTotalTime;
    }

    /// <summary>
    /// Returns from 0 to 1
    /// </summary>
    /// <param name="percent">Value between 0 to 1</param>
    /// <param name="dampingPower">Constant value, higher the value the more time it will take to damp</param>
    /// <returns></returns>
    private float shootToOne(float percent, float dampingPower)
    {
        return (1f - Mathf.Cos(Mathf.Pow(percent, dampingPower) * Mathf.PI)) / 2f;
    }

    public void toggleJump()
    {
        if (!bigJumping)
        {
            jumpingUp = !jumpingUp;
            jumpTotalTime = jumpingUp ? 0.6f : 0.35f;
            jumpCurrTime = 0f;
            playerY = playerObj.position.y;
            speed = 8f;
        }
    }
    public void powerJump()
    {
        if (!bigJumping)
        {
            jumpingUp = true;
            jumpTotalTime = 0.6f;
            jumpCurrTime = 0f;
            speed = 8f;
            persistentData.Instance.hPlat.lastPlatIndex = -1;
            persistentData.Instance.canIncreaseRawScore = true;
        }
    }

    public void bigPowerJump()
    {
        speed = 14f;
        jumpTotalTime = 1f;
        jumpCurrTime = 0f;
        jumpingUp = true;
        bigJumping = true;
        persistentData.Instance.canIncreaseRawScore = true;
    }

    IEnumerator RemovePlatformMask()
    {
        float currentTime = 0f;
        float totalTime = 1f;
        Vector3 StartPos = PlatformMask.position;
        Vector3 FinalPos = PlatformMask.position;
        FinalPos.y += Camera.main.ViewportToWorldPoint(Vector3.one).y + (PlatformMask.localScale.y / 2f); 
        while(currentTime < totalTime)
        {
            currentTime += Time.deltaTime;
            if(currentTime >= totalTime)
            {
                currentTime = totalTime;
            }
            float t = currentTime / totalTime;
            PlatformMask.position = Vector3.Lerp(StartPos, FinalPos, t);
            yield return new WaitForEndOfFrame();
        }
    }
}
