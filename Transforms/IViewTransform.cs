using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Transforms
{
    struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;
    }

    interface IViewTransform
    {
        Vector2 WorldPosToControl(Vector3 pos);
        Vector2 WorldPosToNormalized(Vector3 pos);
        Ray ControlPosToRay(Vector2 pos);
    }
}
