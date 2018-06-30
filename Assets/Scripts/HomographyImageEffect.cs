using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

public class HomographyImageEffect : MonoBehaviour 
{
    [SerializeField] Shader homographyShader;
    [SerializeField, Range(0, 1)] float edgeSmoothness;
    [SerializeField] Color backgroundColor;
    [SerializeField] ViewportGizmo viewportGizmo;

    Material homographyMaterial;
    Matrix4x4 homographyMatrix;
    Vector3[] warpedViewportCorners = new Vector3[4];
    bool isEditing = false;

    // an util introduced to cope with serialization system limitations
    [System.Serializable]
    struct Vector3ArrayWrap
    {
        public Vector3[] arr;

        public Vector3ArrayWrap(Vector3[] arr_)
        {
            arr = arr_;
        }
    }

    const string SETTINGS_CORNERS_KEY = "homography";

    readonly static Vector3[] viewportCorners = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 0)
    };

    void OnEnable()
    {
        Reset();
    }

    void OnDisable()
    {
        viewportGizmo.enabled = isEditing = false;
    }

    void Update()
    {
        CheckEditState();

        if (isEditing && Input.GetKey(KeyCode.R))
        {
            ResetCorners();
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (enabled)
        {
            CheckHomography();
            homographyMaterial.SetColor("_BackgroundColor", backgroundColor);
            homographyMaterial.SetFloat("_EdgeSmoothness", edgeSmoothness);
            Graphics.Blit(src, dest, homographyMaterial);
            Graphics.SetRenderTarget(dest);
            if (viewportGizmo.isActiveAndEnabled)
            {
                viewportGizmo.RenderUI();
            }
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    void Reset()
    {
        LoadSettings();

        viewportGizmo.WriteViewportCorners(warpedViewportCorners);
        viewportGizmo.enabled = isEditing;

        if (homographyShader != null && (homographyMaterial == null || homographyMaterial.shader != homographyShader))
        {
            if (homographyMaterial != null)
            {
                Material.DestroyImmediate(homographyMaterial);
            }

            homographyMaterial = new Material(homographyShader);
            homographyMaterial.hideFlags = HideFlags.DontSave;
        }

        CheckHomography(true);
    }

    void CheckEditState()
    {
        // Ctrl + H to enter edit, Ctrl to exit edit
        var ctrlPressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        if (ctrlPressed && (Input.GetKey(KeyCode.H) || isEditing))
        {
            isEditing = !isEditing;
            viewportGizmo.enabled = isEditing;
            if (isEditing)
            {
                viewportGizmo.WriteViewportCorners(warpedViewportCorners);
            }
            else
            {
                SaveSettings();
            }
        }
    }

    void ResetCorners()
    {
        System.Array.Copy(viewportCorners, warpedViewportCorners, viewportCorners.Length);
        viewportGizmo.WriteViewportCorners(warpedViewportCorners);
    }

    void CheckHomography(bool force = false)
    {
        if (force || viewportGizmo.hasChanged)
        {
            viewportGizmo.ReadViewportCorners(warpedViewportCorners);
            homographyMatrix = FindHomography(warpedViewportCorners, viewportCorners);
            homographyMaterial.SetMatrix("_HomographyMatrix", homographyMatrix);
        }
        viewportGizmo.hasChanged = false;
    }

    void SaveSettings()
    {
        var ser = Vec3ArrayToString(warpedViewportCorners);
        Debug.Log("ser: " + ser);
        PlayerPrefs.SetString(SETTINGS_CORNERS_KEY, ser);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        if (!(PlayerPrefs.HasKey(SETTINGS_CORNERS_KEY) && 
              Vec3ArrayFromString(warpedViewportCorners, PlayerPrefs.GetString(SETTINGS_CORNERS_KEY))))
        {
            ResetCorners();
        }
    }

    string Vec3ArrayToString(Vector3[] arr)
    {
        return JsonUtility.ToJson(new Vector3ArrayWrap(arr));
    }

    bool Vec3ArrayFromString(Vector3[] arr, string str)
    {
        Vector3ArrayWrap data = JsonUtility.FromJson<Vector3ArrayWrap>(str);
        if (data.arr != null && data.arr.Length == arr.Length)
        {
            System.Array.Copy(data.arr, arr, arr.Length);
            return true;
        }
        Debug.LogError("Failed to parse [" + str + "]");
        return false;
    }

    // we end up using a 4x4 matrix instead of a 3x3 matrix as Unity only has a built in type for 4x4 matrices
    // and we want to limit MathNet footprint inside this class
    static Matrix4x4 FindHomography(Vector3[] fromCorners, Vector3[] toCorners)
    {
        double[][] arr = new double[8][];

        for (int i = 0; i != fromCorners.Length; ++i)
        {
            var p1 = fromCorners[i];
            var p2 = toCorners[i];
            arr[i * 2] = new double[] { -p1.x, -p1.y, -1, 0, 0, 0, p2.x * p1.x, p2.x * p1.y, p2.x };
            arr[i * 2 + 1] = new double[] { 0, 0, 0, -p1.x, -p1.y, -1, p2.y * p1.x, p2.y * p1.y, p2.y };
        }

        var svd = DenseMatrix.OfRowArrays(arr).Svd();
        var v = svd.VT.Transpose();

        // right singular vector
        var rsv = v.Column(v.ColumnCount - 1);

        // reshape to 3x3 matrix
        Matrix4x4 h = Matrix4x4.zero;
        h.SetRow(0, new Vector4((float)rsv[0], (float)rsv[1], (float)rsv[2], 0));
        h.SetRow(1, new Vector4((float)rsv[3], (float)rsv[4], (float)rsv[5], 0));
        h.SetRow(2, new Vector4((float)rsv[6], (float)rsv[7], (float)rsv[8], 0));

        return h;
    }
}
