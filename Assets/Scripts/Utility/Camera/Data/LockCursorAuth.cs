using System;
using UnityEngine;

public class LockCursorAuth : MonoBehaviour
{
	void OnEnable() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}
