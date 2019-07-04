using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Postures
{
    class PostureTransformCache
    {
        //fixed for one bone hierarchy
        //re-created (and thus cleared) when model or its bone structure (including base point) is changed
        //data is passed to the cache by the renderer, and the cache does not communicate with PostureProvider
    }
}
