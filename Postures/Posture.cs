using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelModelEditor.Postures
{
    //Serialization only
    class PostureData
    {
        public PostureBoneData[] Bones;
        public Vector3 RootTranslation;
    }

    struct PostureBoneData
    {
        public string Name;
        public PostureBoneTransform Transformation;
    }

    struct PostureBoneTransform
    {
        public Vector3 Scaling;
        public Quaternion Rotation;

        public static readonly PostureBoneTransform Empty =
            new PostureBoneTransform { Scaling = new Vector3(1, 1, 1), Rotation = Quaternion.Identity };
    }
}
