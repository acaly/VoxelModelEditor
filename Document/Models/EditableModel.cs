using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Models
{
    struct EditableModelMetadata
    {
        public string Name;
        public string Author;
        public string Comments;

        public bool Equals(EditableModelMetadata other)
        {
            return Name == other.Name && Author == other.Author && Comments == other.Comments;
        }
    }

    class EditableModel
    {
        private readonly IVoxelVertexBufferFactory _vbFactory;
        private readonly UndoManager _undoManager;
        private List<EditableVoxelList> _parts = new List<EditableVoxelList>();

        public EditableModel(IVoxelVertexBufferFactory vbFactory)
        {
            _vbFactory = vbFactory;
            _undoManager = new UndoManager();
        }

        private EditableModelMetadata _metadata;
        public EditableModelMetadata Metadata
        {
            get => _metadata;
            set
            {
                if (!_metadata.Equals(value))
                {
                    _undoManager.AddModification("Edit model metadata.", CreateUndoMeta(_metadata));
                    _metadata = value;
                }
            }
        }

        public EditablePalette Palette { get; private set; }

        public int PartCount => _parts.Count;

        public EditableVoxelList GetPart(int index)
        {
            return _parts[index];
        }

        public void InsertPart(int index)
        {
            _undoManager.AddModification("Insert part.", CreateUndoInsert(index));
            _parts.Insert(index, new EditableVoxelList(_undoManager, Palette, _vbFactory));
        }

        public void RemovePart(int index)
        {
            BreakPartDependent(index);
            _undoManager.AddModification("Remove part.", CreateUndoRemove(index));
            _parts.RemoveAt(index);
        }

        private void BreakPartDependent(int index)
        {
            foreach (var p in _parts)
            {
                if (p.Parent == _parts[index])
                {
                    p.Parent = null;
                }
            }
        }

        private UndoAction CreateUndoMeta(EditableModelMetadata metadata)
        {
            return delegate (out UndoAction redo)
            {
                redo = CreateUndoMeta(_metadata);
                _metadata = metadata;
            };
        }

        private UndoAction CreateUndoInsert(int index)
        {
            return delegate (out UndoAction redo)
            {
                redo = CreateUndoRemove(index);
                _parts.RemoveAt(index);
            };
        }

        private UndoAction CreateUndoRemove(int index)
        {
            var part = _parts[index];
            return delegate (out UndoAction redo)
            {
                redo = CreateUndoInsert(index);
                _parts.Insert(index, part);
            };
        }

        public void LoadFromModel(VoxelModel model)
        {
            //No support for undo.
            _metadata = new EditableModelMetadata
            {
                Name = model.Name,
                Author = model.Author,
                Comments = model.Comments,
            };

            Palette = new EditablePalette(_undoManager);
            Palette.LoadFromModel(model.Palette);

            _parts.Clear();
            foreach (var p in model.Parts)
            {
                _parts.Add(new EditableVoxelList(_undoManager, Palette, _vbFactory));
            }
            for (int i = 0; i < model.Parts.Length; ++i)
            {
                _parts[i].LoadFromModel(_parts, model.Parts[i]);
            }
        }

        public void CalculateBound(out Vector3 min, out Vector3 max)
        {
            if (_parts.Count == 0)
            {
                min = max = new Vector3();
                return;
            }
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var p in _parts)
            {
                min.X = Math.Min(min.X, p.MinX);
                min.Y = Math.Min(min.Y, p.MinY);
                min.Z = Math.Min(min.Z, p.MinZ);
                max.X = Math.Max(max.X, p.MaxX);
                max.Y = Math.Max(max.Y, p.MaxY);
                max.Z = Math.Max(max.Z, p.MaxZ);
            }
        }
    }
}
