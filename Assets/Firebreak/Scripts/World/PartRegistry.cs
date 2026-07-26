using System.Collections.Generic;
using UnityEngine;

namespace Firebreak
{
    public class PartRegistry : MonoBehaviour
    {
        private readonly List<Part> _parts = new();

        public int UnclaimedCount => _parts.FindAll(p => p != null && !p.Claimed && !p.Collected).Count;

        public void Add(Part part)
        {
            if (part != null && !_parts.Contains(part))
                _parts.Add(part);
        }

        public void Remove(Part part) => _parts.Remove(part);

        public Part FindNearestUnclaimed(Vector3 from, float radius)
        {
            Part nearest = null;
            float nearestDistanceSqr = radius * radius;
            foreach (var part in _parts)
            {
                if (part == null || part.Claimed || part.Collected)
                    continue;

                float distanceSqr = (part.Position - from).sqrMagnitude;
                if (distanceSqr <= nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = part;
                }
            }
            return nearest;
        }
    }
}
