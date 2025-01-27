using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[BurstCompile] public static class DynamicBufExt {
	
	[BurstCompile] public static int IndexOf<T>( this DynamicBuffer<T> buf, T item ) where T : unmanaged {
		for( int index = 0; index < buf.Length; index++ ) {
			if( buf[ index ].Equals( item ) ) {
				return index;
			}
		}

		return -1;
	}
}
