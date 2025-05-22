using UnityEngine;
using Assets.Scripts.MapEditor.Models;

namespace Assets.Scripts.MapEditor.Actions
{
    public class TransformModifyAction : IUndoableAction
    {
        readonly PlacedObject po;
        readonly Vector3 pos0, rot0, scale0;
        readonly Vector3 pos1, rot1, scale1;

        public TransformModifyAction(PlacedObject po,
                                     Vector3 beforePos, Vector3 beforeRot, Vector3 beforeScale,
                                     Vector3 afterPos, Vector3 afterRot, Vector3 afterScale)
        {
            this.po = po;
            pos0 = beforePos; rot0 = beforeRot; scale0 = beforeScale;
            pos1 = afterPos; rot1 = afterRot; scale1 = afterScale;
        }

        public object Undo() 
        { 
            Apply(pos0, rot0, scale0); 
            return null; 
        }

        public object Redo() 
        { 
            Apply(pos1, rot1, scale1); 
            return null; 
        }

        void Apply(Vector3 p, Vector3 r, Vector3 s)
        {
            po.instance.transform.SetPositionAndRotation(p, Quaternion.Euler(r));
            po.instance.transform.localScale = s;
        }
    }
}
