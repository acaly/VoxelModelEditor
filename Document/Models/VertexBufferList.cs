using LightDx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Models
{
    class VertexBufferList<TVertex> where TVertex : unmanaged
    {
        private const int BufferSize = 1024;

        private class BufferInfo
        {
            public VertexBuffer Buffer;
            public TVertex[] Data;
            public int Length;
            public bool Dirty;
        }

        public readonly IVoxelVertexBufferFactory _vbFactory;
        private readonly List<BufferInfo> _buffers = new List<BufferInfo>();
        private int _emptySlots = 0;

        public VertexBufferList(IVoxelVertexBufferFactory vbFactory)
        {
            _vbFactory = vbFactory;
        }

        private void NewBuffer()
        {
            var b = _vbFactory.CreateDynamic(BufferSize);
            _buffers.Add(new BufferInfo
            {
                Buffer = b,
                Data = new TVertex[BufferSize],
                Length = 0,
                Dirty = false,
            });
            _emptySlots += BufferSize;
        }

        public void Add(ref TVertex data)
        {
            if (_emptySlots == 0)
            {
                NewBuffer();
            }
            //TODO since we are not removing, we can remember the tail position and avoid searching.
            foreach (var b in _buffers)
            {
                if (b.Length < BufferSize)
                {
                    _emptySlots -= 1;
                    b.Data[b.Length++] = data;
                    b.Dirty = true;
                    return;
                }
            }
            //Should never to here
        }

        public void Clear()
        {
            foreach (var b in _buffers)
            {
                b.Length = 0;
                b.Dirty = true;
            }
            _emptySlots = _buffers.Count * BufferSize;
        }

        public void DrawAll()
        {
            foreach (var b in _buffers)
            {
                if (b.Dirty)
                {
                    b.Buffer.Update(b.Data);
                }
                b.Buffer.Draw(0, b.Length);
            }
        }
    }
}
