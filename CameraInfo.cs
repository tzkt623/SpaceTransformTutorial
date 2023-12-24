using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace tezcat.Tutorial
{
    public class CameraInfo : MonoBehaviour
    {
        public Camera mCamera;
        public bool mDrawAllRay;

        [Header("Space Info")]
        [Tooltip("Drag this to control MarkObject when mFreeMarkObject=false")]
        public Vector3 mCurrentWorldPosition;
        [Tooltip("Just Show data, Do not drag this")]
        public Vector3 mViewSpacePosition;
        [Tooltip("Just Show data, Do not drag this")]
        public Vector3 mClipSpacePosition;
        [Tooltip("Just Show data, Do not drag this")]
        public float mClipSize;
        [Tooltip("Just Show data, Do not drag this")]
        public Vector3 mNDCSpacePosition;

        [Header("Object Setting")]
        public Transform mMarkObject;
        public Transform mClipObject;
        public Transform mNDCObject;
        public Transform mViewObject;

        [Header("Space Setting")]
        public Transform mClipSpace;
        public Transform mNDCSpace;
        public Transform mViewSpace;

        [Header("NDC Setting")]
        [Tooltip("Control MarkObject in GameWindow by mousePosition")]
        public bool mUseScreenPos = false;
        [Tooltip("You can drag the MarkObject")]
        public bool mFreeMarkObject = false;
        [Tooltip("By multi far to get a normalize depth")]
        public bool mNormalizedDepth = false;
        public Vector3 mScreenNDC;
        public float mDepth = 5;
        public Vector4 mViewPos;

        Vector2 mMousePosition;

        void Start()
        {
        }

        private void OnDrawGizmos()
        {
            this.drawCameraInfo();
            this.drawSpaceViewer();
            this.drawInvNDCFunc();
        }

        private void drawInvNDCFunc()
        {
            /*
             * WolrdSpace * ViewMatrix => ViewSpace
             * ViewSpace * ProjectionMatrix => ClipSpace
             * ClipSpace / ClipSpace.w => NDCSpace
             */

            /*
             * NDCSpace * ClipSpace.w => ClipSpace
             * ClipSpace * InvProjectionMatrix => ViewSpace
             * ViewSpace * InvViewMatrix => WorldSpace
             */

            if (mUseScreenPos)
            {
                var scene_camera = SceneView.currentDrawingSceneView.camera;

                mMousePosition = Event.current.mousePosition;
                mMousePosition.y = scene_camera.pixelHeight - mMousePosition.y * EditorGUIUtility.pixelsPerPoint;
                mMousePosition.x *= EditorGUIUtility.pixelsPerPoint;

                //Debug.Log($"{scene_camera.pixelWidth},{scene_camera.pixelHeight},{mMousePosition}");

                Vector2 uv = new Vector2(mMousePosition.x / scene_camera.pixelWidth, mMousePosition.y / scene_camera.pixelHeight);
                uv = uv * 2.0f - Vector2.one;
                mScreenNDC = uv;

                //Debug.Log(mScreenNDC);
            }

            mScreenNDC = Vector3.Min(mScreenNDC, Vector3.one);
            mScreenNDC = Vector3.Max(mScreenNDC, -Vector3.one);

            if (mNormalizedDepth)
            {
                mDepth = Mathf.Min(mDepth, 1);
                mDepth = Mathf.Max(mDepth, mCamera.nearClipPlane / mCamera.farClipPlane);
            }
            else
            {
                mDepth = Mathf.Min(mDepth, mCamera.farClipPlane);
                mDepth = Mathf.Max(mDepth, mCamera.nearClipPlane);
            }

            Vector4 ndcPos = new Vector4(mScreenNDC.x, mScreenNDC.y, mScreenNDC.z, 0);
            float far = mCamera.farClipPlane;
            Vector4 clipPos = mNormalizedDepth ? new Vector4(ndcPos.x, ndcPos.y, ndcPos.z, 1) * far : new Vector4(ndcPos.x, ndcPos.y, ndcPos.z, 1);

            var viewPos = mCamera.projectionMatrix.inverse * clipPos;
            mViewPos = viewPos;
            Vector4 viewPos3 = new Vector4(viewPos.x, viewPos.y, viewPos.z, 0.0f) * mDepth;

            Vector3 viewPos3World = mCamera.cameraToWorldMatrix * viewPos3;
            //Debug.Log(viewPos3World);
            Gizmos.DrawLine(mCamera.transform.position, mCamera.transform.position + viewPos3World);

            if (!mFreeMarkObject)
            {
                mMarkObject.position = mCamera.transform.position + viewPos3World;
                mCurrentWorldPosition = mMarkObject.localPosition;
            }
        }

        private void drawCameraInfo()
        {
            var resolution = Screen.currentResolution;
            var camera_pos = mCamera.transform.position;
            var plane_length = mCamera.nearClipPlane;
            var rate_fDn = mCamera.farClipPlane / mCamera.nearClipPlane;

            var near_height = plane_length * Mathf.Tan(Mathf.Deg2Rad * mCamera.fieldOfView * 0.5f);
            var near_width = near_height * mCamera.aspect;

            var near_center_pos = camera_pos + mCamera.transform.forward * plane_length;
            var right_offset = mCamera.transform.right * near_width;
            var up_offset = mCamera.transform.up * near_height;
            var lb = near_center_pos - right_offset - up_offset;
            var rt = near_center_pos + right_offset + up_offset;
            var rb = near_center_pos + right_offset - up_offset;

            var lengthH = rt - rb;

            if (mDrawAllRay)
            {
                var screen_width = resolution.width / 512;
                var screen_height = resolution.height / 512;
                for (int sh = 0; sh <= screen_height; sh++)
                {
                    for (int sw = 0; sw <= screen_width; sw++)
                    {
                        var camera_ray_end_pos = Vector3.Lerp(lb, rb, sw / (float)screen_width);
                        camera_ray_end_pos += lengthH * sh / screen_height;

                        Gizmos.DrawLine(camera_pos, camera_ray_end_pos);
                    }
                }
            }


            Gizmos.DrawWireSphere(camera_pos, 0.02f);


            //draw near plane
            Gizmos.DrawWireSphere(near_center_pos, 0.02f);
            Gizmos.DrawWireSphere(lb, 0.02f);
            Gizmos.DrawWireSphere(rt, 0.02f);
            Gizmos.DrawLine(near_center_pos - right_offset, near_center_pos + right_offset);
            Gizmos.DrawLine(near_center_pos - up_offset, near_center_pos + up_offset);

            var near_up_sp = near_center_pos + mCamera.transform.up * near_height;
            Gizmos.DrawWireSphere(near_up_sp, 0.02f);

            //draw far plane
            var far_sp = camera_pos + mCamera.transform.forward * mCamera.farClipPlane;
            Gizmos.DrawWireSphere(far_sp, 0.02f);
            Gizmos.DrawLine(far_sp - right_offset * rate_fDn, far_sp + right_offset * rate_fDn);
            Gizmos.DrawLine(far_sp - up_offset * rate_fDn, far_sp + up_offset * rate_fDn);

            var rate = mCamera.nearClipPlane / mCamera.farClipPlane;
            var dir = near_up_sp - camera_pos;
            var far_up_sp = camera_pos + dir / rate;
            Gizmos.DrawWireSphere(far_up_sp, 0.02f);
        }

        private void drawSpaceViewer()
        {
            if (!mFreeMarkObject)
            {
                mMarkObject.localPosition = mCurrentWorldPosition;
            }

            var wpos = mMarkObject.transform.position;

            Vector4 sample_pos = new Vector4(wpos.x, wpos.y, wpos.z, 1);

            mMarkObject.localScale = mMarkObject.localScale;
            mViewObject.localScale = mMarkObject.localScale;
            mClipObject.localScale = mMarkObject.localScale;
            mNDCObject.localScale = mMarkObject.localScale;

            //View
            var view_pos = mCamera.worldToCameraMatrix * sample_pos;
            var save_mat = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(mViewSpace.transform.position, mViewSpace.transform.rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, mCamera.fieldOfView, mCamera.farClipPlane, mCamera.nearClipPlane, mCamera.aspect);
            Gizmos.matrix = save_mat;
            mViewObject.localPosition = new Vector3(view_pos.x, view_pos.y, -view_pos.z);
            mViewSpacePosition = mViewObject.localPosition;

            //Clip
            var clip_pos = mCamera.projectionMatrix * view_pos;
            Gizmos.DrawWireCube(mClipSpace.transform.position, new Vector3(clip_pos.w, clip_pos.w, clip_pos.w) * 2);
            mClipSize = clip_pos.w;
            var clip_coord = new Vector3(clip_pos.x, clip_pos.y, clip_pos.z);
            //Gizmos.DrawSphere(mClipSpace.transform.position + clip_coord, 0.05f);
            mClipObject.localPosition = clip_coord;
            mClipSpacePosition = mClipObject.localPosition;

            //NDC
            var ndc_pos = new Vector3(clip_pos.x / clip_pos.w, clip_pos.y / clip_pos.w, clip_pos.z / clip_pos.w);
            Gizmos.DrawWireCube(mNDCSpace.transform.position, Vector3.one * 2);
            //ndc_pos.z = (this.LinearizeDepth(ndc_pos.z) / mCamera.farClipPlane) * 2 - 1;
            mNDCObject.localPosition = ndc_pos;
            mNDCSpacePosition = mNDCObject.localPosition;
            //Gizmos.DrawSphere(mNDCSpace.transform.position + ndc_pos, 0.05f);
        }
    }
}