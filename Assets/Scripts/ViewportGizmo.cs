using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewportGizmo : MonoBehaviour 
{
    public bool hasChanged = false;

    [SerializeField] Color anchorColor;
    [SerializeField] Color selectedAnchorColor;
    [SerializeField] Color borderColor;
    [SerializeField] float anchorSelectionRadius;
    [SerializeField] Material guiMaterial;
    [SerializeField] float arrowToPositionFactor = 1.0f; // allowing precise anchor placement

    Vector3[] viewportCorners = new Vector3[4]; // CCW -> Counter Clock Wise
    bool[] selected = new bool[4];
    Dictionary<int, int> touchIdMap = new Dictionary<int, int>();
    TouchEmulation touchEmulation = new TouchEmulation();

    public bool ReadViewportCorners(Vector3[] corners)
    {
        if (corners.Length == viewportCorners.Length)
        {
            System.Array.Copy(viewportCorners, corners, viewportCorners.Length);
            return true;
        }
        Debug.LogError("Could not read viewport corners, parameter size mismatch.");
        return false;
    }

    public bool WriteViewportCorners(Vector3[] corners)
    {
        if (corners.Length == viewportCorners.Length)
        {
            System.Array.Copy(corners, viewportCorners, viewportCorners.Length);
            hasChanged = true;
            return true;
        }
        Debug.LogError("Could not write viewport corners, parameter size mismatch.");
        return false;
    }

    void OnEnable()
    {
        Reset();
    }

    void Update()
    {
        UpdateTouchControls();
        UpdateKeyboardAnchorSelection();
        UpdateArrowsControl();
    }

    public void RenderUI()
    {
        if (guiMaterial == null)
        {
            return;
        }

        GL.PushMatrix();
        GL.LoadIdentity();
        GL.LoadOrtho();

        guiMaterial.color = borderColor;
        guiMaterial.SetPass(0);
        GLDrawPath(viewportCorners, true);

        for (int i = 0; i != viewportCorners.Length; ++i)
        {
            guiMaterial.color = selected[i] ? selectedAnchorColor : anchorColor;
            guiMaterial.SetPass(0);
            GLDrawAnchor(viewportCorners[i], anchorSelectionRadius);
        }

        GL.PopMatrix();
    }

    void Reset()
    {
        DeselectAll();
        touchIdMap.Clear();
    }

    void UpdateTouchControls()
    {
        Touch mouseTouch;
        if (touchEmulation.MouseToTouch(out mouseTouch))
        {
            TouchHandler(mouseTouch);
        }

        foreach (var touch in Input.touches)
        {
            TouchHandler(touch);
        }
    }

    void UpdateArrowsControl()
    {
        Vector3 delta = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            delta += Vector3.up;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            delta += Vector3.down;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            delta += Vector3.right;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            delta += Vector3.left;
        }

        if (delta.sqrMagnitude > 0)
        {
            hasChanged = true;
            for (int i = 0; i != viewportCorners.Length; ++i)
            {
                if (selected[i])
                {
                    viewportCorners[i] += delta * arrowToPositionFactor;
                }
            }
        }
    }

    void UpdateKeyboardAnchorSelection()
    {
        selected[0] = Input.GetKey(KeyCode.Alpha0);
        selected[1] = Input.GetKey(KeyCode.Alpha1);
        selected[2] = Input.GetKey(KeyCode.Alpha2);
        selected[3] = Input.GetKey(KeyCode.Alpha3);
    }

    void TouchHandler(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                TouchDownHandler(touch);
                break;
            case TouchPhase.Moved:
                TouchMoveHandler(touch);
                hasChanged = true;
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                TouchUpHandler(touch);
                break;
            case TouchPhase.Stationary:
                break;
        }
    }

    void TouchDownHandler(Touch touch)
    {
        int index = -1;
        if (IsAnchorHit(ScreenToViewport(touch.position), out index))
        {
            // shouldn't happen based on Unity docs, but better safe than sorry
            if (touchIdMap.ContainsKey(touch.fingerId))
            {
                Debug.LogError("Touch Down with already stored finger ID.");
                return;
            }
            touchIdMap.Add(touch.fingerId, index);
            selected[index] = true;
        }
    }

    void TouchUpHandler(Touch touch)
    {
        if (touchIdMap.ContainsKey(touch.fingerId))
        {
            selected[touchIdMap[touch.fingerId]] = false;
            touchIdMap.Remove(touch.fingerId);
        }
    }

    void TouchMoveHandler(Touch touch)
    {
        if (touchIdMap.ContainsKey(touch.fingerId))
        {
            viewportCorners[touchIdMap[touch.fingerId]] = ScreenToViewport(touch.position);
        }
    }

    bool IsAnchorHit(Vector3 position, out int index)
    {
        for (int i = 0; i != viewportCorners.Length; ++i)
        {
            if ((position - viewportCorners[i]).magnitude < anchorSelectionRadius)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    Vector3 ScreenToViewport(Vector3 position)
    {
        return Camera.main.ScreenToViewportPoint(position);
    }

    void DeselectAll()
    {
        for (int i = 0; i != selected.Length; ++i)
        {
            selected[i] = false;
        }
    }

    static void GLDrawAnchor(Vector3 position, float scale)
    {
        GL.Begin(GL.LINE_STRIP);
        int resolution = 24;
        for (int i = 0; i != resolution + 1; ++i)
        {
            float angle = 2 * Mathf.PI * i / (float)resolution;
            GL.Vertex(position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * scale);
        }
        GL.End();
    }

    static void GLDrawPath(Vector3[] points, bool closed = false)
    {
        GL.Begin(GL.LINE_STRIP);
        int len = points.Length + (closed ? 1 : 0);
        for (int i = 0; i != len; ++i)
        {
            GL.Vertex(points[i % points.Length]);
        }
        GL.End();
    }
}
