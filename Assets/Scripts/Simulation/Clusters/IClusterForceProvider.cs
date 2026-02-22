namespace FallingSand
{
    /// <summary>
    /// Interface for systems that apply forces to clusters each physics step.
    /// Providers are registered in a list and iterated uniformly — no per-type wiring needed.
    /// </summary>
    public interface IClusterForceProvider
    {
        /// <summary>
        /// Apply forces to all affected clusters.
        /// Implementations should increment cluster.activeForceCount for any cluster they affect,
        /// so the sleep guard knows not to force-sleep them.
        /// </summary>
        void ApplyForcesToClusters(ClusterManager clusterManager, int worldWidth, int worldHeight);
    }
}
