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
    class EditableVoxelList
    {
        private struct UndoData
        {
            public int X, Y, Z;
            public int? C1, C2;
        }

        private readonly UndoManager _undoManager;
        private readonly EditablePalette _palette;
        private readonly VoxelDataStorage<int> _voxelData;
        private readonly VertexBufferList<Voxel> _buffers;

        private EditableVoxelList _parent;
        public EditableVoxelList Parent
        {
            get => _parent;
            set
            {
                UndoAction CreateUndoAction(EditableVoxelList val)
                {
                    return delegate (out UndoAction redo)
                    {
                        redo = CreateUndoAction(_parent);
                        _parent = val;
                    };
                }
                if (_parent != value)
                {
                    _undoManager.AddModification("Change parent.", CreateUndoAction(_parent));
                    _parent = value;
                }
            }
        }

        private string _bondName;
        public string BondName
        {
            get => _bondName;
            set
            {
                UndoAction CreateUndoAction(string val)
                {
                    return delegate (out UndoAction redo)
                    {
                        redo = CreateUndoAction(_bondName);
                        _bondName = val;
                    };
                }
                if (_bondName != value)
                {
                    _undoManager.AddModification("Modify bond name.", CreateUndoAction(_bondName));
                    _bondName = value;
                }
            }
        }

        private Vector3 _basePoint;
        public Vector3 BasePoint
        {
            get => _basePoint;
            set
            {
                UndoAction CreateUndoAction(Vector3 val)
                {
                    return delegate (out UndoAction redo)
                    {
                        redo = CreateUndoAction(_basePoint);
                        _basePoint = val;
                    };
                }
                if (_basePoint != value)
                {
                    _undoManager.AddModification("Modify base point.", CreateUndoAction(_basePoint));
                    _basePoint = value;
                }
            }
        }

        private Vector3 _translation;
        public Vector3 Translation
        {
            get => _translation;
            set
            {
                UndoAction CreateUndoAction(Vector3 val)
                {
                    return delegate (out UndoAction redo)
                    {
                        redo = CreateUndoAction(_translation);
                        _translation = val;
                    };
                }
                if (_translation != value)
                {
                    _undoManager.AddModification("Modify translation.", CreateUndoAction(_translation));
                    _translation = value;
                }
            }
        }

        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }
        public int MinZ { get; private set; }
        public int MaxZ { get; private set; }

        private bool _boundInitialized;

        private readonly List<UndoData> _undoBuffer = new List<UndoData>();

        public EditableVoxelList(UndoManager undoManager, EditablePalette palette, IVoxelVertexBufferFactory vbFactory)
        {
            _undoManager = undoManager;
            _palette = palette;
            _voxelData = new VoxelDataStorage<int>();
            _buffers = new VertexBufferList<Voxel>(vbFactory);
        }

        public void AddVoxel(int x, int y, int z, int c)
        {
            AddVoxelWithoutUpdating(x, y, z, c);
            UpdateVoxel(x, y, z, c);
        }

        public void AddVoxelWithoutUpdating(int x, int y, int z, int c)
        {
            _undoBuffer.Add(CreateUndoData(x, y, z, c));
            _voxelData.AddAndGetRef(x, y, z) = c;
        }

        public void RemoveVoxelWithoutUpdating(int x, int y, int z)
        {
            _undoBuffer.Add(CreateUndoData(x, y, z, null));
            _voxelData.RemoveAt(x, y, z);
        }

        private UndoData CreateUndoData(int x, int y, int z, int? c)
        {
            int? oldC = null;
            if (_voxelData.TryGet(x, y, z, out var oldCVal))
            {
                oldC = oldCVal;
            }
            return new UndoData
            {
                X = x,
                Y = y,
                Z = z,
                C1 = oldC,
                C2 = c,
            };
        }

        public void FlushUndoAction()
        {
            _undoManager.AddModification("Modify voxel.", CreateUndoAction(_undoBuffer.ToArray(), true));
            _undoBuffer.Clear();
        }

        private UndoAction CreateUndoAction(UndoData[] data, bool isUndo)
        {
            return delegate (out UndoAction redo)
            {
                redo = CreateUndoAction(data, !isUndo);
                foreach (var d in data)
                {
                    var c = isUndo ? d.C1 : d.C2;
                    if (c.HasValue)
                    {
                        _voxelData.AddAndGetRef(d.X, d.Y, d.Z) = c.Value;
                    }
                    else
                    {
                        _voxelData.RemoveAt(d.X, d.Y, d.Z);
                    }
                }
            };
        }

        public void UpdateAll()
        {
            _buffers.Clear();

            _boundInitialized = false;
            foreach (var v in _voxelData)
            {
                UpdateVoxel(v.X, v.Y, v.Z, v.Value);
            }
        }

        private void UpdateVoxel(int x, int y, int z, int c)
        {
            if (!_boundInitialized)
            {
                MinX = MaxX = x;
                MinY = MaxY = y;
                MinZ = MaxZ = z;
                _boundInitialized = true;
            }
            else
            {
                MinX = Math.Min(MinX, x);
                MaxX = Math.Max(MaxX, x);
                MinY = Math.Min(MinY, y);
                MaxY = Math.Max(MaxY, y);
                MinZ = Math.Min(MinZ, z);
                MaxZ = Math.Max(MaxZ, z);
            }

            if (!_voxelData.HasDataAt(x + 1, y, z))
            {
                AddFace(x + 0.5f, y, z, 0, c);
            }
            if (!_voxelData.HasDataAt(x - 1, y, z))
            {
                AddFace(x - 0.5f, y, z, 1, c);
            }
            if (!_voxelData.HasDataAt(x, y + 1, z))
            {
                AddFace(x, y + 0.5f, z, 2, c);
            }
            if (!_voxelData.HasDataAt(x, y - 1, z))
            {
                AddFace(x, y - 0.5f, z, 3, c);
            }
            if (!_voxelData.HasDataAt(x, y, z + 1))
            {
                AddFace(x, y, z + 0.5f, 4, c);
            }
            if (!_voxelData.HasDataAt(x, y, z - 1))
            {
                AddFace(x, y, z - 0.5f, 5, c);
            }
        }

        private void AddFace(float x, float y, float z, uint dir, int c)
        {
            Voxel v = new Voxel
            {
                Position = new Vector3(x, y, z),
                Dir = dir,
                Color = _palette.GetColor(c).WithAlpha(1),
            };
            _buffers.Add(ref v);
        }

        public void DrawAll()
        {
            _buffers.DrawAll();
        }

        public void LoadFromModel(List<EditableVoxelList> allParts, VoxelModelPart part)
        {
            //No support for undo.
            _voxelData.Clear();
            foreach (var d in part.VoxelList)
            {
                _voxelData.AddAndGetRef(d.X, d.Y, d.Z) = d.Color;
            }
            UpdateAll();
            _basePoint = part.BasePoint;
            _translation = part.Translation;
            _bondName = part.BoneName;
            _parent = part.ParentId == -1 ? null : allParts[part.ParentId];
        }
    }
}
