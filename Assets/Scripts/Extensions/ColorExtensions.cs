﻿using UnityEngine;


public static class ColorExtensions {

    public static Color ChangeColor( this Color original, 
        float? r = null, float? g = null, float? b = null, float? a = null )
    {
        return new Color(r ?? original.r, g ?? original.g, b ?? original.b, a ?? original.a);
    }

}
