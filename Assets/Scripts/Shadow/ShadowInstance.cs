using UnityEngine;

public class ShadowInstance : MonoBehaviour
{
    public float shadowLength;

    public ShadowChunk chunk;

    private LineRenderer lineRenderer;
    private Vector3[] linePoints;
    private int positionCount;

    private Vector2[] vertices;
    private Vector2 shadowPos;

    private bool hasAwakened = false;

    public void ManualAwake()
    {
        GetShadowData();

        chunk.AddShadow(positionCount - 1, vertices, shadowLength, shadowPos, this);

        hasAwakened = true;
    }


    private void GetShadowData()
    {
        lineRenderer = GetComponent<LineRenderer>();
        linePoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePoints);
        positionCount = lineRenderer.positionCount;

        shadowPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(linePoints[0].x, linePoints[0].y);

        vertices = new Vector2[positionCount - 1];
        for (int i = 1; i < positionCount; i++)
        {
            vertices[i - 1] = new Vector2(linePoints[i].x, linePoints[i].y);
        }
    }


    private void OnDestroy()
    {
        if (!hasAwakened) return;
        try
        {
            chunk.manager.RemoveShadow(this);
        }
        catch
        {
        }
    }
}
