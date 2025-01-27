using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pool : MonoBehaviour {

	Dictionary<Type, List<Component>> pool = new();

	public static Pool current;

	void Awake() {
		current = this;
	}

	public T Get<T>( GameObject prefab ) where T : Component {
		T pooledItem = null;
		if( pool.TryGetValue( typeof(T), out List<Component> instances ) ) {
			pooledItem = instances.FirstOrDefault( item => item != null && !item.gameObject.activeSelf ) as T;
		}
		else {
			pool.Add( typeof(T), new List<Component>() );
		}
		
		if( pooledItem == null ) {
			pooledItem = Instantiate( prefab.GetComponent<T>() );
			pool[ typeof(T) ].Add( pooledItem );
		}
		else {
			pooledItem.gameObject.SetActive( true );
		}

		return pooledItem;
	}
}
