using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensityVolumeEditorCrane : MonoBehaviour
    {
        #region Modifiable Variables

        [Header("Cursor")]
        public Transform Cursor;

        [Header("Scroll Wheel")]
        public bool ScrollWheelInverted = true;
        public float ScrollWheelSensitivity = 1f;

        [Header("Crane Limits")]
        public float CraneHeightMin = 0;
        public float CraneHeightMax = 10;

        #endregion

        #region Encapsulated Variables

        private VoxelBrush2D primaryBrush = new VoxelBrush2D();
        private VoxelBrush2D secondaryBrush = new VoxelBrush2D();

        private float craneHeight = 0f;
        private Vector3 cranePos = Vector3.zero;

        #endregion

        #region Unity Messages

        void Start()
        {
            // Define Default brush
            primaryBrush.ValueDirectionPairs = new VoxelDirectionValuePair2D[]
            {
                new VoxelDirectionValuePair2D(0,0,1)/*,
                new VoxelDirectionValuePair2D(-1,0,1),
                new VoxelDirectionValuePair2D(0,1,1),
                new VoxelDirectionValuePair2D(1,0,1),
                new VoxelDirectionValuePair2D(0,-1,1)*/
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
            // Track mouse state
            sbyte mouseDown = 0;
            if (Input.GetKey(KeyCode.Mouse0)) mouseDown--;
            if (Input.GetKey(KeyCode.Mouse1)) mouseDown++;

            var hit = new RaycastHit();
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                // Get mouse position along collider surface
                cranePos = hit.point;

                // Adjust height with mouse wheel
                float mouseScroll = Input.GetAxis("Mouse ScrollWheel") * ScrollWheelSensitivity;
                craneHeight = Mathf.Clamp(
                     craneHeight + (ScrollWheelInverted ? -mouseScroll : mouseScroll),
                     CraneHeightMin,
                     CraneHeightMax
                     );

                // Apply height to get "crane" position
                cranePos += Vector3.up * craneHeight;

                // Display cursor at "crane" position
                Cursor.gameObject.SetActive(true);
                Cursor.position = cranePos;
            }
            else
            {
                Cursor.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}