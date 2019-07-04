using LightDx;
using LightDx.InputAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Models
{
    //Vertex buffer data only
    struct Voxel
    {
        [Position]
        public Vector3 Position;
        [TexCoord(1, Format = 0x2A /*DXGI_FORMAT_R32_UINT*/)]
        public uint Dir;
        [Color]
        public Vector4 Color;
    }

    interface IVoxelVertexBufferFactory
    {
        VertexBuffer CreateDynamic(int size);
        VertexBuffer CreateStatic(Voxel[] data);
    }
}
