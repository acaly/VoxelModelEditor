using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Document.Postures
{
    interface IPostureSource
    {
        bool Updated { get; }
        Vector3 RootTranslation { get; }
        PostureBoneTransform GetBoneData(string bone);
    }

    class EmptyPostureSource : IPostureSource
    {
        public bool Updated => false;
        public Vector3 RootTranslation => new Vector3();

        public PostureBoneTransform GetBoneData(string bone)
        {
            return PostureBoneTransform.Empty;
        }
    }

    //TODO single posture source
}
