using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class U {

	public static float Det(Vector2 v1, Vector2 v2) {
        return v1.x * v2.y - v1.y * v2.x;
    }

    public static List<T> InverseList<T>(List<T> list) {
        if (list == null) return null;
        List<T> result = new List<T>();
        for (int i = list.Count-1; i >= 0; i--) {
            result.Add(list[i]);
        }
        return result;
    }

    public static float Sq(float n) {
        return n * n;
    }
}
