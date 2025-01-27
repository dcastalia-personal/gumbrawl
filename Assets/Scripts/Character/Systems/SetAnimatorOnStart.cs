using UnityEngine;

public class SetAnimatorOnStart : MonoBehaviour {
	public string animParam;
	public bool value;
	
	public Animator animator;

	void Start() {
		animator.SetBool( animParam, value );
	}
}
