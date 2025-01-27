using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerScoreEntryUI : MonoBehaviour {
	public RectTransform rectTransform;
	[FormerlySerializedAs( "name" )] public TMP_Text nameLabel;
	public TMP_Text score;

	public Image background;
}
