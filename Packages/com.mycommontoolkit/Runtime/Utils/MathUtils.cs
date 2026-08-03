using UnityEngine;

namespace MyCommonToolkit
{
    public static class MathUtils
    {
        public static Vector3 FlattenXZ(Vector3 unFlattened) => new(unFlattened.x,0, unFlattened.z);
        public static Vector3 UnFlattenXZ(Vector2 flattened, float y) => new(flattened.x, y, flattened.y);
        public static Vector3 FlattenYZ(Vector3 unFlattened) => new(unFlattened.y,0, unFlattened.z);
        public static Vector3 UnFlattenYZ(Vector2 flattened, float x) => new(x, flattened.x, flattened.y);
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
            => new(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        public static Vector3 Add(this Vector3 vector, float? x = null, float? y = null, float? z = null)
            => new(vector.x + (x ?? 0), vector.y + (y ?? 0), vector.z + (z ?? 0));
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
    => new(x ?? vector.x, y ?? vector.y);
        public static Vector2 Add(this Vector2 vector, float? x = null, float? y = null)
            => new(vector.x + (x ?? 0), vector.y + (y ?? 0));

    }
}
