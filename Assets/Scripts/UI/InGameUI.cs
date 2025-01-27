using TMPro;
using UnityEngine;

public class InGameUI : MonoBehaviour {
	public static InGameUI current;

	public TMP_Text levelCompleteLabel;
	public VerticalLayoutUI playerScoresLayout;
	public VerticalLayoutUI bubbleNotificationLayout;
	public VictoryUI victoryUI;
	
	public GameObject plusOnePopupPrefab;
}
