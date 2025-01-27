using UnityEngine;

public class DeactivateTarget : MonoBehaviour {
	public GameObject target;
	
	public void DestroyNow() {
		target.gameObject.SetActive( false );
	}
}
