using System.Collections.Generic;
using UnityEngine;

public class powerJumpManager : MonoBehaviour {
	public List<curveData> CurveData;
	public List<Transform> icons;
    public List<Transform> bigJump;
    public List<Transform> enemy;
	public Transform pivotObject;
    public Transform blastParticleTrans;

    public ParticleSystem blastParticle;

	private int curveIndex = -1;
	private bool canMove;
    private Vector2 restPos;
    private List<float> enemyLivingSince;


    void Start(){
        restPos = Vector2.zero;
        restPos.x = persistentData.Instance.deathMargin - 5f;
        foreach(Transform item in bigJump)
        {
            item.position = restPos;
        }
        foreach (Transform item in enemy)
        {
            item.position = restPos;
        }
        enemyLivingSince = new List<float>(new float[enemy.Count]);
        persistentData.Instance.pjManager = null;
		persistentData.Instance.pjManager = this;
	}

	void Update(){
		if(canMove){
			if(persistentData.Instance.hPlayer.playerObj.position.y + persistentData.Instance.playerHalfWidth > pivotObject.position.y){
				bool check = true;
				int index = 0;
				while (check)
				{
					if(Vector2.Distance(icons[index].position, persistentData.Instance.hPlayer.playerObj.position) <= (persistentData.Instance.playerHalfWidth + 0.18f)){
						icons[index].localPosition = restPos;
						persistentData.Instance.hPlayer.powerJump(); // alter this function there are other types of jumps aswell
					}
					index = Mathf.Clamp(++index, 0, CurveData[curveIndex].data.Count);
					check = index != CurveData[curveIndex].data.Count;
				}
			}
            if (pivotObject.position.y < (persistentData.Instance.deathMargin - CurveData[curveIndex].highestPoint.y))
            {
                curveIndex = -1;
                canMove = false;
                foreach (Transform trans in icons)
                {
                    trans.position = restPos;
                }
            }
		}
        for(int i = 0; i < bigJump.Count; i++)
        {
            if (Vector2.Distance(bigJump[i].position, persistentData.Instance.hPlayer.playerObj.position) <= (persistentData.Instance.playerHalfWidth + 0.22f))
            {
                bigJump[i].position = restPos;
                persistentData.Instance.hPlayer.bigPowerJump();
            }
            if(bigJump[i].position.y < persistentData.Instance.deathMargin)
            {
                bigJump[i].position = restPos;
            }
        }

        // Moving enemy back and forth
        for(int i = 0; i < enemy.Count; i++)
        {
            if (enemy[i].position.x != restPos.x)
            {
                float enemyPosFactor = 1f * Mathf.Sin((enemyLivingSince[i] / 0.5f));
                enemyLivingSince[i] += Time.deltaTime;
                Vector2 tempPos = enemy[i].position;
                tempPos.x += enemyPosFactor * (2.5f * Time.deltaTime);
                enemy[i].position = tempPos;
                if (Vector2.Distance(persistentData.Instance.hPlayer.playerObj.position, enemy[i].position) <= (persistentData.Instance.playerHalfWidth + 0.18f))
                {
                    persistentData.Instance.canIncreaseRawScore = false;
                    blastParticleTrans.position = persistentData.Instance.hPlayer.playerObj.position;
                    persistentData.Instance.hPlayer.playerObj.position = restPos; // Make player out of screen and bring blast particle in
                    blastParticle.Play();
                    persistentData.Instance.makeGameOver();
                    Invoke("showGameoverUI", 2f);
                }
                if (enemy[i].position.y < persistentData.Instance.deathMargin)
                {
                    enemy[i].position = restPos;
                    enemyLivingSince[i] = 0f;
                }
            }
        }
        // ***************************
	}

    void showGameoverUI()
    {
        persistentData.Instance.showInterstitial();
    }

    public int EnabledCurveIndex{
		get{
			return curveIndex;
		}
	}

	public void showCurve(Vector2 onPosition){
        if (!canMove)
        {
            curveIndex = Random.Range(0, CurveData.Count);
            for (int i = 0; i < CurveData[curveIndex].data.Count; i++)
            {
                icons[i].localPosition = CurveData[curveIndex].data[i];
            }
            pivotObject.position = onPosition;
            canMove = true;
        }
	}

    public void placeBigJump(Vector2 atPos)
    {
        int bigJumpIndex = 0;
        while(bigJumpIndex >= 0)
        {
            if(bigJump[bigJumpIndex].position.x == restPos.x)
            {
                bigJump[bigJumpIndex].position = atPos;
                bigJumpIndex = -1;
            }
            else
            {
                bigJumpIndex++;
                if(bigJumpIndex == bigJump.Count)
                {
                    bigJumpIndex = -1;
                }
            }
        }
    }

    public void placeEnemy(Vector2 atPos)
    {
        int enemyIndex = 0;
        while(enemyIndex >= 0)
        {
            if(enemy[enemyIndex].position.x == restPos.x)
            {
                enemy[enemyIndex].position = atPos;
                enemyIndex = -1;
            }
            else
            {
                enemyIndex++;
                if(enemyIndex == enemy.Count)
                {
                    enemyIndex = -1;
                }
            }
        }
    }

    public void changePivotPosition(float withDelta)
    {
        if (canMove)
        {
            Vector2 tempPos = pivotObject.position;
            tempPos.y -= withDelta;
            pivotObject.position = tempPos;
        }
        foreach (Transform item in bigJump)
        {
            if (item.position.x != restPos.x)
            {
                Vector2 tempPos = item.position;
                tempPos.y -= withDelta;
                item.position = tempPos;
            }
        }
        foreach (Transform item in enemy)
        {
            if (item.position.x != restPos.x)
            {
                Vector2 tempPos = item.position;
                tempPos.y -= withDelta;
                item.position = tempPos;
            }
        }
    }
}
