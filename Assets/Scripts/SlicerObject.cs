using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Yr;

[RequireComponent(typeof(MeshFilter))]

public class SlicerObject : MonoBehaviour
{
    public Transform PlaneTransform;
    public Material capMaterial;

    private Yr.MeshCut _meshCutter = new Yr.MeshCut();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // For Debug
            gameObject.SetActive(false);

            GameObject[] sliced = _meshCutter.Slice(gameObject, PlaneTransform.up, PlaneTransform.position, capMaterial);

            foreach(var i in sliced)
            {
                i.name = string.Format("{0} ({1})", gameObject.name, i.name);

                if (i.GetComponent<MeshCollider>() == null)
                {
                    MeshCollider meshCollider = i.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = i.GetComponent<MeshFilter>().mesh;
                }

                if (i.GetComponent<SlicerObject>() == null) {
                    SlicerObject slicerObject = i.AddComponent<SlicerObject>();
                    slicerObject.PlaneTransform = this.PlaneTransform;
                    slicerObject.capMaterial = this.capMaterial;
                }
            }            
        }
    }

}
