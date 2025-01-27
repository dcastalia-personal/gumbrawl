using UnityEngine;
using UnityEngine.Serialization;

public class CameraRig : MonoBehaviour {
	public static CameraRig main;
	[FormerlySerializedAs( "camera" )] public Camera cam;

	void OnEnable() {
		if( main ) {
			Destroy( main.gameObject );
		}
		
		main = this;
	}
}
