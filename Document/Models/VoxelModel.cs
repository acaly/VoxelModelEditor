using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Models
{
    class VoxelModel
    {
        public string Name;
        public string Author;
        public string Comments;

        public ColorList[] Palette;
        public VoxelModelPart[] Parts;
    }

    class ColorList
    {
        public string Tags;
        public Color[] Colors;
    }

    class VoxelModelPart
    {
        public int ParentId;
        public string BoneName;

        public VoxelData[] VoxelList;
        public Vector3 BasePoint;
        public Vector3 Translation;
    }

    struct VoxelData
    {
        public int X, Y, Z, Color;
    }
}
