using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Models
{
    class EditablePalette
    {
        private struct ColorMetadata
        {
            public int Count;
            public string Tags;
        }

        private readonly UndoManager _undoManager;
        private Color[] _colors;
        private readonly List<ColorMetadata> _metadata;

        public EditablePalette(UndoManager undoManager)
        {
            _undoManager = undoManager;
            _metadata = new List<ColorMetadata>();
            _colors = new Color[0];
        }

        public Color GetColor(int index)
        {
            if (index >= _colors.Length) return Color.Transparent;
            return _colors[index];
        }

        public void SetColor(int index, Color newValue)
        {
            UndoAction CreateUndoAction(Color oldVal)
            {
                return delegate (out UndoAction redo)
                {
                    redo = CreateUndoAction(_colors[index]);
                    _colors[index] = oldVal;
                };
            }
            _undoManager.AddModification("Modify palette.", CreateUndoAction(_colors[index]));
            _colors[index] = newValue;
        }

        public void Resize(int newSize)
        {
            if (newSize == _colors.Length) return;
            if (newSize > _colors.Length)
            {
                _undoManager.AddModification("Resize palette.", CreateUndoGrow());
                _colors = _colors.Concat(Enumerable.Repeat(Color.Transparent, newSize - _colors.Length)).ToArray();
            }
            else
            {
                _undoManager.AddModification("Resize palette.", CreateUndoShink());
                _colors = _colors.Take(newSize).ToArray();
            }
        }

        private UndoAction CreateUndoShink()
        {
            int oldSize = _colors.Length;
            Color[] backup = _colors.Skip(oldSize).ToArray();
            return delegate (out UndoAction redo)
            {
                redo = CreateUndoGrow();
                _colors = _colors.Concat(backup).ToArray();
            };
        }

        private UndoAction CreateUndoGrow()
        {
            int oldSize = _colors.Length;
            return delegate (out UndoAction redo)
            {
                redo = CreateUndoShink();
                _colors = _colors.Take(oldSize).ToArray();
            };
        }

        //Note that end is exclusive
        public void SetMetadata(int start, int end, string tags)
        {
            UndoAction CreateUndoAction()
            {
                var oldData = _metadata.ToArray();
                return delegate (out UndoAction redo)
                {
                    redo = CreateUndoAction();
                    _metadata.Clear();
                    _metadata.AddRange(oldData);
                };
            }
            CreateUndoAction();
            for (int i = 0, ci = 0; i < _metadata.Count; ci += _metadata[i].Count, ++i)
            {
                if (ci + _metadata[i].Count >= start)
                {
                    if (ci + _metadata[i].Count >= end)
                    {
                        if (ci + _metadata[i].Count > end)
                        {
                            _metadata[i] = new ColorMetadata { Count = ci + _metadata[i].Count - end, Tags = _metadata[i].Tags };
                        }
                        _metadata.Insert(i, new ColorMetadata { Count = end - start, Tags = tags });
                        break;
                    }
                    if (ci < start)
                    {
                        _metadata[i] = new ColorMetadata { Count = start - ci, Tags = _metadata[i].Tags };
                    }
                    else
                    {
                        _metadata.RemoveAt(i);
                    }
                }
            }
        }

        public void LoadFromModel(ColorList[] colorLists)
        {
            _colors = new Color[colorLists.Sum(l => l.Colors.Length)];
            _metadata.Clear();
            int listPointer = 0;
            foreach (var l in colorLists)
            {
                foreach (var c in l.Colors)
                {
                    _colors[listPointer++] = c;
                }
                _metadata.Add(new ColorMetadata { Count = l.Colors.Length, Tags = l.Tags });
            }
        }
    }
}
