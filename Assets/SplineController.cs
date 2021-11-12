using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SplineController : MonoBehaviour
{

    public bool isTweening = false;
    public float tweenTime = 3f;
    public Transform tweenee;
    public LeanTweenType tweenType = LeanTweenType.easeInOutQuad;

    private int _tweenId;
    private bool _cachedIsTweening = false;

    public Transform[] controlPoints;
    public LineRenderer lineRenderer;
    public bool isQuadratic = true;
    
    private int curveCount = 0;    
    private int layerOrder = 0;
    private int SEGMENT_COUNT = 50;

    private int _cachedChildCount;
    
        
    void Awake()
    {
        if (!lineRenderer)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        print($"count: {transform.childCount}");
        SetControlPoints();

    }

    void SetControlPoints() {
        controlPoints = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++) {
            controlPoints[i] = transform.GetChild(i);
        }
        curveCount = (int)controlPoints.Length / 3;
        lineRenderer.sortingLayerID = layerOrder;
    }

    void Update()
    {
        if (controlPoints.Length == 0 || transform.childCount != _cachedChildCount) {
            SetControlPoints();
        }

        DrawCurve();

        if (isTweening && !_cachedIsTweening) {
            TweenSpline();
        }

        if (!isTweening && _cachedIsTweening) {
            LeanTween.cancel(_tweenId);
        }

        _cachedIsTweening = isTweening;
        _cachedChildCount = transform.childCount;
    }
    
    void DrawCurve()
    {
        var tolerancePoint = controlPoints[1];
        var xDist = Mathf.Abs((controlPoints[0].position-controlPoints[2].position).x);
        var tolerancePointPos = tolerancePoint.position;
        tolerancePointPos.x = controlPoints[0].position.x + (xDist / 2f);
        tolerancePoint.position = tolerancePointPos;

        lineRenderer.positionCount = SEGMENT_COUNT;
        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            float t = i / ((float)SEGMENT_COUNT-1f);
            Vector3 pixel;
            if (isQuadratic) {
                pixel = CalculateQuadBezierPoint(t, controlPoints[0].position, controlPoints[1].position, controlPoints[2].position);
            } else {
                pixel = CalculateCubicBezierPoint(t, controlPoints[0].position, controlPoints[1].position, controlPoints[2].position, controlPoints[3].position);
            }
            lineRenderer.SetPosition(i, pixel);
        }
    }

    Vector3 CalculateQuadBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2) {
        //  [(1-t)^2]P0 + 2(1-t)tP1 + t^2P2
        //      uu * p0 + (2 * u * t * p1) + (tt * p2)
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;

        return p;
    }
        
    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector3 p = uuu * p0; 
        p += 3 * uu * t * p1; 
        p += 3 * u * tt * p2; 
        p += ttt * p3; 
        
        return p;
    }

    void TweenSpline() {
        if (_tweenId > 0) {
            LeanTween.cancel(_tweenId);
        }

        Vector3[] points = new Vector3[transform.childCount];

        for (int i = 0; i < transform.childCount; i++) {
            points[i] = transform.GetChild(i).position;
        }

        _tweenId = LeanTween
            .moveSpline(tweenee.gameObject, points, tweenTime)
            .setEase(tweenType)
            .setLoopPingPong().id;
        
    }
}
