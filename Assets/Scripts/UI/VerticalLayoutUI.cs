using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class VerticalLayoutUI : MonoBehaviour {
	public float spacing;
	public RectTransform layout;
	
	List<RectTransform> layouts = new();

	public void Add( RectTransform newElement ) {
		newElement.SetParent( layout, false );
		layouts.Add( newElement );

		RegeneratePositions();

		// Debug.Log( $"Added {newElement.gameObject.name} to vertical layout {name}" );
	}

	public void Clear() {
		// Debug.Log( $"Clearing layout {name}" );
		foreach( var layout in layouts ) {
			Destroy( layout.gameObject );
		}
		
		layouts.Clear();
	}
	
	void RegeneratePositions() {
		float height = 0f;
		foreach( var element in layouts ) {
			element.anchoredPosition = new Vector2( 0f, height );
			height -= element.sizeDelta.y - spacing;
		}
	}
}

// TODO: Pooling