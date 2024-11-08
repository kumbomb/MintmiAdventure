using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Hex class defines the grid position, world space position,size
/// negihbours, etc... of a hex tile. however, it does not interact with
/// unity directly in any way.
/// </summary>
public class Hex 
{
    public Hex(int q, int r)
    {
        this.Q = q;
        this.R = r;
        this.S = -(q + r);
    }
    //Q + R + S = 0

    //Hex ���迡�� ��ġ���� x => �б�����
    public readonly int Q; //Column
    public readonly int R; //Row
    public readonly int S;

    static readonly float WIDTH_MULTIPLIER = Mathf.Sqrt(3) / 2;

    /// <summary>
    /// Returns the world-space position of this hex
    /// </summary>
    /// <returns></returns>
    public Vector3 Position()
    {
        //x?,0,z?

        float radius = 1f;
        float height = radius * 2f;
        float width = WIDTH_MULTIPLIER * height;

        float vert = height * 0.75f;
        float horiz = width;

        return new Vector3(horiz * (this.Q + this.R/2f), 0, vert * this.R);

    }

}
