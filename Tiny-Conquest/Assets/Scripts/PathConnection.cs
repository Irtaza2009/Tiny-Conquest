using UnityEngine;

public class PathConnection : MonoBehaviour
{
    [Header("Connected Platforms")]
    [SerializeField] private Platform platformA;
    [SerializeField] private Platform platformB;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypointsFromA;

    public Platform GetOtherPlatform(Platform platform)
    {
        if (platform == platformA)
            return platformB;
        if (platform == platformB)
            return platformA;

        return null;
    }

    public bool Connects(Platform a, Platform b)
    {
        return (platformA == a && platformB == b) ||
               (platformA == b && platformB == a);
    }

    public bool TryGetRoute(Platform source, Platform destination, out Transform[] route)
    {
        route = null;

        if (!Connects(source, destination))
        {
            return false;
        }

        // forward
        if (source == platformA && destination == platformB)
        {
            route = waypointsFromA;
            return route != null && route.Length > 0;
        }

        // reverse (note: have to decide whether to allow this)
        if (source == platformB && destination == platformA)
        {
            route = new Transform[waypointsFromA.Length];
            for (int i = 0; i < waypointsFromA.Length; i++)
            {
                route[i] = waypointsFromA[waypointsFromA.Length - 1 - i];
            }
            return route.Length > 0;
        }
        return false;
    }
}
