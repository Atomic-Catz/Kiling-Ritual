using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skeleton_Character
{
	


public class Skeleton_Rotate : MonoBehaviour
{
    public int rotateSpeed = 13;
	void Update () 
	{
		transform.Rotate (0,rotateSpeed * Time.deltaTime,0);
	}
}

}
