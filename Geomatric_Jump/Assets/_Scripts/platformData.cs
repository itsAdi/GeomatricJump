using UnityEngine;

public class platformData : MonoBehaviour {
	public Transform platTrans;
	public SpriteRenderer platRend;
	[HideInInspector]
	public int moveType = -1; // -1 for nothing and 0 for temporary and 1 for looping

	bool move;
	Vector3 startPos;
	float currTime, totalTime, toX, fromX, amplitude = 1f;

	void Update(){
		if(move){
			if(moveType == 0){
				if(currTime < totalTime){
					currTime += Time.deltaTime;
					if(currTime > totalTime){
						currTime = totalTime;
					}
					float t = currTime / totalTime;
					t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
					Vector3 tempPos = platTrans.position;
					tempPos.x = Mathf.Lerp(fromX, toX, t);
					platTrans.position = tempPos;
				}
			}
			if(moveType == 1){
				float theta = Time.timeSinceLevelLoad / totalTime;
				float dist = amplitude * Mathf.Sin(theta);
				startPos.y = platTrans.position.y;
				platTrans.position = startPos + Vector3.right * dist;
			}
		}
	}

	public void placeStatic(Vector2 withPos, Vector2 withSize){
		platRend.sprite = persistentData.Instance.platSprites[0];
		changeTrans(withPos, withSize);
		moveType = -1;
	}

	public void placeMoving(Vector2 withPos, Vector2 withSize){
		placeStatic(withPos, withSize);
		startPos = withPos;
		moveType = 1;
		totalTime = 0.5f;
		move = true;
	}

	public void placeTemporary(Vector2 withPos, Vector2 withSize){
		platRend.sprite = persistentData.Instance.platSprites[1];
		changeTrans(withPos, withSize);
		totalTime = 1f;
		moveType = 0;
	}

	public void moveTemporary(){
		fromX = platTrans.position.x;
		toX = platTrans.position.x - (persistentData.Instance.viewportRight + 5f);
		move = true;
	}

	void changeTrans(Vector2 withPos, Vector2 withSize){
		move = false;
		currTime = 0f;
		platTrans.position = withPos;
		platRend.size = withSize;
	}
}
