﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;
using System.Xml.Serialization;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class SphereGrid : MonoBehaviour
{

    #region Member Variables
    public int xSize, ySize;
    public double radius;
    public bool insideOut;
    public bool openAtTop;
    public float wallHeight;
    public bool drawGizmos;
    public string wallPatternString;
    public string inputWallPath;
    public string outputWallPath;
    private MeshHolder ground;
    private MeshHolder vertWalls;
    private MeshHolder horzWalls;

    private Vector3[] vertices;
    private Mesh mesh;
    private bool[,] wallHere;
    private Vector3 lastClickPos;
    private Vector3 lastMouseOverPos;
    
    #endregion

    #region Starting/Saving Functions
    private void Awake()
    {
        if(!InputFileExists())
            SetUpWallPlan();
        
        GenerateAndExtrude();

        //SphereCollider collider = GetComponent<SphereCollider>();
        //collider.radius = (float)radius;

        //if (insideOut)
        //{
        //    //collider.radius *= -1;
        //    GetComponent<GravityAttractor>().gravity *= -1;
        //}

        if (wallPatternString == "")
            wallPatternString = "Try setting to'Test', 'All Floors', ...";
        //StartCoroutine(GenerateAndExtrude());
    }
    private bool InputFileExists()
    {
        if (inputWallPath == "")
        {
            //Debug.Log("No input path");
            return false;
        }
        try
        {
            string inputPath = @"Assets/SphereSaves/" + inputWallPath + ".xml";
            if (File.Exists(inputPath))
            {
                using (var reader = new StreamReader(inputPath))
                {
                    var serializer = new XmlSerializer(typeof(SphereSerializable));
                    var loadedSphere = (SphereSerializable)serializer.Deserialize(reader);
                    insideOut = loadedSphere.InsideOut;
                    radius = loadedSphere.Radius;
                    wallHeight = loadedSphere.WallHeight;
                    transform.localPosition = loadedSphere.LocalPos;
                    //if (transform.localPosition == null)
                    //    Debug.Log("Null transform");
                    //else
                    //    Debug.Log(String.Format("pos: {0}", transform.localPosition.ToString()));
                    transform.localEulerAngles = loadedSphere.LocalEuler;
                    transform.localScale = loadedSphere.LocalScale;

                    bool[][] jaggedWallArray = loadedSphere.WallHereJagged;
                    xSize = jaggedWallArray.Length;
                    ySize = jaggedWallArray[0].Length;
                    wallHere = new bool[xSize, ySize];
                    for (int i = 0; i < xSize; i++)
                    {
                        for (int j = 0; j < ySize; j++)
                        {
                            wallHere[i, j] = jaggedWallArray[i][j];
                        }
                    }
                }
            }
            else
            {
                Debug.Log("InputWallPaths aren't designated correctly");
                return false;
            }

        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Input File invalid - Error: {0}", e.Message));
            return false;
        }
       
        return true;
    }
    void OnDestroy()
    {
        if (String.IsNullOrEmpty(outputWallPath))
        {
            //Debug.Log("No output path");
            return;
        }
        try
        {
            bool[][] jaggedWallArray = new bool[xSize][];
            for (int i = 0; i < wallHere.GetLength(0); i++)
            {
                jaggedWallArray[i] = new bool[wallHere.GetLength(1)];
                for (int j = 0; j < wallHere.GetLength(1); j++)
                {
                    jaggedWallArray[i][j] = wallHere[i, j];
                }
            }
            Vector3 pos, rot, scale;
            pos = transform.localPosition;
            rot = transform.localEulerAngles;
            scale = transform.localScale;
            SphereSerializable saveMe = new SphereSerializable(insideOut, radius, jaggedWallArray, 
                                                                wallHeight, pos, rot, scale);
            string outputPath = @"Assets/SphereSaves/" + outputWallPath + ".xml";
            using (var stream = File.Create(outputPath))
            {
                var serializer = new XmlSerializer(typeof(SphereSerializable));
                serializer.Serialize(stream, saveMe);
            }
            if (File.Exists(outputPath))
            {
                Debug.Log("File created successfully");
            }
            else
            {
                Debug.Log("File not created successfully");
            }
        }
        catch (Exception)
        {
            Debug.Log("Problem saving file");
        }
    }

    #region Old Save/Load
    private bool oldInputFileExists()
    {
        if (inputWallPath == "")
            return false;

        try
        {
            if (File.Exists(@"Assets/SphereSaves/" + inputWallPath + ".xml") &&
                File.Exists(@"Assets/SphereSaves/isInverse-" + inputWallPath + ".xml"))
            {
                string inputPath = @"Assets/SphereSaves/" + inputWallPath + ".xml";
                using (var reader = new StreamReader(inputPath))
                {
                    var serializer = new XmlSerializer(typeof (bool[][]));
                    bool[][] jaggedWallArray = (bool[][]) serializer.Deserialize(reader);
                    wallHere = new bool[jaggedWallArray.Length, jaggedWallArray[0].Length];
                    for (int i = 0; i < jaggedWallArray.Length; i++)
                    {
                        for (int j = 0; j < jaggedWallArray[i].Length; j++)
                        {
                            wallHere[i, j] = jaggedWallArray[i][j];
                        }
                    }
                }
                string inverseInputPath = @"Assets/SphereSaves/isInverse-" + inputWallPath + ".xml";
                using (var reader = new StreamReader(inverseInputPath))
                {
                    var serializer = new XmlSerializer(typeof (bool));
                    insideOut = (bool) serializer.Deserialize(reader);
                }
            }
            else
            {
                Debug.Log("InputWallPaths aren't designated correctly");
                return false;
            }
                
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Input File invalid - Error: {0}",e.Message));
            return false;
        }
        // TODO set gravity to correct direction
        return true;
    }
    void oldOnDestroy()
    {
        if (outputWallPath == "")
            return;
        try
        {
            bool[][] jaggedWallArray = new bool[xSize][];
            for (int i = 0; i < wallHere.GetLength(0); i++)
            {
                jaggedWallArray[i] = new bool[wallHere.GetLength(1)];
                for (int j = 0; j < wallHere.GetLength(1); j++)
                {
                    jaggedWallArray[i][j] = wallHere[i, j];
                }
            }
            string outputPath = @"Assets/SphereSaves/" + inputWallPath + ".xml";
            using (var stream = File.Create(outputPath))
            {
                var serializer = new XmlSerializer(typeof(bool[][]));
                serializer.Serialize(stream, jaggedWallArray);
            }

            string inverseOutputPath = @"Assets/SphereSaves/isInverse-" + inputWallPath + ".xml";
            using (var stream = File.Create(inverseOutputPath))
            {
                var serializer = new XmlSerializer(typeof(bool));
                serializer.Serialize(stream, insideOut);
            }
        }
        catch (Exception)
        {
            Debug.Log("Problem saving file");

            throw;
        }

    }
    #endregion
    #endregion

    #region Wall Functions
    //private void ExtrudeWalls()
    //{
    //    for (int y = 0; y < ySize; y++)
    //    {
    //        for (int x = 0; x < xSize; x++)
    //        {
    //            if (wallHere[x, y])
    //            {
    //                int i = 2 * x + (4 * xSize) * y;
    //                int[] idxs = { i, i + 1, i + 2 * xSize, i + 1 + 2 * xSize };
    //                foreach (int idx in idxs)
    //                {
    //                    Vector3 extrudeDir = vertices[idx].normalized;
    //                    extrudeDir *= wallHeight;
    //                    if (insideOut)
    //                        extrudeDir *= -1;
    //                    vertices[idx] = vertices[idx] + extrudeDir;
    //                }
    //            }
    //        }
    //    }
    //    mesh.vertices = vertices;
    //    mesh.RecalculateBounds();
    //}

    private bool IsWall(int x, int y)
    {
        x = (x + xSize)%xSize;
        return wallHere[x, y];
    }

    private void ExtrudePoint(ref Vector3 point)
    {
        Vector3 extrudeDir = point.normalized;
        extrudeDir *= wallHeight;
        if (insideOut)
            extrudeDir *= -1;
        point += extrudeDir;
    }
    
    void SetUpWallPlan()
    {
        wallHere = new bool[xSize, ySize];
        string wallPattern = wallPatternString.ToLower();
        switch (wallPattern)
        {
            case "all walls":
                SetWallsToBool(true); 
                break;
            case "all floors":
                SetWallsToBool(false);
                break;
            case "test":
                SetTestWallPlan();
                break;
            case "half":
                SetTopHalfToWalls();
                break;
            default:
                SetWallsToBool(false); // Default to all floors
                break;
        }
    }

    private void SetTopHalfToWalls()
    {
        for (int y = 0; y < ySize/2; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                wallHere[x, y] = true;
            }
        }
    }
    private void SetWallsToBool(bool wallType)
    {
        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                wallHere[x, y] = wallType;
            }
        }
    }
    void SetTestWallPlan()
    {
        wallHere = new bool[xSize,ySize];
        //for (int i = 0; i < xSize; i++)
        //    wallPlan[i, 0] = wallPlan[i, ySize - 1] = true;
        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                if (y < 3 || y > (ySize - 3) || x < 3)
                    wallHere[x, y] = false;
                else if(y % 2 != 0)
                    wallHere[x, y] = true;
                //Debug.Log(String.Format("{0}", wallHere[x, y].ToString()));
            }
        }
    }
    #endregion

    //private void newGenerateAndExtrude()
    //{
    //    // TODO Create walls, perhaps using dictionary to help

    //    GetComponent<MeshFilter>().mesh = mesh = new Mesh();
    //    mesh.Clear();
    //    // TODO add multiple materials for ground/floors etc
    //    GetComponent<Renderer>().materials = materials;
    //    mesh.name = "Procedural Grid";

    //    N = 4*xSize*ySize;
    //    vertices = new Vector3[4 * xSize * ySize]; // 4 vertices per square
    //    Vector3[] normals = new Vector3[vertices.Length];
    //    //int[] triangles = new int[xSize * ySize * 6];
    //    List<int> triangleGroundList = new List<int>();
    //    List<int> triangleWallList = new List<int>();
    //    double deltaPhi = 2*Math.PI/xSize;
    //    double deltaTheta = Math.PI/ySize;
    //    double r = radius;

    //    // Set vertices
    //    for (int y = 0; y < ySize; y++)
    //    {
    //        for (int x = 0; x < xSize; x++)
    //        {
    //            int i = 2*x + (4*xSize)*y;
    //            double phi = 2*Math.PI*x/xSize; // X angle
    //            double theta = Math.PI*y/ySize; // Y angle 
    //            // Boxes are arranged 0 1 2    Each Square idx is arranged  0 1
    //            //                    3 4 5                                 2 3
    //            // Vertex 0
    //            vertices[i] = new Vector3(               
    //                (float) (r*Math.Sin(theta)*Math.Cos(phi)),
    //                (float) (r*Math.Cos(theta)),                
    //                (float) (r*Math.Sin(theta)*Math.Sin(phi)));
    //            // Vertex 1
    //            vertices[i + 1] = new Vector3(
    //                (float) (r*Math.Sin(theta)*Math.Cos(phi + deltaPhi)),
    //                (float) (r*Math.Cos(theta)),
    //                (float) (r*Math.Sin(theta)*Math.Sin(phi + deltaPhi)));
    //            // Vertex 2
    //            vertices[i + 2*xSize] = new Vector3(
    //                (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi)),
    //                (float)(r * Math.Cos(theta + deltaTheta)),
    //                (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi)));
    //            // Vertex 3
    //            vertices[i + 1 + 2*xSize] = new Vector3(
    //                (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi + deltaPhi)),
    //                (float)(r * Math.Cos(theta + deltaTheta)),
    //                (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi + deltaPhi)));

    //            // Triangle Idx of top left corner of current square
    //            // int ti = 6 * x + (6 * xSize) * y;
    //            int[] idxs = { i, i + 1, i + 2 * xSize, i + 1 + 2 * xSize };
    //            int TL = i;
    //            int TR = i + 1;
    //            int BL = i + 2 * xSize;
    //            int BR = i + 2 * xSize + 1;
    //            // Add both triangles, reversing order if inside out. 
    //            // TODO add normals to this
    //            AddTriangleToList(triangleGroundList, TL, TR, BR, insideOut);
    //            AddTriangleToList(triangleGroundList, TL, BR, BL, insideOut);

    //            // Add walls between this square and square to left
    //            AddLatitudeWall(triangleWallList, (x-1), y, x, y);
    //            // Add walls between this square and square above (towards north pole)
    //            if (y > 0) // Don't add if at north pole
    //                AddLongitudeWall(triangleWallList, x, (y - 1), x, y);

    //        }
    //    }
    //    for (int vIdx = 0; vIdx < vertices.Length; vIdx++)
    //    {
    //        // Find the direction from center to vertex, and if insideOut reverse it towards center
    //        Vector3 normal = vertices[vIdx] * (insideOut ? -1 : 1);
    //        normal.Normalize();
    //        normals[vIdx] = normal;
    //    }
    //    //for (int t = 0; t < triangleGroundList.Count; t++)
    //    //{
    //    //    Debug.Log(String.Format("{0} -> {1}", t, triangleGroundList[t]));
    //    //}

    //    mesh.vertices = vertices;
    //    mesh.normals = normals;
    //    int subMeshCount = 0;
    //    mesh.subMeshCount = 2;
    //    if (triangleGroundList.Count > 0)
    //    {
    //        mesh.SetTriangles(triangleGroundList, 0);
    //    }
    //    if (triangleWallList.Count > 0)
    //    {
    //        mesh.SetTriangles(triangleWallList, 2);
    //        Debug.Log(String.Format("wall count {0}", triangleWallList.Count/6));
    //    }
    //    else
    //    {
    //        Debug.Log(String.Format("f-wall count {0}", triangleWallList.Count / 6));
    //    }
    //    //mesh.subMeshCount = subMeshCount;

    //    //ExtrudeWalls();
    //    var meshc = GetComponent<MeshCollider>();
    //    meshc.sharedMesh = mesh;
    //    return;
    //}
    
    // Given the x,y coords of two patches, add vertical wall if one is wall, one is floor

    #region Old GenerateExtrude

    private void GenerateAndExtrude()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.Clear();
        // TODO add multiple materials for ground/floors etc
        //GetComponent<Renderer>().materials = materials;
        mesh.name = "Procedural Sphere";

        // Used to calculate angle of verts relative to top-left vert
        double deltaPhi = 2 * Math.PI / xSize;
        double deltaTheta = Math.PI / ySize;
        double r = radius;

        int yStart = openAtTop ? 2 : 0;
        // Create a ground meshholder first and fill it with verts,tris, normals
        ground = new MeshHolder(0);
        for (int y = yStart; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                double phi = 2 * Math.PI * x / xSize; // X angle
                double theta = Math.PI * y / ySize; // Y angle 

                var p0 = new Vector3( // Add TL vert
                    (float) (r*Math.Sin(theta)*Math.Cos(phi)), // "X"
                    (float) (r*Math.Cos(theta)), // Up Directions "Z"
                    (float) (r*Math.Sin(theta)*Math.Sin(phi)));// "Y"
                var p1 = new Vector3( // Add TR vert
                    (float) (r*Math.Sin(theta)*Math.Cos(phi + deltaPhi)),
                    (float) (r*Math.Cos(theta)),
                    (float) (r*Math.Sin(theta)*Math.Sin(phi + deltaPhi)));
                var p2 = new Vector3( // Add BL vert
                    (float) (r*Math.Sin(theta + deltaTheta)*Math.Cos(phi)),
                    (float) (r*Math.Cos(theta + deltaTheta)),
                    (float) (r*Math.Sin(theta + deltaTheta)*Math.Sin(phi)));
                var p3 = new Vector3( // Add BR vert
                    (float) (r*Math.Sin(theta + deltaTheta)*Math.Cos(phi + deltaPhi)),
                    (float) (r*Math.Cos(theta + deltaTheta)),
                    (float) (r*Math.Sin(theta + deltaTheta)*Math.Sin(phi + deltaPhi)));
                if (IsWall(x, y))
                {
                    ExtrudePoint(ref p0);
                    ExtrudePoint(ref p1);
                    ExtrudePoint(ref p2);
                    ExtrudePoint(ref p3);
                }
                ground.AddSquare(p0, p1, p2, p3, insideOut, true);
            }
        }
        // Start placing North/South facing walls
        horzWalls = new MeshHolder(ground.nextTriIdx);
        for (int y = yStart; y < ySize-1; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                double phi = 2 * Math.PI * x / xSize; // X angle
                double theta = Math.PI * y / ySize; // Y angle 

                var top_BL = new Vector3( // Add BL vert
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi)),
                    (float)(r * Math.Cos(theta + deltaTheta)),
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi)));
                var top_BR = new Vector3( // Add BR vert
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi + deltaPhi)),
                    (float)(r * Math.Cos(theta + deltaTheta)),
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi + deltaPhi)));

                if (IsWall(x, y))
                {
                    ExtrudePoint(ref top_BL);
                    ExtrudePoint(ref top_BR);
                }
                var bot_TL = new Vector3( // Add BL vert
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi)),
                    (float)(r * Math.Cos(theta + deltaTheta)),
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi)));
                var bot_TR = new Vector3( // Add BR vert
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi + deltaPhi)),
                    (float)(r * Math.Cos(theta + deltaTheta)),
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi + deltaPhi)));
                if (IsWall(x, y + 1))
                {
                    ExtrudePoint(ref bot_TL);
                    ExtrudePoint(ref bot_TR);
                }
                horzWalls.AddSquare(top_BL, top_BR, bot_TL, bot_TR, insideOut, false);
            }
        }

        // Start placing East/West facing walls
        //vertWalls = new MeshHolder(ground.nextTriIdx);
        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                double phi = 2 * Math.PI * x / xSize; // X angle
                double theta = Math.PI * y / ySize; // Y angle 

                var left_TR = new Vector3( // Add TL vert
                    (float)(r * Math.Sin(theta) * Math.Cos(phi)),
                    (float)(r * Math.Cos(theta)),
                    (float)(r * Math.Sin(theta) * Math.Sin(phi)));
                var left_BR = new Vector3( // Add BL vert
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi)),
                    (float)(r * Math.Cos(theta + deltaTheta)),
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi)));
                if (IsWall(x - 1, y))
                {
                    ExtrudePoint(ref left_TR);
                    ExtrudePoint(ref left_BR);
                }
                var right_TL = new Vector3( // Add TL vert
                    (float)(r * Math.Sin(theta) * Math.Cos(phi)),
                    (float)(r * Math.Cos(theta)),
                    (float)(r * Math.Sin(theta) * Math.Sin(phi)));
                var right_BL = new Vector3( // Add BL vert
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Cos(phi)),
                    (float)(r * Math.Cos(theta + deltaTheta)),
                    (float)(r * Math.Sin(theta + deltaTheta) * Math.Sin(phi)));
                if (IsWall(x, y))
                {
                    ExtrudePoint(ref right_TL);
                    ExtrudePoint(ref right_BL);
                }
                //vertWalls.AddSquare(left_BR, left_TR, right_BL, right_TL, insideOut);
                horzWalls.AddSquare(left_BR, left_TR, right_BL, right_TL, insideOut, false);
            }
        }

        //ground.verts.AddRange(vertWalls.verts);
        ground.verts.AddRange(horzWalls.verts);
        //ground.normals.AddRange(vertWalls.normals);
        ground.normals.AddRange(horzWalls.normals);

        
        mesh.SetVertices(ground.verts);
        mesh.SetNormals(ground.normals);
        //mesh.RecalculateNormals();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(ground.tris, 0);
        //mesh.SetTriangles(vertWalls.tris, 1);
        //mesh.SetTriangles(horzWalls.tris, 2);
        mesh.SetTriangles(horzWalls.tris, 1);


        var meshc = GetComponent<MeshCollider>();
        meshc.sharedMesh = mesh;
        

        #region Scrap Code

        //Vector2[] uv = new Vector2[vertices.Length];
        //Vector4[] tangents = new Vector4[vertices.Length];
        //Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);


        //for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        //{
        //    for (int x = 0; x < xSize; x++, ti += 6, vi++)
        //    {
        //        // Add 
        //        triangles[ti] = vi;
        //        triangles[ti + 3] = triangles[ti + 2] = vi + 1;
        //        triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
        //        triangles[ti + 5] = vi + xSize + 2;


        //        //yield return wait;
        //    }
        //}
        //mesh.triangles = triangles;

        #endregion

    }

    #endregion
    
    #region Special Functions "On_blah"
    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            //if (lastClickPos != null)
            //{
            //    Gizmos.color = Color.magenta;
            //    Gizmos.DrawSphere(transform.TransformPoint(lastClickPos), (float)radius / 20f);
            //}
            if (lastMouseOverPos != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(transform.TransformPoint(lastMouseOverPos), (float)radius / 40);
            }
                

            //Gizmos.color = Color.black;
            //for (int i = 0; i < vertices.Length; i++)
            //{
            //    Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.1f);
            //}
            //Gizmos.color = Color.magenta;
            //for (int y = 0; y < ySize; y++)
            //{
            //    for (int x = 0; x < xSize; x++)
            //    {
            //        if (wallHere[x, y])
            //        {
            //            int i = 2*x + (4*xSize)*y;
            //            int[] idxs = {i, i + 1, i + 2*xSize, i + 1 + 2*xSize};
            //            foreach (int idx in idxs)
            //            {
            //                Vector3 extrudeDir = vertices[idx].normalized;
            //                extrudeDir *= wallHeight;
            //                if (insideOut)
            //                    extrudeDir *= -1;
            //                Gizmos.DrawRay(transform.TransformPoint(vertices[idx]), extrudeDir);
            //             }
            //        }
            //    }
            //}
        }
    }

    void OnMouseOver()
    {
        var mousePos = Input.mousePosition;
        var hit = new RaycastHit();
        var ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out hit))
        {
            var clickPos = hit.point;
            var relativePos = hit.point - transform.position;
            Quaternion undoRotation = Quaternion.Inverse(transform.rotation);
            var relativeDir = lastMouseOverPos = undoRotation * relativePos;
            relativeDir.Normalize();
            //Debug.Log(String.Format("MouseP {0} ClickP {1} RelP {2} RelDir {3}",
            //    mousePos, clickPos, relativePos, relativeDir));

            // TODO Convert into angles using my spherical coordinates
            double deltaPhi = 2 * Math.PI / xSize;
            double deltaTheta = Math.PI / ySize;
            //var p0 = new Vector3( // Add TL vert
            //(float)(r * Math.Sin(theta) * Math.Cos(phi)), // "X" According to wikipedia convention
            //(float)(r * Math.Cos(theta)), // Up Directions   "Z"
            //(float)(r * Math.Sin(theta) * Math.Sin(phi)));// "Y"
            float x, y, z;
            x = relativeDir.x; y = relativeDir.z; z = relativeDir.y;
            
            //double phi = Math.Atan(y / x);
            double phi = Math.Atan2(y, x);
            double theta = Math.Acos(z);

            int xCoord = (int)Math.Floor(phi / deltaPhi);
            xCoord = (xCoord + xSize) % xSize;
            int yCoord = (int)Math.Floor(theta / deltaTheta);

            double newPhi = 2 * Math.PI * (xCoord + 0.5f) / xSize; // X angle
            double newTheta = Math.PI * (yCoord + 0.5f) / ySize; // Y angle 

            //lastMouseOverPos = new Vector3( // Add TL vert
            //    (float)(radius * Math.Sin(newTheta) * Math.Cos(newPhi)), // "X"
            //    (float)(radius * Math.Cos(newTheta)), // Up Directions "Z"
            //    (float)(radius * Math.Sin(newTheta) * Math.Sin(newPhi)));// "Y"

        }

    }

    void OnMouseDown()
    {
        var mousePos = Input.mousePosition;
        var hit = new RaycastHit();
        var ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out hit))
        {
            var clickPos = hit.point;
            var relativePos = hit.point - transform.position;
            Quaternion undoRotation = Quaternion.Inverse(transform.rotation);
            var relativeDir = lastClickPos = undoRotation* relativePos;
            relativeDir.Normalize();
            Debug.Log(String.Format("MouseP {0} ClickP {1} RelP {2} RelDir {3}",
                mousePos, clickPos, relativePos, relativeDir));


            // TODO Convert into angles using my spherical coordinates
            double deltaPhi = 2 * Math.PI / xSize;
            double deltaTheta = Math.PI / ySize;
            //var p0 = new Vector3( // Add TL vert
            //(float)(r * Math.Sin(theta) * Math.Cos(phi)), // "X" According to wikipedia convention
            //(float)(r * Math.Cos(theta)), // Up Directions   "Z"
            //(float)(r * Math.Sin(theta) * Math.Sin(phi)));// "Y"
            float x, y, z;
            x = relativeDir.x; y = relativeDir.z; z = relativeDir.y;
            Debug.Log(String.Format("xyz relative {0},{1},{2}", x, y, z));

            double phi = Math.Atan2(y, x);
            double theta = Math.Acos(z);

            int xCoord = (int)Math.Floor(phi/deltaPhi);
            xCoord = (xCoord + xSize)%xSize;
            int yCoord = (int)Math.Floor(theta/deltaTheta);


            if (xCoord >= 0 && xCoord < xSize && yCoord >= 0 && yCoord < ySize)
            {
                Debug.Log(String.Format("Successful, x,y {0},{1}", xCoord, yCoord));
                wallHere[xCoord, yCoord] = !wallHere[xCoord, yCoord];
                GenerateAndExtrude();
            }
            else
            {
                Debug.Log(String.Format("Failed, x,y: {0},{1}", xCoord, yCoord));
            }
        }
    }
    #endregion
}
