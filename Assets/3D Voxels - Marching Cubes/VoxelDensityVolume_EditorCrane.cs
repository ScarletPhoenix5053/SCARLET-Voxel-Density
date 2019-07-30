using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCARLET.VoxelDensity
{
    public class VoxelDensityVolume_EditorCrane : MonoBehaviour
    {
        #region Modifiable Variables

        [Header("Volume")]
        public VoxelDensityVolume VoxelDensityVolume;

        [Header("Cursor")]
        public Transform Cursor;
        public bool HoldMouseToPaint = true;
        public LayerMask ColliderMask;
        public GameObject MouseDetectionPlane;
        public float MouseDetectionPlaneHeight = -5;
        public Transform[] CursorExtraLines;

        [Header("Scroll Wheel")]
        public bool ScrollWheelInverted = true;
        public float ScrollWheelSensitivity = 1f;

        [Header("Crane Limits")]
        public float CraneHeightMin = 0;
        public float CraneHeightMax = 10;


        #endregion

        #region Encapsulated Variables

        private VoxelBrush primaryBrush = new VoxelBrush();
        private VoxelBrush secondaryBrush = new VoxelBrush();

        private float craneHeight = 0f;
        private Vector3 cranePos = Vector3.zero;


        #endregion

        #region Unity Messages

        void Start()
        {
            // Define Default brush
            primaryBrush.ValueDirectionPairs = new VoxelDirectionValuePair[]
            {
                new VoxelDirectionValuePair(0,0,0,1)/*,
                new VoxelDirectionValuePair2D(-1,0,1),
                new VoxelDirectionValuePair2D(0,1,1),
                new VoxelDirectionValuePair2D(1,0,1),
                new VoxelDirectionValuePair2D(0,-1,1)*/
            };
            secondaryBrush.ValueDirectionPairs = new VoxelDirectionValuePair[]
            {
                new VoxelDirectionValuePair(0,0,0,0)/*,
                new VoxelDirectionValuePair2D(-1,0,0),
                new VoxelDirectionValuePair2D(0,1,0),
                new VoxelDirectionValuePair2D(1,0,0),
                new VoxelDirectionValuePair2D(0,-1,0)*/
            };

            //Place mouse detection plane
            MouseDetectionPlane.transform.localPosition = Vector3.up * MouseDetectionPlaneHeight;
        }

        void Update()
        {
            // If mouse is over a collider
            var hit = new RaycastHit();
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, ColliderMask))
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
                CursorExtraLines[0].localPosition = Vector3.up * cranePos.y;
                CursorExtraLines[1].localPosition = Vector3.up * cranePos.y + Vector3.right * cranePos.x;
                CursorExtraLines[2].localPosition = Vector3.up * cranePos.y + Vector3.forward * cranePos.z;
                CursorExtraLines[3].localPosition = new Vector3(cranePos.x, 0, cranePos.z);

                // Allow editing of voxel state
                sbyte mouseDown = 0;
                if (HoldMouseToPaint)
                {
                    if (Input.GetKey(KeyCode.Mouse0)) mouseDown--;
                    if (Input.GetKey(KeyCode.Mouse1)) mouseDown++;
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0)) mouseDown--;
                    if (Input.GetKeyDown(KeyCode.Mouse1)) mouseDown++;
                }
                if (mouseDown != 0)
                {
                    VoxelDensityVolume.ApplyVoxelBrush(
                        cranePos,
                        mouseDown < 0 ? primaryBrush : secondaryBrush
                        );
                }
            }
            else
            {
                Cursor.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}