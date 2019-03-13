using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//TEMP
using UnityEngine.UI;


public class DynamicTexture : MonoBehaviour {
    public int angle = 360;

    public float intensity = 1;

    public float radius = 5;
    public int pixelPerUnit = 128;

    public LayerMask colliderLayer;

    public ComputeShader shader;
    int indexOfKernel;


    float[] points;

    float[] startAngles;


    public RenderTexture texture;
    public Light light;


    public bool textureDraw;

    public bool gizDraw = true;
	// Use this for initialization
	void Start () {
        //On créer la texture et les tableaux de bonne taille

        //on ajoute 1 rayon pour evitet un bug nul
        angle++;

        texture = new RenderTexture(2 * (int)radius * pixelPerUnit, 2 * (int)radius * pixelPerUnit, 24);
        texture.enableRandomWrite = true;
        texture.Create();
        points = new float[angle];
        startAngles = new float[angle];

        indexOfKernel = shader.FindKernel("CSMain");

    }

    /*
    private void OnDrawGizmosSelected()
    {


            for (int i = 0; i < startAngles.Length; i++)
            {
                Handles.color = Color.yellow;
                float x = Mathf.Sin(startAngles[i]) * radius;
                float y = Mathf.Cos(startAngles[i]) * radius;
                Handles.Label(new Vector3(x, y, 0), i.ToString());
            }
        
    }*/

    // Update is called once per frame
    void Update () {
        //On boucle l'angle à 1 de plus pour éviter un bug nul
        if (angle > 361)
            angle -= 360;

        //Offset celon la rotation Z
        float offset = transform.eulerAngles.z % 360;

        //On shoot un raycast pour chaque rayon
        for (int i = 0; i < angle; i++)
        {

            startAngles[i] = Mathf.Round((1f / 360f * i * angle + offset) % 360 * Mathf.Deg2Rad*1000)/1000f;

            //On limite les valeurs pour éviter de dépasser 2PI
            if (startAngles[i] < 0.01f)
                startAngles[i] = 0.01f;

            if (startAngles[i] > 6.28f)
                startAngles[i] = 6.28f;

            float x = Mathf.Sin(startAngles[i]) * radius;
            float y = Mathf.Cos(startAngles[i]) * radius;



            RaycastHit2D hit;
            hit = Physics2D.Linecast(transform.position, new Vector2(transform.position.x + x, transform.position.y + y), colliderLayer);
            if(gizDraw)
                Debug.DrawLine(transform.position, new Vector2(transform.position.x + x, transform.position.y + y));
            if (hit.transform != null)
            {
                //On stock la position en % décimal par rapport au rayon max
                points[i] = Vector2.Distance(transform.position, hit.point) / radius;
                
            }
            else
            {
                //Sinon on stock presque 100% (bug quand c'est 100%)
                points[i] = 0.99f;
            }

        }

        if(gizDraw)
        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawRay(transform.position, new Vector2(Mathf.Sin(startAngles[i]) * radius, Mathf.Cos(startAngles[i]) * radius), Color.yellow);
        }

        generateTexture(2 * (int)radius * pixelPerUnit, 2 * (int)radius * pixelPerUnit);
        light.cookie = texture;
        // /!\ Manque la cookie size j'ai pas encore trouvé la formule
        light.range = radius + 1;
        light.spotAngle = 100;
        light.gameObject.transform.localPosition = new Vector3(0, 0, -radius);
        light.intensity = intensity;
        
    }

    void generateTexture(int width, int height)
    {
        texture.name = Random.Range(0, 999).ToString();
        Vector2 center = new Vector2(width / 2, height / 2);


        float[] size = new float[2];
        size[0] = width;
        size[1] = height;
        shader.SetFloats("size", size);

        shader.SetFloat("radius", radius);

        shader.SetInt("maxAngles", points.Length);

        ComputeBuffer start = new ComputeBuffer(startAngles.Length, sizeof(float));
        start.SetData(startAngles);
        shader.SetBuffer(indexOfKernel, "startAngles", start);



        ComputeBuffer pointsBuffer = new ComputeBuffer(points.Length, sizeof(float));
        pointsBuffer.SetData(points);
        shader.SetBuffer(indexOfKernel, "magAngles", pointsBuffer);

        shader.SetTexture(indexOfKernel, "Result", texture);

        // 32 32 1;
        int sx = (int)width/32;
        int sy = (int)height/ 32;

        shader.Dispatch(indexOfKernel,sx, sy, 1);




        start.Release();
        pointsBuffer.Release();
    }
}
