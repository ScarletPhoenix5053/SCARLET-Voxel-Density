using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensityPlane_EditorMouse : MonoBehaviour
    {
        public VoxelDensityPlane VoxelDensityPlane;

        private VoxelBrush2D primaryBrush = new VoxelBrush2D();
        private VoxelBrush2D secondaryBrush = new VoxelBrush2D();
        
        void Start()
        {
            // Define Default brush
            primaryBrush.ValueDirectionPairs = new VoxelDirectionValuePair2D[]
            {
                new VoxelDirectionValuePair2D(0,0,1),
                new VoxelDirectionValuePair2D(-1,0,1),
                new VoxelDirectionValuePair2D(0,1,1),
                new VoxelDirectionValuePair2D(1,0,1),
                new VoxelDirectionValuePair2D(0,-1,1)
            };
            secondaryBrush.ValueDirectionPairs = new VoxelDirectionValuePair2D[]
            {
                new VoxelDirectionValuePair2D(0,0,0)/*,
                new VoxelDirectionValuePair2D(-1,0,0),
                new VoxelDirectionValuePair2D(0,1,0),
                new VoxelDirectionValuePair2D(1,0,0),
                new VoxelDirectionValuePair2D(0,-1,0)*/
            };
        }

        void Update()
        {
            // Allow editing of voxel values
            sbyte mouseDown = 0;
            if (Input.GetKey(KeyCode.Mouse0)) mouseDown--;
            if (Input.GetKey(KeyCode.Mouse1)) mouseDown++;

            if (mouseDown != 0)
            {
                var hit = new RaycastHit();
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("hi");
                    VoxelDensityPlane.ApplyVoxelBrush(
                       hit.point,
                       mouseDown < 0 ? primaryBrush : secondaryBrush
                       );
                }
            }
        }
    }
}