using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Models
{
    class VoxelDataStorage<TValue> : IEnumerable<VoxelDataStorage<TValue>.KeyValuePair>
    {
        public struct KeyValuePair
        {
            public int X, Y, Z;
            public TValue Value;
        }

        private const int BlockSize = 8;

        private class Block
        {
            public int X, Y, Z; //not multiplied by 8
            public TValue[] Data;
            public char[] Bitmap;
        }

        private List<Block> _blocks = new List<Block>();

        private Block GetBlock(int x, int y, int z)
        {
            foreach (var b in _blocks)
            {
                if (b.X == x && b.Y == y && b.Z == z) return b;
            }
            var nb = new Block
            {
                X = x,
                Y = y,
                Z = z,
                Data = new TValue[BlockSize * BlockSize * BlockSize],
                Bitmap = new char[BlockSize * BlockSize * BlockSize / 8 + 1],
            };
            _blocks.Add(nb);
            return nb;
        }

        private static int GetBlockCoord(int voxelCoord)
        {
            if (voxelCoord < 0)
            {
                return (voxelCoord - BlockSize + 1) / BlockSize;
            }
            return voxelCoord / BlockSize;
        }

        public ref TValue AddAndGetRef(int x, int y, int z)
        {
            var bx = GetBlockCoord(x);
            var by = GetBlockCoord(y);
            var bz = GetBlockCoord(z);
            var ix = x - bx * BlockSize;
            var iy = y - by * BlockSize;
            var iz = z - bz * BlockSize;
            var bl = GetBlock(bx, by, bz);
            var i = ix + (iy + iz * BlockSize) * BlockSize;
            bl.Bitmap[i / 8] |= (char)(1 << (i % 8));
            return ref bl.Data[i];
        }

        public void RemoveAt(int x, int y, int z)
        {
            var bx = GetBlockCoord(x);
            var by = GetBlockCoord(y);
            var bz = GetBlockCoord(z);
            var ix = x - bx * BlockSize;
            var iy = y - by * BlockSize;
            var iz = z - bz * BlockSize;
            var bl = GetBlock(bx, by, bz);
            var i = ix + (iy + iz * BlockSize) * BlockSize;
            bl.Bitmap[i / 8] &= (char)(255 ^ (1 << (i % 8)));
        }

        public bool HasDataAt(int x, int y, int z)
        {
            var bx = GetBlockCoord(x);
            var by = GetBlockCoord(y);
            var bz = GetBlockCoord(z);
            var ix = x - bx * BlockSize;
            var iy = y - by * BlockSize;
            var iz = z - bz * BlockSize;
            var bl = GetBlock(bx, by, bz);
            var i = ix + (iy + iz * BlockSize) * BlockSize;
            return 0 != (bl.Bitmap[i / 8] & (1 << (i % 8)));
        }

        public bool TryGet(int x, int y, int z, out TValue result)
        {
            var bx = GetBlockCoord(x);
            var by = GetBlockCoord(y);
            var bz = GetBlockCoord(z);
            var ix = x - bx * BlockSize;
            var iy = y - by * BlockSize;
            var iz = z - bz * BlockSize;
            var bl = GetBlock(bx, by, bz);
            var i = ix + (iy + iz * BlockSize) * BlockSize;
            if (0 == (bl.Bitmap[i / 8] & (1 << (i % 8))))
            {
                result = default;
                return false;
            }
            result = bl.Data[i];
            return true;
        }

        public void Clear()
        {
            _blocks.Clear();
        }

        public IEnumerator<KeyValuePair> GetEnumerator()
        {
            return new VDSEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VDSEnumerator(this);
        }

        private class VDSEnumerator : IEnumerator<KeyValuePair>
        {
            private readonly VoxelDataStorage<TValue> _parent;
            private int _blockIndex;
            private int _bitIndex;

            public VDSEnumerator(VoxelDataStorage<TValue> parent)
            {
                _parent = parent;
                _blockIndex = -1;
            }

            public KeyValuePair Current
            {
                get
                {
                    if (_blockIndex == -1 || _blockIndex == int.MaxValue)
                    {
                        throw new InvalidOperationException();
                    }
                    var i = _bitIndex;
                    int ix = i % BlockSize;
                    i /= BlockSize;
                    int iy = i % BlockSize;
                    int iz = i / BlockSize;
                    var bl = _parent._blocks[_blockIndex];
                    return new KeyValuePair
                    {
                        X = bl.X * BlockSize + ix,
                        Y = bl.Y * BlockSize + iy,
                        Z = bl.Z * BlockSize + iz,
                        Value = bl.Data[_bitIndex],
                    };
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_blockIndex == int.MaxValue)
                {
                    throw new InvalidOperationException();
                }
                if (_blockIndex == -1)
                {
                    _bitIndex = BlockSize * BlockSize * BlockSize - 1;
                }
                do
                {
                    if (++_bitIndex == BlockSize * BlockSize * BlockSize)
                    {
                        _bitIndex = 0;
                        _blockIndex += 1;
                        if (_blockIndex >= _parent._blocks.Count)
                        {
                            _blockIndex = int.MaxValue;
                            return false;
                        }
                    }
                } while (!CheckCurrent());
                return true;
            }

            private bool CheckCurrent()
            {
                var i = _bitIndex;
                var bl = _parent._blocks[_blockIndex];
                return 0 != (bl.Bitmap[i / 8] & (1 << (i % 8)));
            }

            public void Reset()
            {
                _blockIndex = -1;
            }
        }
    }
}
