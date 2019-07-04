using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document
{
    delegate void UndoAction(out UndoAction redo);

    class UndoManager
    {
        public void AddModification(string desc, UndoAction undoAction)
        {

        }
    }
}
