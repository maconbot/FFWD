﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PressPlay.FFWD
{
    internal static class VectorConverter
    {
        /// <summary>
        /// Converts the Vector3 to a Vector2 by dropping a given variable.
        /// If Correct Zero Data is on, we will try to use a different mode if one of the axes are 0 after converting. Used primarily for collider sizing.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="correctZeroData"></param>
        /// <returns></returns>
        internal static Vector2 Convert(Vector3 v3, Physics.To2dMode mode)
        {
            Vector2 v = Vector2.zero;
            switch (mode)
            {
                case Physics.To2dMode.DropX:
                    v.x = v3.y;
                    v.y = v3.z;
                    break;
                case Physics.To2dMode.DropY:
                    v.x = v3.x;
                    v.y = v3.z;
                    break;
                case Physics.To2dMode.DropZ:
                    v.x = v3.x;
                    v.y = v3.y;
                    break;
                default:
                    throw new Exception("Unknown enum " + mode);
            }
            return v;
        }

        /// <summary>
        /// Converts the Vector3 to a Vector2 by dropping a given variable.
        /// If Correct Zero Data is on, we will try to use a different mode if one of the axes are 0 after converting. Used primarily for collider sizing.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="correctZeroData"></param>
        /// <returns></returns>
        internal static Vector3 Convert(Vector2 v2, Physics.To2dMode mode)
        {
            Vector3 v = Vector3.zero;
            switch (mode)
            {
                case Physics.To2dMode.DropX:
                    v.y = v2.x;
                    v.z = v2.y;
                    break;
                case Physics.To2dMode.DropY:
                    v.x = v2.x;
                    v.z = v2.y;
                    break;
                case Physics.To2dMode.DropZ:
                    v.x = v2.x;
                    v.y = v2.y;
                    break;
                default:
                    throw new Exception("Unknown enum " + mode);
            }
            return v;
        }

        /// <summary>
        /// Converts the Vector3 to a Vector2 by dropping a given variable.
        /// If Correct Zero Data is on, we will try to use a different mode if one of the axes are 0 after converting. Used primarily for collider sizing.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="correctZeroData"></param>
        /// <returns></returns>
        internal static Vector3 Convert(Microsoft.Xna.Framework.Vector2 v2, Physics.To2dMode mode)
        {
            Vector3 v = Vector3.zero;
            switch (mode)
            {
                case Physics.To2dMode.DropX:
                    v.y = v2.X;
                    v.z = v2.Y;
                    break;
                case Physics.To2dMode.DropY:
                    v.x = v2.X;
                    v.z = v2.Y;
                    break;
                case Physics.To2dMode.DropZ:
                    v.x = v2.X;
                    v.y = v2.Y;
                    break;
                default:
                    throw new Exception("Unknown enum " + mode);
            }
            return v;
        }

        internal static float Reduce(Vector3 v, Physics.To2dMode mode)
        {
            switch (mode)
            {
                case Physics.To2dMode.DropX:
                    return v.x;
                case Physics.To2dMode.DropY:
                    return v.y;
                case Physics.To2dMode.DropZ:
                    return v.z;
            }
            return 0f;
        }

        /// <summary>
        /// This is essentially the same as Reduce, but some angles are inverted.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        internal static float Angle(Vector3 v, Physics.To2dMode mode)
        {
            switch (mode)
            {
                case Physics.To2dMode.DropX:
                    return v.x;
                case Physics.To2dMode.DropY:
                    return -v.y;
                case Physics.To2dMode.DropZ:
                    return v.z;
            }
            return 0f;
        }

        // Gets the up vector in relation to the physics mode. This is the vector that is getting dropped.
        internal static Vector3 GetUp(Physics.To2dMode mode)
        {
            switch (mode)
            {
                case Physics.To2dMode.DropX:
                    return Vector3.right;
                case Physics.To2dMode.DropZ:
                    return Vector3.forward;
            }
            return Vector3.up;
        }
    }
}
