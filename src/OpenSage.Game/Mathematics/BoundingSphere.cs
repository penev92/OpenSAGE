﻿using System;
using System.Numerics;

namespace OpenSage.Mathematics
{
    public struct BoundingSphere
    {
        public Vector3 Center;

        public float Radius;

        public BoundingSphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
        {
            Vector3 ocenterToaCenter = Vector3.Subtract(additional.Center, original.Center);
            float distance = ocenterToaCenter.Length();
            if (distance <= original.Radius + additional.Radius)
            {
                if (distance <= original.Radius - additional.Radius)
                {
                    return original;
                }
                if (distance <= additional.Radius - original.Radius)
                {
                    return additional;
                }
            }
            float leftRadius = Math.Max(original.Radius - distance, additional.Radius);
            float Rightradius = Math.Max(original.Radius + distance, additional.Radius);
            ocenterToaCenter = ocenterToaCenter + (((leftRadius - Rightradius) / (2 * ocenterToaCenter.Length())) * ocenterToaCenter);

            return new BoundingSphere
            {
                Center = original.Center + ocenterToaCenter,
                Radius = (leftRadius + Rightradius) / 2
            };
        }

        public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
        {
            // Find the center of the box.
            Vector3 center = new Vector3((box.Min.X + box.Max.X) / 2.0f,
                                         (box.Min.Y + box.Max.Y) / 2.0f,
                                         (box.Min.Z + box.Max.Z) / 2.0f);

            // Find the distance between the center and one of the corners of the box.
            float radius = Vector3.Distance(center, box.Max);

            return new BoundingSphere(center, radius);
        }

        public BoundingSphere Transform(Matrix4x4 matrix)
        {
            return new BoundingSphere
            {
                Center = Vector3.Transform(Center, matrix),
                Radius = Radius * (MathUtility.Sqrt(
                    Math.Max(
                        ((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12)) + (matrix.M13 * matrix.M13), 
                        Math.Max(
                            ((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22)) + (matrix.M23 * matrix.M23), 
                            ((matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32)) + (matrix.M33 * matrix.M33)))))
            };
        }

        public PlaneIntersectionType Intersects(Plane plane)
        {
            var distance = Vector3.Dot(plane.Normal, this.Center);
            distance += plane.D;
            if (distance > this.Radius)
                return PlaneIntersectionType.Front;
            else if (distance < -this.Radius)
                return PlaneIntersectionType.Back;
            else
                return PlaneIntersectionType.Intersecting;
        }
    }
}
