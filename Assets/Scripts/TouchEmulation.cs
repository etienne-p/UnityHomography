using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchEmulation 
{
    int id = -1;

    public bool MouseToTouch(out Touch touch)
    {
        TouchPhase phase = TouchPhase.Canceled;
        if (Input.GetMouseButtonDown(0))
        {
            id = Mathf.FloorToInt(Random.value * int.MaxValue);
            phase = TouchPhase.Began;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            phase = TouchPhase.Ended;
        }
        else if (Input.GetMouseButton(0))
        {
            phase = TouchPhase.Moved;
        }

        touch = new Touch();
        if (phase != TouchPhase.Canceled)
        {
            touch.fingerId = id;
            touch.phase = phase;
            touch.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
            return true;
        }
        return false;
    }
}
