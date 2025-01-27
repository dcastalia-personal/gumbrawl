using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MatchSettingsEntryUI : MonoBehaviour {
	public TMP_InputField nameInput;
	public TMP_Dropdown teamDropdown;
	public Sprite optionSprite;

	public Entity playerEntity; // injected

	public void Initialize( DynamicBuffer<Team> teams ) {
		teamDropdown.options = new();

		for( int index = 0; index < teams.Length; index++ ) {
			var teamColor = teams[ index ].color;
			teamDropdown.options.Add( new TMP_Dropdown.OptionData { color = new Color( teamColor.x, teamColor.y, teamColor.z, teamColor.w ), image = optionSprite } );
		}
	}
}