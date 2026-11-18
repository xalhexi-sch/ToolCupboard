using UnityEngine;

namespace ToolCupboard.Models
{
    public class ProtectionCenter
    {
        public Vector3 Position { get; set; }
        public float Radius { get; set; }
        public float SqrRadius { get; set; }
        public ulong Owner { get; set; }
        public ulong Group { get; set; }
        public string SourceType { get; set; }

        public bool Protects(Vector3 point, ulong targetOwner, ulong targetGroup)
        {
            if ((Position - point).sqrMagnitude > SqrRadius)
                return false;

            // An unowned protection device protects all, or matches if owner / group match
            if (Owner == 0)
                return true;

            if (Owner == targetOwner)
                return true;

            if (Group != 0 && Group == targetGroup)
                return true;

            return false;
        }
    }
}
